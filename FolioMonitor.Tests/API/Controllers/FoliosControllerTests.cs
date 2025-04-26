using FolioMonitor.API.Controllers;
using FolioMonitor.Core.Interfaces;
using FolioMonitor.Core.Models;
using FolioMonitor.Core.Models.DTOs;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;
using FluentAssertions;
using System;
using System.Linq;
using Microsoft.Extensions.Configuration;

namespace FolioMonitor.Tests.API.Controllers;

public class FoliosControllerTests
{
    private readonly Mock<IFolioHistoryRepository> _mockHistoryRepo;
    private readonly Mock<IAlertService> _mockAlertService;
    private readonly Mock<ILogger<FoliosController>> _mockLogger;
    private readonly Mock<IConfiguration> _mockConfiguration;
    private readonly FoliosController _controller;

    public FoliosControllerTests()
    {
        _mockHistoryRepo = new Mock<IFolioHistoryRepository>();
        _mockAlertService = new Mock<IAlertService>();
        _mockLogger = new Mock<ILogger<FoliosController>>();
        _mockConfiguration = new Mock<IConfiguration>();

        var mockConfigSection = new Mock<IConfigurationSection>();
        mockConfigSection.Setup(s => s.Value).Returns("365");
        _mockConfiguration.Setup(c => c.GetSection("RetentionPolicy:RetentionDays"))
                          .Returns(mockConfigSection.Object);

        _controller = new FoliosController(
            _mockHistoryRepo.Object, 
            _mockAlertService.Object, 
            _mockLogger.Object,
            _mockConfiguration.Object);
    }

    // --- UpdateFolioData Tests ---
    [Fact]
    public async Task UpdateFolioData_ShouldReturnOk_WhenRequestIsValidAndDataExists()
    {
        // Arrange
        var request = new FolioUpdateRequestDto
        {
            FechaConsulta = DateTime.UtcNow,
            Facturas = new List<FolioUpdateItemDto> { new FolioUpdateItemDto { Modulo = "MOD28" } },
            NotasCredito = new List<FolioUpdateItemDto>()
        };
        
        // Act
        var result = await _controller.UpdateFolioData(request);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        _mockHistoryRepo.Verify(r => r.AddRangeAsync(It.IsAny<IEnumerable<FolioHistory>>()), Times.Once);
        _mockAlertService.Verify(a => a.CheckAlertsAsync(request), Times.Once);
    }
    
     [Fact]
    public async Task UpdateFolioData_ShouldReturnOk_WhenRequestIsValidAndDataIsEmpty()
    {
        // Arrange
        var request = new FolioUpdateRequestDto
        {
            FechaConsulta = DateTime.UtcNow,
            Facturas = new List<FolioUpdateItemDto>(),
            NotasCredito = new List<FolioUpdateItemDto>()
        };
        
        // Act
        var result = await _controller.UpdateFolioData(request);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        _mockHistoryRepo.Verify(r => r.AddRangeAsync(It.IsAny<IEnumerable<FolioHistory>>()), Times.Never); // Should not add if empty
        _mockAlertService.Verify(a => a.CheckAlertsAsync(request), Times.Never); // Should not check alerts if empty
    }

    [Fact]
    public async Task UpdateFolioData_ShouldReturnBadRequest_WhenModelStateIsInvalid()
    {
        // Arrange
        _controller.ModelState.AddModelError("Error", "Test error");
        var request = new FolioUpdateRequestDto(); // Invalid request

        // Act
        var result = await _controller.UpdateFolioData(request);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        _mockHistoryRepo.Verify(r => r.AddRangeAsync(It.IsAny<IEnumerable<FolioHistory>>()), Times.Never);
        _mockAlertService.Verify(a => a.CheckAlertsAsync(It.IsAny<FolioUpdateRequestDto>()), Times.Never);
    }

    [Fact]
    public async Task UpdateFolioData_ShouldReturn500_WhenRepositoryThrowsException()
    {
        // Arrange
        var request = new FolioUpdateRequestDto
        {
            FechaConsulta = DateTime.UtcNow,
            Facturas = new List<FolioUpdateItemDto> { new FolioUpdateItemDto { Modulo = "MOD28" } }
        };
        _mockHistoryRepo.Setup(r => r.AddRangeAsync(It.IsAny<IEnumerable<FolioHistory>>()))
                        .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.UpdateFolioData(request);

        // Assert
        result.Should().BeOfType<ObjectResult>();
        (result as ObjectResult)?.StatusCode.Should().Be(500);
        _mockAlertService.Verify(a => a.CheckAlertsAsync(It.IsAny<FolioUpdateRequestDto>()), Times.Never); // Alert check shouldn't happen if save fails
    }

    // --- GetFolioSummary Tests ---
    [Fact]
    public async Task GetFolioSummary_ShouldReturnOkWithSummary_WhenDataExists()
    {
        // Arrange
        var historyData = new List<FolioHistory>
        {
            new FolioHistory { Modulo = "MOD28", FoliosDisponibles = 100 },
            new FolioHistory { Modulo = "MOD28", FoliosDisponibles = 50 },
            new FolioHistory { Modulo = "MOD29", FoliosDisponibles = 20 }
        };
        _mockHistoryRepo.Setup(r => r.GetLatestAsync()).ReturnsAsync(historyData);

        // Act
        var result = await _controller.GetFolioSummary();

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        var summaryList = okResult?.Value as List<FolioSummaryDto>; 
        summaryList.Should().NotBeNull();
        summaryList.Should().HaveCount(2);
        summaryList?.FirstOrDefault(s => s.DocumentType == "Invoice")?.TotalAvailableFolios.Should().Be(150);
        summaryList?.FirstOrDefault(s => s.DocumentType == "Credit Note")?.TotalAvailableFolios.Should().Be(20);
    }

    // --- GetInvoiceFolios Tests ---
    [Fact]
    public async Task GetInvoiceFolios_ShouldReturnOkWithData_WhenDataExists()
    {
        // Arrange
        var historyData = new List<FolioHistory>
        {
            new FolioHistory { Modulo = "MOD28", FoliosDisponibles = 100, FolioInicio = 1, FolioFin = 100 }
        };
        _mockHistoryRepo.Setup(r => r.GetLatestByModuleAsync("MOD28")).ReturnsAsync(historyData);
        
        // Act
        var result = await _controller.GetInvoiceFolios();

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        var seriesList = okResult?.Value as List<FolioSeriesDto>; 
        seriesList.Should().NotBeNull();
        seriesList.Should().HaveCount(1);
        seriesList?.First().Modulo.Should().Be("MOD28");
    }
    
    // --- GetCreditNoteFolios Tests ---
        [Fact]
    public async Task GetCreditNoteFolios_ShouldReturnOkWithData_WhenDataExists()
    {
        // Arrange
        var historyData = new List<FolioHistory>
        {
            new FolioHistory { Modulo = "MOD29", FoliosDisponibles = 50, FolioInicio = 501, FolioFin = 600 }
        };
        _mockHistoryRepo.Setup(r => r.GetLatestByModuleAsync("MOD29")).ReturnsAsync(historyData);
        
        // Act
        var result = await _controller.GetCreditNoteFolios();

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        var seriesList = okResult?.Value as List<FolioSeriesDto>; 
        seriesList.Should().NotBeNull();
        seriesList.Should().HaveCount(1);
        seriesList?.First().Modulo.Should().Be("MOD29");
    }
} 