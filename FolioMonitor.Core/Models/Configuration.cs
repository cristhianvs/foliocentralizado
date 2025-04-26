using System.ComponentModel.DataAnnotations;

namespace FolioMonitor.Core.Models;

/// <summary>
/// Represents a configuration setting for the monitoring system.
/// </summary>
public class Configuration
{
    [Key]
    [MaxLength(100)]
    public string Key { get; set; } = string.Empty;

    [Required]
    public string Value { get; set; } = string.Empty;

    public string? Description { get; set; }
} 