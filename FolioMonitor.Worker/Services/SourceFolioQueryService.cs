using FolioMonitor.Worker.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FolioMonitor.Worker.Services;

public interface ISourceFolioQueryService
{
    Task<List<SourceFolioData>> GetFolioDataAsync(string module);
}

public class SourceFolioQueryService : ISourceFolioQueryService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<SourceFolioQueryService> _logger;
    private readonly string _connectionString;

    public SourceFolioQueryService(IConfiguration configuration, ILogger<SourceFolioQueryService> logger)
    {
        _configuration = configuration;
        _logger = logger;
        _connectionString = _configuration.GetConnectionString("SourceMySql") 
            ?? throw new InvalidOperationException("SourceMySql connection string is not configured.");
    }

    public async Task<List<SourceFolioData>> GetFolioDataAsync(string module)
    {
        _logger.LogInformation("Querying source database for module: {Module}", module);
        var results = new List<SourceFolioData>();

        // Use the query from the PRD, but parameterize the module and use correct date column
        string query = @"
            SELECT FolioInicio, FolioFin, folioactual, Activo, FechaCreacion, CodigoSucursal 
            FROM gts_05controlfolios 
            WHERE Modulo = @Module AND Activo = 1;";
            // PRD query includes calculating folios_disponibles, but we calculate it in the model. 
            // Also, PRD query filters Activo=1 twice, removed redundancy.

        try
        {
            using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();

            using var command = new MySqlCommand(query, connection);
            command.Parameters.AddWithValue("@Module", module);

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                // Get column ordinals once before the loop if performance is critical
                int folioInicioOrdinal = reader.GetOrdinal("FolioInicio");
                int folioFinOrdinal = reader.GetOrdinal("FolioFin");
                int folioActualOrdinal = reader.GetOrdinal("folioactual");
                int activoOrdinal = reader.GetOrdinal("Activo");
                int fechaCreacionOrdinal = reader.GetOrdinal("FechaCreacion");
                int codigoSucursalOrdinal = reader.GetOrdinal("CodigoSucursal");

                results.Add(new SourceFolioData
                {
                    FolioInicio = reader.GetInt32(folioInicioOrdinal),
                    FolioFin = reader.GetInt32(folioFinOrdinal),
                    FolioActual = reader.IsDBNull(folioActualOrdinal) ? (int?)null : reader.GetInt32(folioActualOrdinal),
                    Activo = reader.GetBoolean(activoOrdinal),
                    Modulo = module, // Assign the module we queried for
                    FechaRegistro = reader.GetDateTime(fechaCreacionOrdinal),
                    CodigoSucursal = reader.GetString(codigoSucursalOrdinal)
                });
            }
            _logger.LogInformation("Found {Count} active folio series for module: {Module}", results.Count, module);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error querying source database for module {Module}", module);
            // Depending on requirements, might want to re-throw or return empty list
            throw; 
        }

        return results;
    }
} 