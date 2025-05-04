using FolioMonitor.API.Data;
using FolioMonitor.Core.Interfaces;
using FolioMonitor.Core.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FolioMonitor.API.Repositories;

public class FolioHistoryRepository : IFolioHistoryRepository
{
    private readonly ApplicationDbContext _context;

    public FolioHistoryRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task AddRangeAsync(IEnumerable<FolioHistory> folioHistories)
    {
        await _context.FolioHistories.AddRangeAsync(folioHistories);
        await _context.SaveChangesAsync();
    }

    public async Task<IEnumerable<FolioHistory>> GetLatestAsync()
    {
        // Try LINQ approach: Group to find max timestamp, then join back
        var latestTimestampsQuery = _context.FolioHistories
            .GroupBy(fh => new { fh.CodigoSucursal, fh.Modulo, fh.FolioInicio, fh.FolioFin })
            .Select(g => new 
            {
                g.Key.CodigoSucursal,
                g.Key.Modulo,
                g.Key.FolioInicio,
                g.Key.FolioFin,
                MaxTimestamp = g.Max(item => item.Timestamp)
            });

        var latestEntries = await _context.FolioHistories
            .Join(
                latestTimestampsQuery, 
                history => new { history.CodigoSucursal, history.Modulo, history.FolioInicio, history.FolioFin, history.Timestamp }, 
                grouped => new { grouped.CodigoSucursal, grouped.Modulo, grouped.FolioInicio, grouped.FolioFin, Timestamp = grouped.MaxTimestamp }, 
                (history, grouped) => history 
            )
            .AsNoTracking()
            .ToListAsync();

        return latestEntries;
    }

    public async Task<IEnumerable<FolioHistory>> GetLatestByModuleAsync(string module)
    {
        var latestTimestampsQuery = _context.FolioHistories
           .Where(fh => fh.Modulo == module)
           .GroupBy(fh => new { fh.CodigoSucursal, fh.Modulo, fh.FolioInicio, fh.FolioFin })
           .Select(g => new 
           {
               g.Key.CodigoSucursal,
               g.Key.Modulo,
               g.Key.FolioInicio,
               g.Key.FolioFin,
               MaxTimestamp = g.Max(item => item.Timestamp)
           });

        var latestEntries = await _context.FolioHistories
           .Where(fh => fh.Modulo == module)
           .Join(
               latestTimestampsQuery, 
               history => new { history.CodigoSucursal, history.Modulo, history.FolioInicio, history.FolioFin, history.Timestamp }, 
               grouped => new { grouped.CodigoSucursal, grouped.Modulo, grouped.FolioInicio, grouped.FolioFin, Timestamp = grouped.MaxTimestamp }, 
               (history, grouped) => history
           )
           .AsNoTracking()
           .ToListAsync();

        return latestEntries;
    }

    public async Task<IEnumerable<FolioHistory>> GetHistoryAsync(string? module, DateTime? startDate, DateTime? endDate, int pageNumber = 1, int pageSize = 100)
    {
        var query = _context.FolioHistories.AsQueryable();

        if (!string.IsNullOrEmpty(module))
        {
            query = query.Where(h => h.Modulo == module);
        }

        if (startDate.HasValue)
        {
            // Ensure the comparison includes the whole day
            var startOfDay = startDate.Value.Date;
            query = query.Where(h => h.Timestamp >= startOfDay); 
        }

        if (endDate.HasValue)
        {
             // Ensure the comparison includes the whole day
            var endOfDay = endDate.Value.Date.AddDays(1).AddTicks(-1); // End of the specified day
            query = query.Where(h => h.Timestamp <= endOfDay);
        }

        // Apply pagination
        // Ensure pageNumber is at least 1 and pageSize is positive
        pageNumber = Math.Max(1, pageNumber);
        pageSize = Math.Max(1, pageSize);
        
        query = query.OrderByDescending(h => h.Timestamp) // Order by timestamp descending by default
                     .Skip((pageNumber - 1) * pageSize)
                     .Take(pageSize);

        return await query.ToListAsync();
    }

    public async Task<int> DeleteOldEntriesAsync(int retentionDays)
    {
        if (retentionDays <= 0) 
        {
            return 0; 
        }
        
        // Cutoff date is the start of the day 'retentionDays' ago
        var cutoffDate = DateTime.UtcNow.Date.AddDays(-retentionDays);
        
        // Fetch entities where the timestamp is strictly before the cutoff date
        var entriesToDelete = await _context.FolioHistories
            .Where(h => h.Timestamp < cutoffDate)
            .ToListAsync(); // Materialize the list

        if (!entriesToDelete.Any())
        {
            return 0;
        }

        _context.FolioHistories.RemoveRange(entriesToDelete);
        var deletedCount = await _context.SaveChangesAsync(); // SaveChanges returns the count
            
        return deletedCount;
    }

    // Implementation for other query methods (Task 8) will go here
} 