using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FolioMonitor.API.Migrations
{
    /// <inheritdoc />
    public partial class AddFechaActualizacionToFolioHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "FechaActualizacion",
                table: "FolioHistories",
                type: "datetime(6)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FechaActualizacion",
                table: "FolioHistories");
        }
    }
}
