using FolioMonitor.API.Repositories;
using FolioMonitor.Core.Models;
using FolioMonitor.Tests.Helpers;
using System.Threading.Tasks;
using Xunit;
using FluentAssertions;
using System.Collections.Generic;
using System.Linq;
using System;
using Microsoft.EntityFrameworkCore;

namespace FolioMonitor.Tests.API.Repositories;

public class FolioHistoryRepositoryTests : IDisposable
{
    private readonly FolioMonitor.API.Data.ApplicationDbContext _context;
    private readonly FolioHistoryRepository _repository;

    public FolioHistoryRepositoryTests()
    {
        _context = DbContextHelper.GetInMemoryDbContext();
        _repository = new FolioHistoryRepository(_context);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    [Fact]
    public async Task AddRangeAsync_ShouldAddMultipleHistoryEntries()
    {
        // Arrange
        var timestamp = DateTime.UtcNow;
        var histories = new List<FolioHistory>
        {
            new FolioHistory { FolioInicio = 1, FolioFin = 100, Modulo = "MOD28", FoliosDisponibles = 10, Timestamp = timestamp },
            new FolioHistory { FolioInicio = 101, FolioFin = 200, Modulo = "MOD28", FoliosDisponibles = 50, Timestamp = timestamp },
            new FolioHistory { FolioInicio = 500, FolioFin = 600, Modulo = "MOD29", FoliosDisponibles = 20, Timestamp = timestamp }
        };

        // Act
        await _repository.AddRangeAsync(histories);

        // Assert
        _context.FolioHistories.Should().HaveCount(3);
        _context.FolioHistories.Should().BeEquivalentTo(histories, options => options.Excluding(h => h.Id)); // Exclude ID as it's generated
    }

    // [Fact] // Skip: FromSqlRaw not supported by InMemory provider
    // public async Task GetLatestByModuleAsync_ShouldReturnLatestEntriesForModule()
    // { ... test body ... }

    // [Fact] // Skip: FromSqlRaw not supported by InMemory provider
    // public async Task GetLatestAsync_ShouldReturnLatestEntriesForAllModulesAndStores()
    // { ... test body ... }

    // --- GetHistoryAsync Tests ---

    private async Task SeedHistoryDataForFiltering() // Helper for history tests
    {
        var data = new List<FolioHistory>
        {
            // MOD28 Data
            new FolioHistory { FolioInicio = 1, FolioFin = 100, Modulo = "MOD28", Timestamp = new DateTime(2024, 4, 10, 10, 0, 0, DateTimeKind.Utc), FoliosDisponibles = 50 },
            new FolioHistory { FolioInicio = 1, FolioFin = 100, Modulo = "MOD28", Timestamp = new DateTime(2024, 4, 11, 10, 0, 0, DateTimeKind.Utc), FoliosDisponibles = 40 }, // Day 11
            new FolioHistory { FolioInicio = 1, FolioFin = 100, Modulo = "MOD28", Timestamp = new DateTime(2024, 4, 12, 10, 0, 0, DateTimeKind.Utc), FoliosDisponibles = 30 }, // Day 12
            new FolioHistory { FolioInicio = 101, FolioFin = 200, Modulo = "MOD28", Timestamp = new DateTime(2024, 4, 11, 11, 0, 0, DateTimeKind.Utc), FoliosDisponibles = 90 }, // Day 11
            
            // MOD29 Data
            new FolioHistory { FolioInicio = 500, FolioFin = 600, Modulo = "MOD29", Timestamp = new DateTime(2024, 4, 11, 12, 0, 0, DateTimeKind.Utc), FoliosDisponibles = 80 }, // Day 11
            new FolioHistory { FolioInicio = 500, FolioFin = 600, Modulo = "MOD29", Timestamp = new DateTime(2024, 4, 13, 10, 0, 0, DateTimeKind.Utc), FoliosDisponibles = 70 }  // Day 13
        };
        await _context.FolioHistories.AddRangeAsync(data);
        await _context.SaveChangesAsync();
        DbContextHelper.DetachAllEntities(_context);
    }

    [Fact]
    public async Task GetHistoryAsync_ShouldReturnAll_WhenNoFilters()
    {
        // Arrange
        await SeedHistoryDataForFiltering();

        // Act
        var result = await _repository.GetHistoryAsync(null, null, null, 1, 10); // Page size 10

        // Assert
        result.Should().HaveCount(6); // All seeded items
    }

    [Fact]
    public async Task GetHistoryAsync_ShouldFilterByModule()
    {
        // Arrange
        await SeedHistoryDataForFiltering();

        // Act
        var result = await _repository.GetHistoryAsync("MOD29", null, null, 1, 10);

        // Assert
        result.Should().HaveCount(2);
        result.Should().OnlyContain(h => h.Modulo == "MOD29");
    }

    [Fact]
    public async Task GetHistoryAsync_ShouldFilterByStartDate()
    {
        // Arrange
        await SeedHistoryDataForFiltering();
        var startDate = new DateTime(2024, 4, 12); // Should include day 12 and 13

        // Act
        var result = await _repository.GetHistoryAsync(null, startDate, null, 1, 10);

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain(h => h.Timestamp.Date == startDate.Date); // MOD28 Day 12
        result.Should().Contain(h => h.Timestamp.Date == new DateTime(2024, 4, 13)); // MOD29 Day 13
    }

    [Fact]
    public async Task GetHistoryAsync_ShouldFilterByEndDate()
    {
        // Arrange
        await SeedHistoryDataForFiltering();
        var endDate = new DateTime(2024, 4, 11); // Should include day 10 and 11

        // Act
        var result = await _repository.GetHistoryAsync(null, null, endDate, 1, 10);

        // Assert
        result.Should().HaveCount(4);
        result.Should().OnlyContain(h => h.Timestamp.Date <= endDate.Date);
    }

    [Fact]
    public async Task GetHistoryAsync_ShouldFilterByDateRangeAndModule()
    {
        // Arrange
        await SeedHistoryDataForFiltering();
        var startDate = new DateTime(2024, 4, 11);
        var endDate = new DateTime(2024, 4, 11);

        // Act
        var result = await _repository.GetHistoryAsync("MOD28", startDate, endDate, 1, 10);

        // Assert
        result.Should().HaveCount(2); // MOD28 entries for Day 11
        result.Should().OnlyContain(h => h.Modulo == "MOD28" && h.Timestamp.Date == startDate.Date);
    }

    [Fact]
    public async Task GetHistoryAsync_ShouldApplyPagination()
    {
        // Arrange
        await SeedHistoryDataForFiltering();

        // Act
        var page1 = await _repository.GetHistoryAsync(null, null, null, 1, 4); // Page 1, Size 4
        var page2 = await _repository.GetHistoryAsync(null, null, null, 2, 4); // Page 2, Size 4

        // Assert
        page1.Should().HaveCount(4);
        page2.Should().HaveCount(2); // Remaining 2 items
        
        // Verify order is descending by timestamp (default)
        page1.Should().BeInDescendingOrder(h => h.Timestamp);
        page2.Should().BeInDescendingOrder(h => h.Timestamp);
        page1.First().Timestamp.Should().Be(new DateTime(2024, 4, 13, 10, 0, 0, DateTimeKind.Utc)); // Latest
    }

    // --- DeleteOldEntriesAsync Tests ---

    [Fact]
    public async Task DeleteOldEntriesAsync_ShouldDeleteRecordsOlderThanRetentionDays()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var data = new List<FolioHistory>
        {
            new FolioHistory { Modulo="MOD28", Timestamp = now.AddDays(-10) }, // Keep
            new FolioHistory { Modulo="MOD28", Timestamp = now.AddDays(-31) }, // Delete
            new FolioHistory { Modulo="MOD29", Timestamp = now.AddDays(-29) }, // Keep
            new FolioHistory { Modulo="MOD29", Timestamp = now.AddDays(-30) }, // Keep (exactly 30 days old)
            new FolioHistory { Modulo="MOD28", Timestamp = now.AddDays(-60) }  // Delete
        };
        await _context.FolioHistories.AddRangeAsync(data);
        await _context.SaveChangesAsync();
        DbContextHelper.DetachAllEntities(_context); // Ensure counts are read fresh
        var initialCount = await _context.FolioHistories.CountAsync();
        initialCount.Should().Be(5);

        var retentionDays = 30;

        // Act
        var deletedCount = await _repository.DeleteOldEntriesAsync(retentionDays);

        // Assert
        deletedCount.Should().Be(2);
        var remainingCount = await _context.FolioHistories.CountAsync();
        remainingCount.Should().Be(3);
        var remaining = await _context.FolioHistories.ToListAsync();
        remaining.Should().OnlyContain(h => h.Timestamp >= now.AddDays(-retentionDays));
    }

    [Fact]
    public async Task DeleteOldEntriesAsync_ShouldReturnZero_WhenRetentionDaysIsZeroOrNegative()
    {
        // Arrange
        var now = DateTime.UtcNow;
        await _context.FolioHistories.AddAsync(new FolioHistory { Modulo="MOD28", Timestamp = now.AddDays(-10) });
        await _context.SaveChangesAsync();
        var initialCount = await _context.FolioHistories.CountAsync();
        initialCount.Should().Be(1);

        // Act
        var deletedCountZero = await _repository.DeleteOldEntriesAsync(0);
        var deletedCountNegative = await _repository.DeleteOldEntriesAsync(-5);

        // Assert
        deletedCountZero.Should().Be(0);
        deletedCountNegative.Should().Be(0);
        var finalCount = await _context.FolioHistories.CountAsync();
        finalCount.Should().Be(1); // No records deleted
    }
    
    [Fact]
    public async Task DeleteOldEntriesAsync_ShouldReturnZero_WhenNoOldRecordsExist()
    {
        // Arrange
        var now = DateTime.UtcNow;
        await _context.FolioHistories.AddAsync(new FolioHistory { Modulo="MOD28", Timestamp = now.AddDays(-10) });
        await _context.SaveChangesAsync();
        var initialCount = await _context.FolioHistories.CountAsync();
        initialCount.Should().Be(1);

        // Act
        var deletedCount = await _repository.DeleteOldEntriesAsync(30); // 30 day retention

        // Assert
        deletedCount.Should().Be(0);
        var finalCount = await _context.FolioHistories.CountAsync();
        finalCount.Should().Be(1);
    }
} 