using FolioMonitor.Core.Interfaces;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace FolioMonitor.API.Services;

/// <summary>
/// Dummy implementation of INotificationService for Task 6.
/// Replace this with the actual implementation in Task 7.
/// </summary>
public class DummyNotificationService : INotificationService
{
    private readonly ILogger<DummyNotificationService> _logger;

    public DummyNotificationService(ILogger<DummyNotificationService> logger)
    {
        _logger = logger;
    }

    public Task SendAlertAsync(AlertInfo alertInfo)
    {
        _logger.LogWarning("[DUMMY NOTIFICATION] Would send {Level} alert: {Message}", 
                           alertInfo.Level, alertInfo.Message);
        
        // Simulate async operation
        return Task.CompletedTask;
    }
} 