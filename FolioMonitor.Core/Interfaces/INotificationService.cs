using FolioMonitor.Core.Models; // For AlertInfo perhaps, define later
using System.Threading.Tasks;

namespace FolioMonitor.Core.Interfaces;

// Placeholder for Task 7
public enum AlertLevel
{
    Warning,
    Critical
}

/// <summary>
/// Represents the information needed to send an alert.
/// </summary>
public class AlertInfo
{
    public AlertLevel Level { get; set; }
    public string Module { get; set; } = string.Empty;
    public int FolioInicio { get; set; }
    public int FolioFin { get; set; }
    public int? FolioActual { get; set; }
    public int FoliosDisponibles { get; set; }
    public int Threshold { get; set; } 
    public string Message { get; set; } = string.Empty;
}

/// <summary>
/// Service responsible for sending notifications (e.g., email, SMS).
/// Placeholder for Task 7.
/// </summary>
public interface INotificationService
{
    Task SendAlertAsync(AlertInfo alertInfo);
} 