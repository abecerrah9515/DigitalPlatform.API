using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DigitalPlatform.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FixBaseClientesColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Region",
                table: "BaseClientes",
                newName: "TipoNif");

            migrationBuilder.RenameColumn(
                name: "Nit",
                table: "BaseClientes",
                newName: "Poblacion");

            migrationBuilder.RenameColumn(
                name: "Grupo",
                table: "BaseClientes",
                newName: "PersonaFisica");

            migrationBuilder.RenameColumn(
                name: "Email",
                table: "BaseClientes",
                newName: "NumeroCliente");

            migrationBuilder.RenameColumn(
                name: "Direccion",
                table: "BaseClientes",
                newName: "ModificadoPor");

            migrationBuilder.RenameColumn(
                name: "CondicionesPago",
                table: "BaseClientes",
                newName: "IdTipoNif");

            migrationBuilder.RenameColumn(
                name: "CodigoPostal",
                table: "BaseClientes",
                newName: "IdPais");

            migrationBuilder.RenameColumn(
                name: "Ciudad",
                table: "BaseClientes",
                newName: "IdGrupoClientes");

            migrationBuilder.AddColumn<string>(
                name: "Addenda",
                table: "BaseClientes",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CampoClasificacion",
                table: "BaseClientes",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ClaseImpuesto",
                table: "BaseClientes",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CorreoElectronico",
                table: "BaseClientes",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "FechaModificacion",
                table: "BaseClientes",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GpoClientes",
                table: "BaseClientes",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "GrupoCuenta",
                table: "BaseClientes",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "IdAddenda",
                table: "BaseClientes",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "IdClaseImpuesto",
                table: "BaseClientes",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Addenda",
                table: "BaseClientes");

            migrationBuilder.DropColumn(
                name: "CampoClasificacion",
                table: "BaseClientes");

            migrationBuilder.DropColumn(
                name: "ClaseImpuesto",
                table: "BaseClientes");

            migrationBuilder.DropColumn(
                name: "CorreoElectronico",
                table: "BaseClientes");

            migrationBuilder.DropColumn(
                name: "FechaModificacion",
                table: "BaseClientes");

            migrationBuilder.DropColumn(
                name: "GpoClientes",
                table: "BaseClientes");

            migrationBuilder.DropColumn(
                name: "GrupoCuenta",
                table: "BaseClientes");

            migrationBuilder.DropColumn(
                name: "IdAddenda",
                table: "BaseClientes");

            migrationBuilder.DropColumn(
                name: "IdClaseImpuesto",
                table: "BaseClientes");

            migrationBuilder.RenameColumn(
                name: "TipoNif",
                table: "BaseClientes",
                newName: "Region");

            migrationBuilder.RenameColumn(
                name: "Poblacion",
                table: "BaseClientes",
                newName: "Nit");

            migrationBuilder.RenameColumn(
                name: "PersonaFisica",
                table: "BaseClientes",
                newName: "Grupo");

            migrationBuilder.RenameColumn(
                name: "NumeroCliente",
                table: "BaseClientes",
                newName: "Email");

            migrationBuilder.RenameColumn(
                name: "ModificadoPor",
                table: "BaseClientes",
                newName: "Direccion");

            migrationBuilder.RenameColumn(
                name: "IdTipoNif",
                table: "BaseClientes",
                newName: "CondicionesPago");

            migrationBuilder.RenameColumn(
                name: "IdPais",
                table: "BaseClientes",
                newName: "CodigoPostal");

            migrationBuilder.RenameColumn(
                name: "IdGrupoClientes",
                table: "BaseClientes",
                newName: "Ciudad");
        }
    }
}
