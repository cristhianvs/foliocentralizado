using System;
using System.Collections.Generic;

namespace FolioMonitor.API.Models.Scaffolded;

public partial class Configuration
{
    public string Key { get; set; } = null!;

    public string Value { get; set; } = null!;

    public string? Description { get; set; }
}
