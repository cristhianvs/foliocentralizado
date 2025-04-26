using FolioMonitor.API.Repositories;
using FolioMonitor.Core.Models;
using FolioMonitor.Tests.Helpers;
using System.Threading.Tasks;
using Xunit;
using FluentAssertions; // Using FluentAssertions for more readable asserts
using System.Linq;

namespace FolioMonitor.Tests.API.Repositories;

public class ConfigurationRepositoryTests : IDisposable
{
    private readonly FolioMonitor.API.Data.ApplicationDbContext _context;
    private readonly ConfigurationRepository _repository;

    public ConfigurationRepositoryTests()
    {
        _context = DbContextHelper.GetInMemoryDbContext();
        _repository = new ConfigurationRepository(_context);
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    [Fact]
    public async Task AddAsync_ShouldAddConfiguration()
    {
        // Arrange
        var config = new Configuration { Key = "TestKey", Value = "TestValue" };

        // Act
        await _repository.AddAsync(config);

        // Assert
        var addedConfig = await _context.Configurations.FindAsync("TestKey");
        addedConfig.Should().NotBeNull();
        addedConfig.Should().BeEquivalentTo(config);
    }

    [Fact]
    public async Task GetByKeyAsync_ShouldReturnConfiguration_WhenExists()
    {
        // Arrange
        var key = "ExistingKey";
        var expectedConfig = new Configuration { Key = key, Value = "ExistingValue" };
        await _context.Configurations.AddAsync(expectedConfig);
        await _context.SaveChangesAsync();
        DbContextHelper.DetachAllEntities(_context); // Detach to ensure fresh read

        // Act
        var result = await _repository.GetByKeyAsync(key);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEquivalentTo(expectedConfig);
    }

    [Fact]
    public async Task GetByKeyAsync_ShouldReturnNull_WhenNotExists()
    {
        // Arrange
        var key = "NonExistingKey";

        // Act
        var result = await _repository.GetByKeyAsync(key);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnAllConfigurations()
    {
        // Arrange
        var configs = new[]
        {
            new Configuration { Key = "Key1", Value = "Val1" },
            new Configuration { Key = "Key2", Value = "Val2" }
        };
        await _context.Configurations.AddRangeAsync(configs);
        await _context.SaveChangesAsync();
        DbContextHelper.DetachAllEntities(_context); 

        // Act
        var result = await _repository.GetAllAsync();

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result.Should().BeEquivalentTo(configs);
    }

    [Fact]
    public async Task UpdateAsync_ShouldUpdateConfiguration()
    {
        // Arrange
        var key = "UpdateKey";
        var initialConfig = new Configuration { Key = key, Value = "InitialValue", Description = "Initial" };
        await _context.Configurations.AddAsync(initialConfig);
        await _context.SaveChangesAsync();
        // DbContextHelper.DetachAllEntities(_context); // No longer needed with fetch-and-update approach

        var configWithUpdates = new Configuration { Key = key, Value = "UpdatedValue", Description = "Updated" };

        // Act
        await _repository.UpdateAsync(configWithUpdates);
        // DbContextHelper.DetachAllEntities(_context); 

        // Assert
        var result = await _context.Configurations.FindAsync(key);
        result.Should().NotBeNull();
        result.Value.Should().Be("UpdatedValue");
        result.Description.Should().Be("Updated");
    }
} 