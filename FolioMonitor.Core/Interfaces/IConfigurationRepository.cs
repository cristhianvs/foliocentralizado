using FolioMonitor.Core.Models;
using System.Threading.Tasks;

namespace FolioMonitor.Core.Interfaces;

/// <summary>
/// Interface for accessing configuration data.
/// </summary>
public interface IConfigurationRepository
{
    Task<Configuration?> GetByKeyAsync(string key);
    Task<IEnumerable<Configuration>> GetAllAsync();
    Task UpdateAsync(Configuration configuration);
    Task AddAsync(Configuration configuration);
} 