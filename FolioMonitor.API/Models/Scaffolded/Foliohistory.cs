using System;
using System.Collections.Generic;

namespace FolioMonitor.API.Models.Scaffolded;

public partial class Foliohistory
{
    public int Id { get; set; }

    public int FolioInicio { get; set; }

    public int FolioFin { get; set; }

    public string Modulo { get; set; } = null!;

    public int? FolioActual { get; set; }

    public int FoliosDisponibles { get; set; }

    public bool Activo { get; set; }

    public string CodigoSucursal { get; set; } = null!;

    public DateTime FechaRegistro { get; set; }

    public DateTime Timestamp { get; set; }

    public DateTime? FechaActualizacion { get; set; }
}
