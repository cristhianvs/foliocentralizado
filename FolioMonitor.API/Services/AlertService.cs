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
    private readonly INotificationService _notificationService; // Placeholder (Task 7)
    private readonly ILogger<AlertService> _logger;

    // Configuration keys
    private const string WarningThresholdKey = "AlertThreshold_Warning";
    private const string CriticalThresholdKey = "AlertThreshold_Critical";
    private const int DefaultWarningThreshold = 100; // Default if not configured
    private const int DefaultCriticalThreshold = 20;  // Default if not configured

    public AlertService(
        IConfigurationRepository configRepo,
        INotificationService notificationService, 
        ILogger<AlertService> logger)
    {
        _configRepo = configRepo;
        _notificationService = notificationService; 
        _logger = logger;
    }

    public async Task CheckAlertsAsync(FolioUpdateRequestDto updateRequest)
    {
        _logger.LogInformation("Checking for alerts based on received folio data.");

        // Get thresholds from config (with defaults)
        int warningThreshold = await GetThresholdAsync(WarningThresholdKey, DefaultWarningThreshold);
        int criticalThreshold = await GetThresholdAsync(CriticalThresholdKey, DefaultCriticalThreshold);

        var allSeries = updateRequest.Facturas.Concat(updateRequest.NotasCredito);
        
        // Simple state management: Keep track of alerts sent in this run to avoid duplicates per run
        var alertsSentThisRun = new HashSet<string>(); 

        foreach (var series in allSeries)
        {
            string seriesIdentifier = $"{series.Modulo}-{series.FolioInicio}-{series.FolioFin}";

            if (series.FoliosDisponibles <= criticalThreshold)
            {
                if (alertsSentThisRun.Add(seriesIdentifier + "-Critical")) // Try to add, returns true if added (not a duplicate)
                {
                    _logger.LogWarning("CRITICAL alert threshold reached for series {SeriesIdentifier}. Available: {Available}, Threshold: {Threshold}", 
                                     seriesIdentifier, series.FoliosDisponibles, criticalThreshold);
                    var alertInfo = CreateAlertInfo(series, AlertLevel.Critical, criticalThreshold);
                    await _notificationService.SendAlertAsync(alertInfo); // Call placeholder
                }
            }
            else if (series.FoliosDisponibles <= warningThreshold)
            {
                 if (alertsSentThisRun.Add(seriesIdentifier + "-Warning"))
                 {
                    _logger.LogWarning("WARNING alert threshold reached for series {SeriesIdentifier}. Available: {Available}, Threshold: {Threshold}", 
                                     seriesIdentifier, series.FoliosDisponibles, warningThreshold);
                    var alertInfo = CreateAlertInfo(series, AlertLevel.Warning, warningThreshold);
                    await _notificationService.SendAlertAsync(alertInfo); // Call placeholder
                 }
            }
        }
        _logger.LogInformation("Alert check completed. Sent {Count} alerts this run.", alertsSentThisRun.Count);
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

    private AlertInfo CreateAlertInfo(FolioUpdateItemDto series, AlertLevel level, int threshold)
    {
        return new AlertInfo
        {
            Level = level,
            Module = series.Modulo,
            FolioInicio = series.FolioInicio,
            FolioFin = series.FolioFin,
            FolioActual = series.FolioActual,
            FoliosDisponibles = series.FoliosDisponibles,
            Threshold = threshold,
            Message = $"{level} threshold reached for {series.Modulo} (Start: {series.FolioInicio}, End: {series.FolioFin}). Available: {series.FoliosDisponibles} (Threshold: {threshold})"
        };
    }
} 