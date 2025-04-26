using FolioMonitor.Core.Models.DTOs;
using FolioMonitor.Core.Interfaces;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FolioMonitor.API.Services;

public class AlertService : IAlertService
{
    private readonly IConfigurationRepository _configRepo;
    private readonly ILogger<AlertService> _logger;

    // Configuration keys
    private const string WarningThresholdKey = "AlertThreshold_Warning";
    private const string CriticalThresholdKey = "AlertThreshold_Critical";
    private const int DefaultWarningThreshold = 100; // Default if not configured
    private const int DefaultCriticalThreshold = 20;  // Default if not configured

    public AlertService(
        IConfigurationRepository configRepo,
        ILogger<AlertService> logger)
    {
        _configRepo = configRepo;
        _logger = logger;
    }

    public async Task CheckAlertsAsync(FolioUpdateRequestDto updateRequest)
    {
        _logger.LogInformation("Checking for alerts based on received folio data.");

        // Get thresholds from config (with defaults)
        int warningThreshold = await GetThresholdAsync(WarningThresholdKey, DefaultWarningThreshold);
        int criticalThreshold = await GetThresholdAsync(CriticalThresholdKey, DefaultCriticalThreshold);

        var allSeries = updateRequest.Facturas.Concat(updateRequest.NotasCredito);
        var alertsTriggeredCount = 0; // Track how many alerts would have been sent

        foreach (var series in allSeries)
        {
            string seriesIdentifier = $"{series.Modulo}-{series.CodigoSucursal}-{series.FolioInicio}-{series.FolioFin}"; // Added CodigoSucursal for unique ID

            if (series.FoliosDisponibles <= criticalThreshold)
            {
                 _logger.LogWarning("CRITICAL alert threshold reached for series {SeriesIdentifier}. Available: {Available}, Threshold: {Threshold}", 
                                     seriesIdentifier, series.FoliosDisponibles, criticalThreshold);
                 alertsTriggeredCount++;
            }
            else if (series.FoliosDisponibles <= warningThreshold)
            {
                 _logger.LogWarning("WARNING alert threshold reached for series {SeriesIdentifier}. Available: {Available}, Threshold: {Threshold}", 
                                     seriesIdentifier, series.FoliosDisponibles, warningThreshold);
                 alertsTriggeredCount++;
            }
        }
        _logger.LogInformation("Alert check completed. {Count} alerts were triggered (notifications disabled).", alertsTriggeredCount);
    }

    private async Task<int> GetThresholdAsync(string key, int defaultValue)
    {
        try
        {
            var config = await _configRepo.GetByKeyAsync(key);
            if (config != null && int.TryParse(config.Value, out int value))
            {
                return value;
            }
            _logger.LogWarning("Configuration key {Key} not found or not a valid integer. Using default value: {DefaultValue}", key, defaultValue);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving configuration for key {Key}. Using default value: {DefaultValue}", key, defaultValue);
        }
        return defaultValue;
    }
} 