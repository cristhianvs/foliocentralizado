using FolioMonitor.API.Data;
using FolioMonitor.Core.Interfaces;
using FolioMonitor.Core.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FolioMonitor.API.Repositories;

public class ConfigurationRepository : IConfigurationRepository
{
    private readonly ApplicationDbContext _context;

    public ConfigurationRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Configuration?> GetByKeyAsync(string key)
    {
        return await _context.Configurations.FindAsync(key);
    }

    public async Task<IEnumerable<Configuration>> GetAllAsync()
    {
        return await _context.Configurations.ToListAsync();
    }

    public async Task UpdateAsync(Configuration configuration)
    {
        var existingConfig = await _context.Configurations.FindAsync(configuration.Key);
        if (existingConfig != null)
        {
            // Update properties of the tracked entity
            existingConfig.Value = configuration.Value;
            existingConfig.Description = configuration.Description;
            // _context.Configurations.Update(existingConfig); // No need to call Update() if already tracking
            await _context.SaveChangesAsync();
        }
        // Optional: else throw an exception or log a warning if trying to update non-existent key?
    }

    public async Task AddAsync(Configuration configuration)
    {
        await _context.Configurations.AddAsync(configuration);
        await _context.SaveChangesAsync();
    }
} 