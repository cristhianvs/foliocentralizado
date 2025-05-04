using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FolioMonitor.API.Migrations
{
    public partial class AddLatestFolioSnapshot : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LatestFolioSnapshots",
                columns: table => new
                {
                    CodigoSucursal = table.Column<string>(type: "varchar(255)", nullable: false),
                    Modulo = table.Column<string>(type: "varchar(255)", nullable: false),
                    FolioInicio = table.Column<int>(nullable: false),
                    FolioFin = table.Column<int>(nullable: false),
                    FolioActual = table.Column<int>(nullable: true),
                    FoliosDisponibles = table.Column<int>(nullable: false),
                    Activo = table.Column<bool>(nullable: false),
                    FechaRegistro = table.Column<DateTime>(nullable: false),
                    FechaActualizacion = table.Column<DateTime>(nullable: true),
                    Timestamp = table.Column<DateTime>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LatestFolioSnapshots", x => new { x.CodigoSucursal, x.Modulo, x.FolioInicio, x.FolioFin });
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LatestFolioSnapshots");
        }
    }
} 