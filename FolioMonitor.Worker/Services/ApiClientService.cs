using FolioMonitor.Worker.Models; // For SourceFolioData
using FolioMonitor.Core.Models.DTOs; // Correct namespace for DTOs
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json; // Requires Microsoft.Extensions.Http
using System.Text.Json;
using System.Threading.Tasks;

namespace FolioMonitor.Worker.Services;

public interface IApiClientService
{
    Task SendFolioDataAsync(List<SourceFolioData> invoiceData, List<SourceFolioData> creditNoteData);
}

public class ApiClientService : IApiClientService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ApiClientService> _logger;
    private readonly string _apiEndpoint;
    private readonly string _apiKey;

    public ApiClientService(HttpClient httpClient, IConfiguration configuration, ILogger<ApiClientService> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;

        var apiConfig = _configuration.GetSection("FolioApi");
        var baseUrl = apiConfig.GetValue<string>("BaseUrl")
            ?? throw new InvalidOperationException("API BaseUrl is not configured.");
        _apiEndpoint = apiConfig.GetValue<string>("Endpoint")
            ?? throw new InvalidOperationException("API Endpoint is not configured.");
        _apiKey = apiConfig.GetValue<string>("ApiKey")
            ?? throw new InvalidOperationException("API ApiKey is not configured.");

        _httpClient.BaseAddress = new Uri(baseUrl);
        _httpClient.DefaultRequestHeaders.Add("X-API-KEY", _apiKey);
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    }

    public async Task SendFolioDataAsync(List<SourceFolioData> invoiceData, List<SourceFolioData> creditNoteData)
    {
        var payload = new FolioUpdateRequestDto 
        {
            FechaConsulta = DateTime.UtcNow, // Use UTC time
            Facturas = invoiceData.Select(MapToPayloadItem).ToList(),
            NotasCredito = creditNoteData.Select(MapToPayloadItem).ToList()
        };

        try
        {
            _logger.LogInformation("Sending data to API endpoint: {Endpoint} with payload: {Payload}", _apiEndpoint, JsonSerializer.Serialize(payload)); // Log payload for easier debugging
            var response = await _httpClient.PostAsJsonAsync(_apiEndpoint, payload);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Successfully sent data to API.");
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Failed to send data to API. Status: {StatusCode}, Response: {Response}", 
                                 response.StatusCode, 
                                 errorContent);
                // Consider more robust error handling/retry logic here
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending data to API endpoint {Endpoint}", _apiEndpoint);
            throw;
        }
    }

    // Helper to map SourceFolioData to the structure expected by the API payload
    private FolioUpdateItemDto MapToPayloadItem(SourceFolioData data)
    {
        // Map to the concrete DTO instead of an anonymous object
        return new FolioUpdateItemDto
        {
            FolioInicio = data.FolioInicio,
            FolioFin = data.FolioFin,
            FolioActual = data.FolioActual,
            FoliosDisponibles = data.FoliosDisponibles, // Use calculated property
            Modulo = data.Modulo, // Include Modulo
            FechaRegistro = data.FechaRegistro, // Include FechaRegistro
            Activo = data.Activo, // Include Activo
            CodigoSucursal = data.CodigoSucursal // Include Store Code
        };
    }
} 