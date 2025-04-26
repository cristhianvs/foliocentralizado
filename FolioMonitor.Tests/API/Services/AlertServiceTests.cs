using FolioMonitor.API.Services;
using FolioMonitor.Core.Interfaces;
using FolioMonitor.Core.Models;
using FolioMonitor.Core.Models.DTOs;
using Microsoft.Extensions.Logging;
using Moq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;
using FluentAssertions;
using System;

namespace FolioMonitor.Tests.API.Services;

public class AlertServiceTests
{
    private readonly Mock<IConfigurationRepository> _mockConfigRepo;
    private readonly Mock<INotificationService> _mockNotificationService;
    private readonly Mock<ILogger<AlertService>> _mockLogger;
    private readonly AlertService _service;

    private const string WarningKey = "AlertThreshold_Warning";
    private const string CriticalKey = "AlertThreshold_Critical";

    public AlertServiceTests()
    {
        _mockConfigRepo = new Mock<IConfigurationRepository>();
        _mockNotificationService = new Mock<INotificationService>();
        _mockLogger = new Mock<ILogger<AlertService>>();
        _service = new AlertService(_mockConfigRepo.Object, _mockNotificationService.Object, _mockLogger.Object);
    }

    private void SetupThresholds(string? warningValue, string? criticalValue)
    {
        _mockConfigRepo.Setup(r => r.GetByKeyAsync(WarningKey))
                       .ReturnsAsync(warningValue != null ? new Configuration { Key = WarningKey, Value = warningValue } : null);
        _mockConfigRepo.Setup(r => r.GetByKeyAsync(CriticalKey))
                       .ReturnsAsync(criticalValue != null ? new Configuration { Key = CriticalKey, Value = criticalValue } : null);
    }

    [Theory]
    [InlineData(100, 20, 15, 0, 1)] // Below critical -> Only Critical alert (Corrected)
    [InlineData(100, 20, 20, 0, 1)] // At critical -> Only Critical alert (Corrected)
    [InlineData(100, 20, 21, 1, 0)] // Above critical, below warning -> Warning alert
    [InlineData(100, 20, 100, 1, 0)] // At warning -> Warning alert
    [InlineData(100, 20, 101, 0, 0)] // Above warning -> No alert
    public async Task CheckAlertsAsync_ShouldSendCorrectAlertsBasedOnThresholds(
        int warningThreshold, int criticalThreshold, int availableFolios, 
        int expectedWarningCalls, int expectedCriticalCalls)
    {
        // Arrange
        SetupThresholds(warningThreshold.ToString(), criticalThreshold.ToString());
        var request = new FolioUpdateRequestDto
        {
            Facturas = new List<FolioUpdateItemDto>
            {
                new FolioUpdateItemDto { Modulo = "MOD28", FolioInicio=1, FolioFin=200, FoliosDisponibles = availableFolios }
            }
        };

        // Act
        await _service.CheckAlertsAsync(request);

        // Assert
        _mockNotificationService.Verify(
            n => n.SendAlertAsync(It.Is<AlertInfo>(a => a.Level == AlertLevel.Warning)), 
            Times.Exactly(expectedWarningCalls));
            
        _mockNotificationService.Verify(
            n => n.SendAlertAsync(It.Is<AlertInfo>(a => a.Level == AlertLevel.Critical)), 
            Times.Exactly(expectedCriticalCalls));
    }

    [Fact]
    public async Task CheckAlertsAsync_ShouldUseDefaultThresholds_WhenConfigMissingOrInvalid()
    {
        // Arrange
        // Setup repo to return null or invalid values
        SetupThresholds(null, "not-an-int"); 
        var request = new FolioUpdateRequestDto
        {
            Facturas = new List<FolioUpdateItemDto>
            {
                // Default critical is 20, Default warning is 100
                new FolioUpdateItemDto { Modulo = "MOD28", FolioInicio=1, FolioFin=200, FoliosDisponibles = 15 }, // Should trigger critical
                new FolioUpdateItemDto { Modulo = "MOD29", FolioInicio=1, FolioFin=200, FoliosDisponibles = 90 }  // Should trigger warning
            }
        };

        // Act
        await _service.CheckAlertsAsync(request);

        // Assert
        _mockNotificationService.Verify(
            n => n.SendAlertAsync(It.Is<AlertInfo>(a => a.Level == AlertLevel.Critical && a.Threshold == 20)), 
            Times.Once);
        _mockNotificationService.Verify(
            n => n.SendAlertAsync(It.Is<AlertInfo>(a => a.Level == AlertLevel.Warning && a.Threshold == 100)), 
            Times.Once);
    }
    
    [Fact]
    public async Task CheckAlertsAsync_ShouldNotSendDuplicateAlerts_InSameRun()
    {
        // Arrange
        SetupThresholds("100", "20"); 
        var request = new FolioUpdateRequestDto
        {
            Facturas = new List<FolioUpdateItemDto>
            {
                // Same series, should only trigger one alert even if processed multiple times (though concat prevents this here)
                new FolioUpdateItemDto { Modulo = "MOD28", FolioInicio=1, FolioFin=200, FoliosDisponibles = 15 }, 
            },
             NotasCredito = new List<FolioUpdateItemDto>
            {
                new FolioUpdateItemDto { Modulo = "MOD28", FolioInicio=1, FolioFin=200, FoliosDisponibles = 15 }, 
            }
        };

        // Act
        await _service.CheckAlertsAsync(request);

        // Assert
        // Should only send one critical alert for the series 1-200 MOD28
        _mockNotificationService.Verify(
            n => n.SendAlertAsync(It.Is<AlertInfo>(a => a.Level == AlertLevel.Critical && a.FolioInicio == 1)), 
            Times.Once); 
        _mockNotificationService.Verify(
            n => n.SendAlertAsync(It.Is<AlertInfo>(a => a.Level == AlertLevel.Warning)), 
            Times.Never);
    }
} 