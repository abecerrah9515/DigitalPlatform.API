using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DigitalPlatform.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddLookupTablesAndConsolidacionLog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CeBes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Codigo = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CeBeGroup = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Nombre = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CeBes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ConsolidacionLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FechaInicio = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FechaFin = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Estado = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TotalRegistros = table.Column<int>(type: "int", nullable: false),
                    RegistrosExitosos = table.Column<int>(type: "int", nullable: false),
                    RegistrosFallidos = table.Column<int>(type: "int", nullable: false),
                    Errores = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IniciadoPor = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConsolidacionLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Industrias",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CodIndustria = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Vertical = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Industrias", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Sociedades",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Codigo = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    RazonSocial = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Pais = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sociedades", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TiposCambio",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Año = table.Column<int>(type: "int", nullable: false),
                    Mes = table.Column<int>(type: "int", nullable: false),
                    Moneda = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Tasa = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TiposCambio", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TiposCambio_Año_Mes_Moneda",
                table: "TiposCambio",
                columns: new[] { "Año", "Mes", "Moneda" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CeBes");

            migrationBuilder.DropTable(
                name: "ConsolidacionLogs");

            migrationBuilder.DropTable(
                name: "Industrias");

            migrationBuilder.DropTable(
                name: "Sociedades");

            migrationBuilder.DropTable(
                name: "TiposCambio");
        }
    }
}
