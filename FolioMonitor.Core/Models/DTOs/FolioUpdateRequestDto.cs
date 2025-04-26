using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FolioMonitor.Core.Models.DTOs; // Updated namespace

/// <summary>
/// Represents the data structure expected in the body of the POST /api/folios/update request.
/// Based on the payload sent by the Worker Service.
/// </summary>
public class FolioUpdateRequestDto
{
    [Required]
    public DateTime FechaConsulta { get; set; }

    public List<FolioUpdateItemDto> Facturas { get; set; } = new List<FolioUpdateItemDto>();

    public List<FolioUpdateItemDto> NotasCredito { get; set; } = new List<FolioUpdateItemDto>();
}

/// <summary>
/// Represents a single item within the folio update request lists.
/// </summary>
public class FolioUpdateItemDto
{
    // Matches the anonymous type created in ApiClientService.MapToPayloadItem
    public int FolioInicio { get; set; }
    public int FolioFin { get; set; }
    public int? FolioActual { get; set; }
    public int FoliosDisponibles { get; set; }

    // Include other fields from SourceFolioData if needed for mapping to FolioHistory
    // We need Modulo and FechaRegistro from the source to store in FolioHistory
    // Let's add them here, assuming ApiClientService will be updated to include them.
    [Required]
    public string Modulo { get; set; } = string.Empty;
    public DateTime FechaRegistro { get; set; } // From original source
    public bool Activo { get; set; } // From original source
    [Required]
    public string CodigoSucursal { get; set; } = string.Empty; // Store Code
} 