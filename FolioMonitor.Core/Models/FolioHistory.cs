namespace FolioMonitor.Core.Models;

/// <summary>
/// Represents a historical record of folio availability for a specific series at a point in time.
/// </summary>
public class FolioHistory
{
    public int Id { get; set; }
    public int FolioInicio { get; set; }
    public int FolioFin { get; set; }
    public string Modulo { get; set; } = string.Empty; // e.g., "MOD28", "MOD29"
    public int? FolioActual { get; set; } // Null if no folio has been used
    public int FoliosDisponibles { get; set; } 
    public bool Activo { get; set; } // Whether the original series was active at time of check
    public string CodigoSucursal { get; set; } = string.Empty; // Store Code
    public DateTime FechaRegistro { get; set; } // Original FechaRegistro from the source table (if available)
    public DateTime Timestamp { get; set; } // When this record was created in the monitoring DB
} 