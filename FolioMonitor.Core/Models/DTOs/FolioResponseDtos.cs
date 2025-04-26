using System;
using System.Collections.Generic;

namespace FolioMonitor.Core.Models.DTOs; // Updated namespace

/// <summary>
/// Data Transfer Object representing a single folio series' details.
/// Used for API responses.
/// </summary>
public class FolioSeriesDto
{
    public int Id { get; set; } // Monitoring DB History Id
    public int FolioInicio { get; set; }
    public int FolioFin { get; set; }
    public string Modulo { get; set; } = string.Empty; // "MOD28" or "MOD29"
    public int? FolioActual { get; set; }
    public bool Activo { get; set; } // Status from source at time of check
    public int FoliosDisponibles { get; set; }
    public string CodigoSucursal { get; set; } = string.Empty; // Store Code
    public DateTime FechaRegistro { get; set; } // Timestamp from original source
    public DateTime Timestamp { get; set; } // Timestamp from monitoring DB
}

/// <summary>
/// Data Transfer Object representing a summary of folio availability for a document type.
/// Used for API responses.
/// </summary>
public class FolioSummaryDto
{
    public string DocumentType { get; set; } = string.Empty; // e.g., "Invoice", "Credit Note"
    public int TotalAvailableFolios { get; set; }
    public List<FolioSeriesDto> Series { get; set; } = new List<FolioSeriesDto>();
} 