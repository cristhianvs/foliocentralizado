using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FolioMonitor.API.Migrations
{
    /// <inheritdoc />
    public partial class UpdateLatestSnapshotKeyLength : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LatestFolioSnapshots",
                columns: table => new
                {
                    FolioInicio = table.Column<int>(type: "int", nullable: false),
                    FolioFin = table.Column<int>(type: "int", nullable: false),
                    Modulo = table.Column<string>(type: "varchar(10)", maxLength: 10, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CodigoSucursal = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    FolioActual = table.Column<int>(type: "int", nullable: true),
                    FoliosDisponibles = table.Column<int>(type: "int", nullable: false),
                    Activo = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    FechaRegistro = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    FechaActualizacion = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    Timestamp = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LatestFolioSnapshots", x => new { x.CodigoSucursal, x.Modulo, x.FolioInicio, x.FolioFin });
                })
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LatestFolioSnapshots");
        }
    }
}
