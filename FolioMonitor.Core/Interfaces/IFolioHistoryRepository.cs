using FolioMonitor.Core.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FolioMonitor.Core.Interfaces;

/// <summary>
/// Interface for accessing folio history data.
/// </summary>
public interface IFolioHistoryRepository
{
    Task AddRangeAsync(IEnumerable<FolioHistory> folioHistories);
    Task<IEnumerable<FolioHistory>> GetLatestAsync(); // Gets the most recent record for each unique series
    Task<IEnumerable<FolioHistory>> GetLatestByModuleAsync(string module);
    
    /// <summary>
    /// Gets historical folio records, optionally filtered by module and date range, with pagination.
    /// </summary>
    /// <param name="module">Optional module (MOD28/MOD29) to filter by.</param>
    /// <param name="startDate">Optional start date (inclusive) to filter by.</param>
    /// <param name="endDate">Optional end date (inclusive) to filter by.</param>
    /// <param name="pageNumber">Page number for pagination (1-based).</param>
    /// <param name="pageSize">Number of records per page.</param>
    /// <returns>A collection of historical folio records.</returns>
    Task<IEnumerable<FolioHistory>> GetHistoryAsync(string? module, DateTime? startDate, DateTime? endDate, int pageNumber = 1, int pageSize = 100);
    
    /// <summary>
    /// Deletes folio history records older than the specified retention period.
    /// </summary>
    /// <param name="retentionDays">The maximum age in days for records to keep.</param>
    /// <returns>The number of records deleted.</returns>
    Task<int> DeleteOldEntriesAsync(int retentionDays);
    // Add other query methods as needed (e.g., for Task 8 - filtering)
} 