using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DigitalPlatform.Infrastructure.Migrations
{
    public partial class ExpandControlFacturaColumns : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "ControlFacturas");

            migrationBuilder.CreateTable(
                name: "ControlFacturas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", Npgsql.EntityFrameworkCore.PostgreSQL.Metadata.NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CargaArchivoId = table.Column<int>(type: "integer", nullable: false),
                    FechaSolicito = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    FechaEmision = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    FechaEnvio = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    FechaPago = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    NumeroCliente = table.Column<string>(type: "text", nullable: false),
                    Pep = table.Column<string>(type: "text", nullable: false),
                    Cliente = table.Column<string>(type: "text", nullable: false),
                    Valor = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Iva = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    ReteIva = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Autorenta = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Retencion = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Ica = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Total = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    OrdenConsecutivo = table.Column<string>(type: "text", nullable: false),
                    NumeroDocumento = table.Column<string>(type: "text", nullable: false),
                    OrdenPedido = table.Column<string>(type: "text", nullable: false),
                    EntradaMercancia = table.Column<string>(type: "text", nullable: false),
                    Concepto = table.Column<string>(type: "text", nullable: false),
                    Observaciones = table.Column<string>(type: "text", nullable: false),
                    Estado = table.Column<string>(type: "text", nullable: false),
                    DatosAdicionales = table.Column<string>(type: "text", nullable: false),
                    Trm = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    DiaTrm = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ValorUsd = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    FechaSolicitud = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ValorAnulacion = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    ServicioProducto = table.Column<string>(type: "text", nullable: false),
                    RazonAnulacion = table.Column<string>(type: "text", nullable: false),
                    NotaCredito = table.Column<string>(type: "text", nullable: false),
                    NumeroDocumento2 = table.Column<string>(type: "text", nullable: false),
                    Compensacion = table.Column<string>(type: "text", nullable: false),
                    Reemplazo = table.Column<string>(type: "text", nullable: false),
                    ValorCancelar = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    ActaNumero = table.Column<string>(type: "text", nullable: false),
                    NumeroSeguimiento = table.Column<string>(type: "text", nullable: false),
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

            migrationBuilder.CreateIndex(
                name: "IX_ControlFacturas_CargaArchivoId",
                table: "ControlFacturas",
                column: "CargaArchivoId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "ControlFacturas");

            migrationBuilder.CreateTable(
                name: "ControlFacturas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", Npgsql.EntityFrameworkCore.PostgreSQL.Metadata.NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CargaArchivoId = table.Column<int>(type: "integer", nullable: false),
                    Cliente = table.Column<string>(type: "text", nullable: false),
                    Estado = table.Column<string>(type: "text", nullable: false),
                    Factura = table.Column<string>(type: "text", nullable: false),
                    FechaEmision = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    FechaVencimiento = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Monto = table.Column<decimal>(type: "numeric", nullable: false),
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

            migrationBuilder.CreateIndex(
                name: "IX_ControlFacturas_CargaArchivoId",
                table: "ControlFacturas",
                column: "CargaArchivoId");
        }
    }
}
