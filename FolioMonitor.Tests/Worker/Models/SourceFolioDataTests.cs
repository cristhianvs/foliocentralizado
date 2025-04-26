using FolioMonitor.Worker.Models;
using Xunit;
using FluentAssertions;

namespace FolioMonitor.Tests.Worker.Models;

public class SourceFolioDataTests
{
    [Theory]
    [InlineData(1, 100, 50, true, 50)] // Normal case: 100 - 50 = 50
    [InlineData(1, 100, 100, true, 0)] // Last folio used: 100 - 100 = 0
    [InlineData(1, 100, null, true, 100)] // No folio used yet: 100 - 1 + 1 = 100
    [InlineData(101, 200, 150, true, 50)] // Different range: 200 - 150 = 50
    [InlineData(1, 100, 50, false, 0)] // Inactive series: Should be 0
    [InlineData(1, 1, null, true, 1)]   // Single folio, unused: 1 - 1 + 1 = 1
    [InlineData(1, 1, 1, true, 0)]     // Single folio, used: 1 - 1 = 0
    public void FoliosDisponibles_ShouldCalculateCorrectly(int inicio, int fin, int? actual, bool activo, int expectedDisponibles)
    {
        // Arrange
        var data = new SourceFolioData
        {
            FolioInicio = inicio,
            FolioFin = fin,
            FolioActual = actual,
            Activo = activo,
            Modulo = "MOD28", // Not relevant for calculation
            FechaRegistro = DateTime.Now // Not relevant
        };

        // Act
        var result = data.FoliosDisponibles;

        // Assert
        result.Should().Be(expectedDisponibles);
    }
} 