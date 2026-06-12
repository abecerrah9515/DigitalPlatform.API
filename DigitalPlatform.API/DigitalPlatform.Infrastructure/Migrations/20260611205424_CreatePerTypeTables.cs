using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace DigitalPlatform.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class CreatePerTypeTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BaseClientes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CargaArchivoId = table.Column<int>(type: "integer", nullable: false),
                    Nombre = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Nit = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Grupo = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Direccion = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Ciudad = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Region = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Pais = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    CodigoPostal = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Telefono = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Email = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    CondicionesPago = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ContactoContabilidad = table.Column<string>(type: "text", nullable: false),
                    ContactoTesoreria = table.Column<string>(type: "text", nullable: false),
                    ContactoFinanzas = table.Column<string>(type: "text", nullable: false),
                    ContactoOperacion = table.Column<string>(type: "text", nullable: false),
                    ContactoComercial = table.Column<string>(type: "text", nullable: false),
                    ContactoCompras = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BaseClientes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BaseClientes_CargasArchivo_CargaArchivoId",
                        column: x => x.CargaArchivoId,
                        principalTable: "CargasArchivo",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ControlFacturas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CargaArchivoId = table.Column<int>(type: "integer", nullable: false),
                    FechaEmision = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FechaVencimiento = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Factura = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Cliente = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Monto = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Estado = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ControlFacturas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ControlFacturas_CargasArchivo_CargaArchivoId",
                        column: x => x.CargaArchivoId,
                        principalTable: "CargasArchivo",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ReporteCarteraFacturas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CargaArchivoId = table.Column<int>(type: "integer", nullable: false),
                    Deudor = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    RazonSocial = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Referencia = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Asignacion = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    FechaDocumento = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    FechaPago = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    FechaCompromiso = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ValorRecibir = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    ImporteMonedaLocal = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Estado = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    DemoraDPP1 = table.Column<int>(type: "integer", nullable: false),
                    MonedaDocumento = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    VencidoEnTiempo = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Vencido0_15 = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Vencido16_30 = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Vencido31_60 = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Vencido61_90 = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Vencido91_120 = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReporteCarteraFacturas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReporteCarteraFacturas_CargasArchivo_CargaArchivoId",
                        column: x => x.CargaArchivoId,
                        principalTable: "CargasArchivo",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BaseClientes_CargaArchivoId",
                table: "BaseClientes",
                column: "CargaArchivoId");

            migrationBuilder.CreateIndex(
                name: "IX_ControlFacturas_CargaArchivoId",
                table: "ControlFacturas",
                column: "CargaArchivoId");

            migrationBuilder.CreateIndex(
                name: "IX_ReporteCarteraFacturas_CargaArchivoId",
                table: "ReporteCarteraFacturas",
                column: "CargaArchivoId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BaseClientes");

            migrationBuilder.DropTable(
                name: "ControlFacturas");

            migrationBuilder.DropTable(
                name: "ReporteCarteraFacturas");
        }
    }
}
