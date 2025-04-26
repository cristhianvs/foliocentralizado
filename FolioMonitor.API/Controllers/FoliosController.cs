using FolioMonitor.Core.Models.DTOs;
using FolioMonitor.Core.Interfaces;
using FolioMonitor.Core.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FolioMonitor.API.Controllers;

[Route("api/[controller]")]
[ApiController]
// Consider adding authorization attribute here if needed later, 
// e.g., based on API key or other mechanism.
// Middleware is handling basic API key check for now.
public class FoliosController : ControllerBase
{
    private readonly IFolioHistoryRepository _folioHistoryRepo;
    private readonly IAlertService _alertService;
    private readonly ILogger<FoliosController> _logger;
    private readonly IConfiguration _configuration;

    public FoliosController(
        IFolioHistoryRepository folioHistoryRepo, 
        IAlertService alertService,
        ILogger<FoliosController> logger,
        IConfiguration configuration)
    {
        _folioHistoryRepo = folioHistoryRepo;
        _alertService = alertService;
        _logger = logger;
        _configuration = configuration;
    }

    // POST /api/folios/update
    // Task 3 & 4: Accepts data from Worker and persists it.
    // Task 6: Checks for alerts
    [HttpPost("update")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(string))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    /// <summary>
    /// Receives folio availability data from the Worker Service and updates the monitoring database.
    /// </summary>
    /// <remarks>
    /// This endpoint is intended to be called by the background Worker Service.
    /// It requires a valid API Key in the X-API-KEY header.
    /// </remarks>
    /// <param name="request">The folio data payload.</param>
    /// <returns>An HTTP status code indicating success or failure.</returns>
    public async Task<IActionResult> UpdateFolioData([FromBody] FolioUpdateRequestDto request)
    {
        _logger.LogInformation("Received folio update request at {Timestamp}", DateTime.UtcNow);

        if (!ModelState.IsValid)
        {
            _logger.LogWarning("Invalid folio update request received.");
            return BadRequest(ModelState);
        }

        try
        {
            var historyEntries = new List<FolioHistory>();
            var timestamp = DateTime.UtcNow; // Use a consistent timestamp for all records in this batch

            // Map Facturas
            historyEntries.AddRange(request.Facturas.Select(item => MapToHistory(item, timestamp)));

            // Map NotasCredito
            historyEntries.AddRange(request.NotasCredito.Select(item => MapToHistory(item, timestamp)));
            
            if (!historyEntries.Any())
            {
                 _logger.LogInformation("Received folio update request with no series data.");
                 return Ok("No folio data received.");
            }

            // Persist using the repository (Task 4)
            await _folioHistoryRepo.AddRangeAsync(historyEntries);

            _logger.LogInformation("Successfully processed and stored {Count} folio history records.", historyEntries.Count);
            
            // Task 6 - Trigger Alert System Logic here after saving data
            await _alertService.CheckAlertsAsync(request);

            return Ok("Folio data updated successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing folio update request.");
            return StatusCode(500, "An internal server error occurred while updating folio data.");
        }
    }

    // GET /api/folios/summary
    // Task 3: Returns overall summary
    [HttpGet("summary")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<FolioSummaryDto>))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    /// <summary>
    /// Gets the latest overall summary of folio availability for invoices and credit notes.
    /// </summary>
    /// <returns>A list of summaries, one for each document type.</returns>
    public async Task<ActionResult<List<FolioSummaryDto>>> GetFolioSummary()
    {
        _logger.LogInformation("Fetching folio summary.");
        try
        {
            var latestData = await _folioHistoryRepo.GetLatestAsync();
            
            var summary = latestData
                .GroupBy(h => h.Modulo)
                .Select(g => new FolioSummaryDto
                {
                    DocumentType = MapModuloToDocumentType(g.Key),
                    TotalAvailableFolios = g.Sum(h => h.FoliosDisponibles),
                    Series = g.Select(MapToSeriesDto).ToList()
                }).ToList();

            return Ok(summary);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching folio summary.");
            return StatusCode(500, "An internal server error occurred while fetching the summary.");
        }
    }

    // GET /api/folios/invoices
    // Task 3: Returns invoice folio details
    [HttpGet("invoices")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<FolioSeriesDto>))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    /// <summary>
    /// Gets the detailed information for the latest active invoice (MOD28) folio series.
    /// </summary>
    /// <returns>A list of invoice folio series details.</returns>
    public async Task<ActionResult<IEnumerable<FolioSeriesDto>>> GetInvoiceFolios()
    {
        return await GetFoliosByModule("MOD28");
    }

    // GET /api/folios/creditnotes
    // Task 3: Returns credit note folio details
    [HttpGet("creditnotes")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<FolioSeriesDto>))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    /// <summary>
    /// Gets the detailed information for the latest active credit note (MOD29) folio series.
    /// </summary>
    /// <returns>A list of credit note folio series details.</returns>
    public async Task<ActionResult<IEnumerable<FolioSeriesDto>>> GetCreditNoteFolios()
    {
        return await GetFoliosByModule("MOD29");
    }

    // GET /api/folios/history
    // Task 8: Returns historical folio data with filtering and pagination
    [HttpGet("history")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<FolioSeriesDto>))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    /// <summary>
    /// Gets historical folio availability records, with optional filtering and pagination.
    /// </summary>
    /// <param name="module">Filter by module (MOD28 or MOD29).</param>
    /// <param name="startDate">Filter by start date (yyyy-MM-dd).</param>
    /// <param name="endDate">Filter by end date (yyyy-MM-dd).</param>
    /// <param name="pageNumber">Page number (default: 1).</param>
    /// <param name="pageSize">Number of items per page (default: 100).</param>
    /// <returns>A list of historical folio records.</returns>
    public async Task<ActionResult<IEnumerable<FolioSeriesDto>>> GetHistory(
        [FromQuery] string? module = null,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 100)
    {
        _logger.LogInformation("Fetching folio history with params: Module={Module}, Start={StartDate}, End={EndDate}, Page={Page}, Size={PageSize}",
            module, startDate, endDate, pageNumber, pageSize);
        
        // Basic validation
        if (pageNumber < 1 || pageSize < 1)
        {
            return BadRequest("Page number and page size must be positive.");
        }
        if (startDate.HasValue && endDate.HasValue && startDate > endDate)
        {
            return BadRequest("Start date cannot be after end date.");
        }
        // Could add module validation (MOD28/MOD29)
        if (!string.IsNullOrEmpty(module) && module != "MOD28" && module != "MOD29")
        { 
             return BadRequest("Module must be either MOD28 or MOD29.");
        }

        try
        {
            var historyData = await _folioHistoryRepo.GetHistoryAsync(module, startDate, endDate, pageNumber, pageSize);
            var dtos = historyData.Select(MapToSeriesDto).ToList(); // Reuse existing DTO mapping
            
            // Consider adding pagination info to the response headers or a wrapper object
            // e.g., Response.Headers.Add("X-Pagination", JsonSerializer.Serialize(new { pageNumber, pageSize, totalCount = ... }));
            // Getting totalCount would require another query or modifying GetHistoryAsync.
            
            return Ok(dtos);
        }
        catch (Exception ex)
        {
             _logger.LogError(ex, "Error fetching folio history.");
            return StatusCode(500, "An internal server error occurred while fetching folio history.");
        }
    }

    // DELETE /api/folios/history/cleanup
    // Task 9: Deletes old history records based on retention policy
    [HttpDelete("history/cleanup")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(int))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    /// <summary>
    /// Deletes historical folio records older than the configured retention period.
    /// </summary>
    /// <remarks>
    /// Reads the retention period from configuration (RetentionPolicy:RetentionDays). 
    /// Default is 365 days if not configured.
    /// Requires a valid API Key.
    /// </remarks>
    /// <returns>The number of historical records deleted.</returns>
    public async Task<ActionResult<int>> CleanupHistory()
    {
        _logger.LogInformation("Starting folio history cleanup process.");
        
        var retentionDays = _configuration.GetValue<int?>("RetentionPolicy:RetentionDays") ?? 365;
        if (retentionDays <= 0)
        {
            _logger.LogWarning("Invalid RetentionDays ({RetentionDays}) configured. Cleanup aborted.", retentionDays);
            // Return 0 deleted or BadRequest?
            return Ok(0); // Indicate no action taken due to config
        }

        _logger.LogInformation("Retention policy set to {RetentionDays} days. Deleting records older than {CutoffDate}.", 
                               retentionDays, 
                               DateTime.UtcNow.AddDays(-retentionDays));

        try
        {
            var deletedCount = await _folioHistoryRepo.DeleteOldEntriesAsync(retentionDays);
            _logger.LogInformation("Successfully deleted {DeletedCount} old folio history records.", deletedCount);
            return Ok(deletedCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during folio history cleanup.");
            return StatusCode(500, "An internal server error occurred during history cleanup.");
        }
    }

    // --- Helper Methods ---

    private async Task<ActionResult<IEnumerable<FolioSeriesDto>>> GetFoliosByModule(string module)
    {
         _logger.LogInformation("Fetching latest folio data for module: {Module}", module);
        try
        {
            var latestData = await _folioHistoryRepo.GetLatestByModuleAsync(module);
            var dtos = latestData.Select(MapToSeriesDto).ToList();
            return Ok(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching folio data for module {Module}", module);
            return StatusCode(500, $"An internal server error occurred while fetching data for {module}.");
        }
    }

    private FolioHistory MapToHistory(FolioUpdateItemDto dto, DateTime timestamp)
    {
        return new FolioHistory
        {
            FolioInicio = dto.FolioInicio,
            FolioFin = dto.FolioFin,
            FolioActual = dto.FolioActual,
            FoliosDisponibles = dto.FoliosDisponibles,
            Modulo = dto.Modulo,
            Activo = dto.Activo, 
            CodigoSucursal = dto.CodigoSucursal, // Map Store Code
            FechaRegistro = dto.FechaRegistro, // From source
            Timestamp = timestamp // Timestamp of this monitoring record
        };
    }

    private FolioSeriesDto MapToSeriesDto(FolioHistory history)
    {
        return new FolioSeriesDto
        {
            Id = history.Id, // Use the ID from the monitoring DB history record
            FolioInicio = history.FolioInicio,
            FolioFin = history.FolioFin,
            Modulo = history.Modulo,
            CodigoSucursal = history.CodigoSucursal, // Map Store Code
            FolioActual = history.FolioActual,
            Activo = history.Activo,
            FoliosDisponibles = history.FoliosDisponibles,
            FechaRegistro = history.FechaRegistro,
            Timestamp = history.Timestamp // Map the Timestamp
        };
    }

    private string MapModuloToDocumentType(string modulo)
    {
        return modulo switch
        {
            "MOD28" => "Invoice",
            "MOD29" => "Credit Note",
            _ => "Unknown"
        };
    }
} 