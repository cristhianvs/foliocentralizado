using FolioMonitor.API;
using FolioMonitor.API.Data;
using FolioMonitor.Core.Interfaces;
using FolioMonitor.Core.Models;
using FolioMonitor.Core.Models.DTOs;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Moq;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Xunit;
using FluentAssertions;
using System;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore.Storage;

namespace FolioMonitor.Tests.API.Integration;

// Custom WebApplicationFactory to override services for testing
public class FolioApiTestFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    // Use Mock for IAlertService to verify it gets called, without needing INotificationService setup
    public Mock<IAlertService> MockAlertService { get; } = new Mock<IAlertService>();
    
    // Use a shared SQLite connection that is kept open for the test lifetime
    private readonly SqliteConnection _connection = new SqliteConnection("DataSource=:memory:");
    private IDbContextTransaction? _transaction; // Transaction for isolation if needed

    protected override IHost CreateHost(IHostBuilder builder)
    {
        // Set the environment to Testing
        builder.UseEnvironment("Testing"); 

        // Configure AppConfiguration - Only override what's needed for tests
        builder.ConfigureAppConfiguration((context, configBuilder) =>
        {
            configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
            {
                // ["ApiKey"] = FoliosApiIntegrationTests.ApiKey, // Removed
                ["RetentionPolicy:RetentionDays"] = "30"
                // [TestingEnvironmentFlag] = "true" // Removed
            });
        });
        
        builder.ConfigureServices(services =>
        {
            // Remove original DbContextOptions IF THEY EXIST
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));
            if (descriptor != null) { services.Remove(descriptor); }
            
            // Add DbContext using SQLite in-memory database.
            services.AddDbContext<ApplicationDbContext>(options =>
            {
                options.UseSqlite(_connection); // Use the shared connection
            });
            
            // Replace IAlertService with our mock instance
            services.AddScoped<IAlertService>(_ => MockAlertService.Object);
        });

        return base.CreateHost(builder);
    }

    public async Task InitializeAsync()
    {
        await _connection.OpenAsync(); // Open the connection
        
        // Create the schema for the new connection
        using var scope = Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await context.Database.EnsureCreatedAsync();
        
        // Optional: Start a transaction for test isolation (though less common for in-memory)
        // _transaction = await context.Database.BeginTransactionAsync(); 
    }

    public new async Task DisposeAsync()
    {
        // Optional: Rollback transaction
        // if (_transaction != null) await _transaction.RollbackAsync();
        
        await _connection.CloseAsync(); // Close the connection
        await base.DisposeAsync();
    }
}

public class FoliosApiIntegrationTests : IClassFixture<FolioApiTestFactory> // Change back to public
{
    private readonly HttpClient _client;
    private readonly FolioApiTestFactory _factory;
    private readonly Mock<IAlertService> _mockAlertService;
    // internal const string ApiKey = "TEST_INTEGRATION_KEY"; // No longer needed for middleware
    // private const string ApiKeyHeader = "X-API-KEY"; // No longer needed

    public FoliosApiIntegrationTests(FolioApiTestFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
        _mockAlertService = factory.MockAlertService;

        // _client.DefaultRequestHeaders.Add(ApiKeyHeader, ApiKey); // No longer needed
        
        // Override the API Key in the factory's configuration for the middleware
        // This is a bit tricky as CreateHost runs before CreateClient
        // A better approach might involve a custom configuration source for tests
        // For now, we assume the middleware will read this key during the request.
        // Note: This doesn't work reliably. Test middleware separately or adjust setup.
    }

    private async Task SeedDatabaseAsync(ApplicationDbContext context)
    {
         // Clear existing data first to avoid key conflicts across tests
         context.FolioHistories.RemoveRange(context.FolioHistories);
         await context.SaveChangesAsync();

         var history = new List<FolioHistory>
         {
             // Store S1
             new FolioHistory { Id=1, CodigoSucursal="S1", FolioInicio=1, FolioFin=100, Modulo="MOD28", FoliosDisponibles=50, Activo=true, FechaRegistro=DateTime.UtcNow.AddDays(-1), Timestamp=DateTime.UtcNow.AddHours(-1) },
             new FolioHistory { Id=2, CodigoSucursal="S1", FolioInicio=501, FolioFin=600, Modulo="MOD29", FoliosDisponibles=25, Activo=true, FechaRegistro=DateTime.UtcNow.AddDays(-1), Timestamp=DateTime.UtcNow.AddHours(-1) },
             new FolioHistory { Id=3, CodigoSucursal="S1", FolioInicio=1, FolioFin=100, Modulo="MOD28", FoliosDisponibles=60, Activo=true, FechaRegistro=DateTime.UtcNow.AddDays(-2), Timestamp=DateTime.UtcNow.AddHours(-24) }, // Older MOD28/S1
             // Store S2
             new FolioHistory { Id=4, CodigoSucursal="S2", FolioInicio=1, FolioFin=100, Modulo="MOD28", FoliosDisponibles=90, Activo=true, FechaRegistro=DateTime.UtcNow.AddDays(-1), Timestamp=DateTime.UtcNow.AddHours(-1) },
         };
         context.FolioHistories.AddRange(history);
         await context.SaveChangesAsync();
    }

    private async Task SeedHistoryDataForFilteringIntegration() // Helper for history integration tests
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        
        // Clear existing data first
        context.FolioHistories.RemoveRange(context.FolioHistories);
        await context.SaveChangesAsync();
        
        var data = new List<FolioHistory>
        {
            new FolioHistory { CodigoSucursal="S1", FolioInicio = 1, FolioFin = 100, Modulo = "MOD28", Timestamp = new DateTime(2024, 4, 10, 10, 0, 0, DateTimeKind.Utc), FoliosDisponibles = 50 },
            new FolioHistory { CodigoSucursal="S1", FolioInicio = 1, FolioFin = 100, Modulo = "MOD28", Timestamp = new DateTime(2024, 4, 11, 10, 0, 0, DateTimeKind.Utc), FoliosDisponibles = 40 }, // Day 11
            new FolioHistory { CodigoSucursal="S2", FolioInicio = 1, FolioFin = 100, Modulo = "MOD28", Timestamp = new DateTime(2024, 4, 12, 10, 0, 0, DateTimeKind.Utc), FoliosDisponibles = 30 }, // Day 12
            new FolioHistory { CodigoSucursal="S1", FolioInicio = 101, FolioFin = 200, Modulo = "MOD28", Timestamp = new DateTime(2024, 4, 11, 11, 0, 0, DateTimeKind.Utc), FoliosDisponibles = 90 }, // Day 11
            new FolioHistory { CodigoSucursal="S1", FolioInicio = 500, FolioFin = 600, Modulo = "MOD29", Timestamp = new DateTime(2024, 4, 11, 12, 0, 0, DateTimeKind.Utc), FoliosDisponibles = 80 }, // Day 11
            new FolioHistory { CodigoSucursal="S2", FolioInicio = 500, FolioFin = 600, Modulo = "MOD29", Timestamp = new DateTime(2024, 4, 13, 10, 0, 0, DateTimeKind.Utc), FoliosDisponibles = 70 }  // Day 13
        };
        context.FolioHistories.AddRange(data);
        await context.SaveChangesAsync();
    }

    // --- POST /api/folios/update Tests ---
    [Fact]
    public async Task PostUpdate_WithValidDataAndKey_ReturnsOkAndSavesDataAndCallsAlertService()
    {
        // Arrange
        var requestDto = new FolioUpdateRequestDto
        {
            FechaConsulta = DateTime.UtcNow,
            Facturas = new List<FolioUpdateItemDto> { new FolioUpdateItemDto { CodigoSucursal="S3", FolioInicio = 1001, FolioFin = 1100, Modulo = "MOD28", FoliosDisponibles = 99, Activo=true } },
            NotasCredito = new List<FolioUpdateItemDto> { new FolioUpdateItemDto { CodigoSucursal="S3", FolioInicio = 2001, FolioFin = 2100, Modulo = "MOD29", FoliosDisponibles = 88, Activo=true } }
        };
        _mockAlertService.Reset(); // Reset mock before test

        // Act
        var response = await _client.PostAsJsonAsync("/api/folios/update", requestDto);

        // Assert
        response.EnsureSuccessStatusCode(); // Status Code 2xx
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        // Verify data persistence
        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var savedFacturas = await context.FolioHistories.Where(h => h.CodigoSucursal=="S3" && h.Modulo == "MOD28" && h.FolioInicio == 1001).ToListAsync();
            savedFacturas.Should().HaveCount(1);
            savedFacturas.First().FoliosDisponibles.Should().Be(99);
            var savedNotas = await context.FolioHistories.Where(h => h.CodigoSucursal=="S3" && h.Modulo == "MOD29" && h.FolioInicio == 2001).ToListAsync();
            savedNotas.Should().HaveCount(1);
            savedNotas.First().FoliosDisponibles.Should().Be(88);
        }
        
        // Verify AlertService was called
        _mockAlertService.Verify(a => a.CheckAlertsAsync(It.IsAny<FolioUpdateRequestDto>()), Times.Once);
    }

    // --- GET /api/folios/summary Tests ---
    [Fact]
    public async Task GetSummary_ReturnsCorrectSummary()
    {
        // Arrange
        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            await SeedDatabaseAsync(context);
        }
        
        // Act
        var response = await _client.GetAsync("/api/folios/summary");
        
        // Assert
        response.EnsureSuccessStatusCode();
        var summary = await response.Content.ReadFromJsonAsync<List<FolioSummaryDto>>();
        summary.Should().NotBeNull();
        summary.Should().HaveCount(2);
        // Summary logic currently sums across all stores
        summary?.FirstOrDefault(s => s.DocumentType == "Invoice")?.TotalAvailableFolios.Should().Be(140); // 50 (S1) + 90 (S2)
        summary?.FirstOrDefault(s => s.DocumentType == "Credit Note")?.TotalAvailableFolios.Should().Be(25); // 25 (S1)
        // Verify series details include store code
        var invoiceSummary = summary?.FirstOrDefault(s => s.DocumentType == "Invoice");
        invoiceSummary?.Series.Should().HaveCount(2); // One from S1, one from S2
        invoiceSummary?.Series.Should().Contain(s => s.CodigoSucursal == "S1" && s.FolioInicio == 1);
        invoiceSummary?.Series.Should().Contain(s => s.CodigoSucursal == "S2" && s.FolioInicio == 1);
    }
    
     // --- GET /api/folios/invoices Tests ---
    [Fact]
    public async Task GetInvoices_ReturnsCorrectInvoiceSeries()
    {
        // Arrange
        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            await SeedDatabaseAsync(context);
        }
        
        // Act
        var response = await _client.GetAsync("/api/folios/invoices");
        
        // Assert
        response.EnsureSuccessStatusCode();
        var series = await response.Content.ReadFromJsonAsync<List<FolioSeriesDto>>();
        series.Should().NotBeNull();
        series.Should().HaveCount(2); // Latest from S1 and S2
        series?.Should().Contain(s => s.CodigoSucursal == "S1" && s.FolioInicio == 1 && s.FoliosDisponibles == 50);
        series?.Should().Contain(s => s.CodigoSucursal == "S2" && s.FolioInicio == 1 && s.FoliosDisponibles == 90);
    }

     // --- GET /api/folios/creditnotes Tests ---
    [Fact]
    public async Task GetCreditNotes_ReturnsCorrectCreditNoteSeries()
    {
         // Arrange
        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            await SeedDatabaseAsync(context);
        }
        
        // Act
        var response = await _client.GetAsync("/api/folios/creditnotes");
        
        // Assert
        response.EnsureSuccessStatusCode();
        var series = await response.Content.ReadFromJsonAsync<List<FolioSeriesDto>>();
        series.Should().NotBeNull();
        series.Should().HaveCount(1); 
        series?.First().Modulo.Should().Be("MOD29");
        series?.First().CodigoSucursal.Should().Be("S1"); // Added check
        series?.First().FolioInicio.Should().Be(501);
        series?.First().FoliosDisponibles.Should().Be(25);
    }

    // --- GET /api/folios/history Tests ---
    [Fact]
    public async Task GetHistory_ReturnsAll_WhenNoFilters()
    {
        // Arrange
        await SeedHistoryDataForFilteringIntegration();
        var expectedCount = 6;

        // Act
        var response = await _client.GetAsync("/api/folios/history");

        // Assert
        response.EnsureSuccessStatusCode();
        var series = await response.Content.ReadFromJsonAsync<List<FolioSeriesDto>>();
        series.Should().HaveCount(expectedCount);
        // Check default ordering (descending timestamp)
        series.Should().BeInDescendingOrder(s => s.FechaRegistro); // Assuming Timestamp maps to FechaRegistro in DTO for now?
                                                                     // Let's check the DTO mapping again. MapToSeriesDto uses history.FechaRegistro
                                                                     // BUT the repository orders by history.Timestamp. Let's verify the latest appears first.
        series?.First().Timestamp.Should().Be(new DateTime(2024, 4, 13, 10, 0, 0, DateTimeKind.Utc)); 
    }

    [Fact]
    public async Task GetHistory_FiltersByModule()
    {
        // Arrange
        await SeedHistoryDataForFilteringIntegration();

        // Act
        var response = await _client.GetAsync("/api/folios/history?module=MOD29");

        // Assert
        response.EnsureSuccessStatusCode();
        var series = await response.Content.ReadFromJsonAsync<List<FolioSeriesDto>>();
        series.Should().HaveCount(2);
        series.Should().OnlyContain(s => s.Modulo == "MOD29");
    }
    
    [Fact]
    public async Task GetHistory_FiltersByStartDate()
    {
        // Arrange
        await SeedHistoryDataForFilteringIntegration();
        var startDate = "2024-04-12";

        // Act
        var response = await _client.GetAsync($"/api/folios/history?startDate={startDate}");

        // Assert
        response.EnsureSuccessStatusCode();
        var series = await response.Content.ReadFromJsonAsync<List<FolioSeriesDto>>();
        series.Should().HaveCount(2);
        series.Should().Contain(s => s.Timestamp.Date >= DateTime.Parse(startDate));
    }
    
    [Fact]
    public async Task GetHistory_FiltersByEndDate()
    {
        // Arrange
        await SeedHistoryDataForFilteringIntegration();
        var endDate = "2024-04-11";

        // Act
        var response = await _client.GetAsync($"/api/folios/history?endDate={endDate}");

        // Assert
        response.EnsureSuccessStatusCode();
        var series = await response.Content.ReadFromJsonAsync<List<FolioSeriesDto>>();
        series.Should().HaveCount(4);
        series.Should().OnlyContain(s => s.Timestamp.Date <= DateTime.Parse(endDate));
    }

    [Fact]
    public async Task GetHistory_AppliesPagination()
    {
        // Arrange
        await SeedHistoryDataForFilteringIntegration();

        // Act
        var response = await _client.GetAsync("/api/folios/history?pageNumber=2&pageSize=3");

        // Assert
        response.EnsureSuccessStatusCode();
        var series = await response.Content.ReadFromJsonAsync<List<FolioSeriesDto>>();
        series.Should().HaveCount(3); // Items 4, 5, 6 in descending order
        // Verify the third item from the default sort (index 2) is the first item on page 2 (index 0)
        // Default sort: 13th, 12th, 11th(12pm), 11th(11am), 11th(10am), 10th
        // Page 1 (size 3): 13th, 12th, 11th(12pm)
        // Page 2 (size 3): 11th(11am), 11th(10am), 10th
        series?.First().Timestamp.Should().Be(new DateTime(2024, 4, 11, 11, 0, 0, DateTimeKind.Utc));
    }
    
    [Fact]
    public async Task GetHistory_ReturnsBadRequest_WhenInvalidModule()
    {
        // Act
        var response = await _client.GetAsync("/api/folios/history?module=INVALID");
        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
    
    [Fact]
    public async Task GetHistory_ReturnsBadRequest_WhenInvalidDateRange()
    {
        // Act
        var response = await _client.GetAsync("/api/folios/history?startDate=2024-04-15&endDate=2024-04-10");
        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
    
    [Fact]
    public async Task GetHistory_ReturnsBadRequest_WhenInvalidPagination()
    {
        // Act
        var response = await _client.GetAsync("/api/folios/history?pageNumber=0&pageSize=10");
        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // --- DELETE /api/folios/history/cleanup Tests ---
    [Fact]
    public async Task CleanupHistory_DeletesOldRecordsAndReturnsCount()
    {
        // Arrange
        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
             // Clear existing data first
            context.FolioHistories.RemoveRange(context.FolioHistories);
            await context.SaveChangesAsync();
        
             var now = DateTime.UtcNow;
             var data = new List<FolioHistory>
             {
                 new FolioHistory { Modulo="MOD28", Timestamp = now.AddDays(-10) }, // Keep
                 new FolioHistory { Modulo="MOD28", Timestamp = now.AddDays(-31) }, // Delete (assuming default 30 days)
                 new FolioHistory { Modulo="MOD29", Timestamp = now.AddDays(-60) }  // Delete
             };
            await context.FolioHistories.AddRangeAsync(data);
            await context.SaveChangesAsync();
        }

        // Act
        var response = await _client.DeleteAsync("/api/folios/history/cleanup");

        // Assert
        response.EnsureSuccessStatusCode();
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var deletedCount = await response.Content.ReadFromJsonAsync<int>();
        deletedCount.Should().Be(2); // Only the -31 and -60 day old records

        // Verify state in DB
        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var remainingCount = await context.FolioHistories.CountAsync();
            remainingCount.Should().Be(1);
            var remainingEntry = await context.FolioHistories.FirstAsync();
            remainingEntry.Timestamp.Should().BeCloseTo(DateTime.UtcNow.AddDays(-10), TimeSpan.FromSeconds(1));
        }
    }
} 