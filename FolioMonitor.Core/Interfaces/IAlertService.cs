using FolioMonitor.Core.Models.DTOs; // Updated namespace
using System.Threading.Tasks;

namespace FolioMonitor.Core.Interfaces;

/// <summary>
/// Service responsible for checking folio data against alert thresholds.
/// </summary>
public interface IAlertService
{
    /// <summary>
    /// Checks the incoming folio data against configured thresholds and triggers notifications if necessary.
    /// </summary>
    /// <param name="updateRequest">The folio data received from the worker.</param>
    Task CheckAlertsAsync(FolioUpdateRequestDto updateRequest);
} 