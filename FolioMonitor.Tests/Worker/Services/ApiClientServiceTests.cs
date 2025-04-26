using FolioMonitor.Core.Models.DTOs;
using FolioMonitor.Worker.Models;
using FolioMonitor.Worker.Services;
using FolioMonitor.Tests.Helpers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;
using FluentAssertions;
using System.Text.Json;
using System.Linq;

namespace FolioMonitor.Tests.Worker.Services;

public class ApiClientServiceTests
{
    private readonly Mock<ILogger<ApiClientService>> _mockLogger;
    private readonly IConfiguration _configuration;

    public ApiClientServiceTests()
    {
        _mockLogger = new Mock<ILogger<ApiClientService>>();

        // Set up configuration (can use in-memory collection)
        var configData = new Dictionary<string, string?>
        {
            {"FolioApi:BaseUrl", "http://test-api.com"},
            {"FolioApi:Endpoint", "/api/testupdate"},
            {"FolioApi:ApiKey", "test-key"}
        };
        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();
    }

    private ApiClientService CreateService(HttpMessageHandler handler)
    {
        var httpClient = new HttpClient(handler);
        return new ApiClientService(httpClient, _configuration, _mockLogger.Object);
    }

    [Fact]
    public async Task SendFolioDataAsync_ShouldSendCorrectPayloadAndHeaders_WhenApiCallSucceeds()
    {
        // Arrange
        var expectedApiKey = "test-key";
        var expectedEndpoint = "/api/testupdate";
        HttpRequestMessage? capturedRequest = null;

        var mockHandler = new MockHttpMessageHandler((req, ct) => 
        {
            capturedRequest = req; // Capture the request for inspection
            return new HttpResponseMessage(HttpStatusCode.OK);
        });
        
        var service = CreateService(mockHandler);
        var invoiceData = new List<SourceFolioData> { new SourceFolioData { FolioInicio = 1, FolioFin = 100, Modulo = "MOD28", Activo = true } };
        var creditNoteData = new List<SourceFolioData> { new SourceFolioData { FolioInicio = 500, FolioFin = 600, Modulo = "MOD29", Activo = true } };

        // Act
        await service.SendFolioDataAsync(invoiceData, creditNoteData);

        // Assert
        capturedRequest.Should().NotBeNull();
        capturedRequest?.Method.Should().Be(HttpMethod.Post);
        capturedRequest?.RequestUri?.AbsolutePath.Should().Be(expectedEndpoint);
        
        // Check API Key Header
        IEnumerable<string>? apiKeys = null; // Initialize
        capturedRequest?.Headers.TryGetValues("X-API-KEY", out apiKeys).Should().BeTrue();
        apiKeys?.FirstOrDefault().Should().Be(expectedApiKey);

        // Check Payload Content (Deserialize and verify)
        capturedRequest?.Content.Should().NotBeNull();
        var contentJson = await capturedRequest!.Content!.ReadAsStringAsync();
        // Deserialize to the actual DTO
        var payload = JsonSerializer.Deserialize<FolioUpdateRequestDto>(contentJson, 
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }); // Be flexible with casing during test deserialization

        payload.Should().NotBeNull();
        payload!.FechaConsulta.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5)); // Allow small diff

        payload.Facturas.Should().HaveCount(1);
        payload.Facturas[0].FolioInicio.Should().Be(1);
        payload.Facturas[0].FolioFin.Should().Be(100);
        payload.Facturas[0].Modulo.Should().Be("MOD28"); // Verify extra fields are sent

        payload.NotasCredito.Should().HaveCount(1);
        payload.NotasCredito[0].FolioInicio.Should().Be(500);
        payload.NotasCredito[0].FolioFin.Should().Be(600);
        payload.NotasCredito[0].Modulo.Should().Be("MOD29");

        // Verify Success Log
        _mockLogger.VerifyLog(LogLevel.Information, Times.Once(), "Successfully sent data to API.");
    }

    [Fact]
    public async Task SendFolioDataAsync_ShouldLogError_WhenApiCallFails()
    {
        // Arrange
        var mockHandler = new MockHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.InternalServerError) 
        { 
            Content = new StringContent("API Error") 
        });
        var service = CreateService(mockHandler);
        var invoiceData = new List<SourceFolioData>();
        var creditNoteData = new List<SourceFolioData>();

        // Act
        await service.SendFolioDataAsync(invoiceData, creditNoteData);

        // Assert
        // Verify Error Log (using helper from below)
         _mockLogger.VerifyLogContains(LogLevel.Error, Times.Once(), "Failed to send data to API.");
    }
} 