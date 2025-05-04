using FolioMonitor.Core.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

namespace FolioMonitor.API.Repositories;

public class LatestFolioSnapshotRepository
{
    private readonly Data.ApplicationDbContext _context;
    public LatestFolioSnapshotRepository(Data.ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task UpsertRangeAsync(IEnumerable<LatestFolioSnapshot> snapshots)
    {
        foreach (var snapshot in snapshots)
        {
            var existing = await _context.LatestFolioSnapshots.FindAsync(snapshot.CodigoSucursal, snapshot.Modulo, snapshot.FolioInicio, snapshot.FolioFin);
            if (existing != null)
            {
                // Update all fields
                _context.Entry(existing).CurrentValues.SetValues(snapshot);
            }
            else
            {
                await _context.LatestFolioSnapshots.AddAsync(snapshot);
            }
        }
        await _context.SaveChangesAsync();
    }

    public async Task<List<LatestFolioSnapshot>> GetAllAsync()
    {
        return await _context.LatestFolioSnapshots.AsNoTracking().ToListAsync();
    }

    public async Task<List<LatestFolioSnapshot>> GetByModuleAsync(string module)
    {
        return await _context.LatestFolioSnapshots.Where(s => s.Modulo == module).AsNoTracking().ToListAsync();
    }
} 