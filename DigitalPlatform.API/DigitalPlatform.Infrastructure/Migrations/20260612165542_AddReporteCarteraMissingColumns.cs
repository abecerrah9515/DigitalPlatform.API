using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DigitalPlatform.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddReporteCarteraMissingColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AcuerdoPago",
                table: "ReporteCarteraFacturas",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Comentario",
                table: "ReporteCarteraFacturas",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CuentaMayor",
                table: "ReporteCarteraFacturas",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "FechaContabiliz",
                table: "ReporteCarteraFacturas",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "FechaPagoReal",
                table: "ReporteCarteraFacturas",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ImporteMonedaDoc",
                table: "ReporteCarteraFacturas",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "Responsable",
                table: "ReporteCarteraFacturas",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Semana",
                table: "ReporteCarteraFacturas",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AcuerdoPago",
                table: "ReporteCarteraFacturas");

            migrationBuilder.DropColumn(
                name: "Comentario",
                table: "ReporteCarteraFacturas");

            migrationBuilder.DropColumn(
                name: "CuentaMayor",
                table: "ReporteCarteraFacturas");

            migrationBuilder.DropColumn(
                name: "FechaContabiliz",
                table: "ReporteCarteraFacturas");

            migrationBuilder.DropColumn(
                name: "FechaPagoReal",
                table: "ReporteCarteraFacturas");

            migrationBuilder.DropColumn(
                name: "ImporteMonedaDoc",
                table: "ReporteCarteraFacturas");

            migrationBuilder.DropColumn(
                name: "Responsable",
                table: "ReporteCarteraFacturas");

            migrationBuilder.DropColumn(
                name: "Semana",
                table: "ReporteCarteraFacturas");
        }
    }
}
