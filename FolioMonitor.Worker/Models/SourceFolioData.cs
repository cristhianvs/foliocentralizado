using System;

namespace FolioMonitor.Worker.Models;

/// <summary>
/// Represents the raw data queried from the source gts_05controlfolios table.
/// </summary>
public class SourceFolioData
{
    public int FolioInicio { get; set; }
    public int FolioFin { get; set; }
    public int? FolioActual { get; set; } 
    public bool Activo { get; set; } // Needed for calculation
    public string Modulo { get; set; } = string.Empty; // e.g., MOD28, MOD29
    public DateTime FechaRegistro { get; set; } // Include FechaRegistro from source
    public string CodigoSucursal { get; set; } = string.Empty; // Store Code

    // Calculated property based on PRD logic
    public int FoliosDisponibles => 
        !Activo ? 0 :
        FolioActual == null ? (FolioFin - FolioInicio + 1) :
        (FolioFin - FolioActual.Value);
} 