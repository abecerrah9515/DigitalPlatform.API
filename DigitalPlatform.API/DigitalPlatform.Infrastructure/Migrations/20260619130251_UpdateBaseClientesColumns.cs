using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DigitalPlatform.Infrastructure.Migrations
{
    public partial class UpdateBaseClientesColumns : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "Addenda", table: "BaseClientes");
            migrationBuilder.DropColumn(name: "ClaseImpuesto", table: "BaseClientes");
            migrationBuilder.DropColumn(name: "CorreoElectronico", table: "BaseClientes");
            migrationBuilder.DropColumn(name: "FechaModificacion", table: "BaseClientes");
            migrationBuilder.DropColumn(name: "GpoClientes", table: "BaseClientes");
            migrationBuilder.DropColumn(name: "IdAddenda", table: "BaseClientes");
            migrationBuilder.DropColumn(name: "IdClaseImpuesto", table: "BaseClientes");
            migrationBuilder.DropColumn(name: "IdGrupoClientes", table: "BaseClientes");
            migrationBuilder.DropColumn(name: "IdPais", table: "BaseClientes");
            migrationBuilder.DropColumn(name: "IdTipoNif", table: "BaseClientes");
            migrationBuilder.DropColumn(name: "ModificadoPor", table: "BaseClientes");
            migrationBuilder.DropColumn(name: "PersonaFisica", table: "BaseClientes");
            migrationBuilder.DropColumn(name: "Poblacion", table: "BaseClientes");
            migrationBuilder.DropColumn(name: "TipoNif", table: "BaseClientes");

            migrationBuilder.RenameColumn(
                name: "NumeroCliente",
                table: "BaseClientes",
                newName: "NumeroCuenta");

            migrationBuilder.RenameColumn(
                name: "Nombre",
                table: "BaseClientes",
                newName: "NombreCliente");

            migrationBuilder.RenameColumn(
                name: "CampoClasificacion",
                table: "BaseClientes",
                newName: "CampoClas");

            migrationBuilder.AddColumn<string>(
                name: "Calle",
                table: "BaseClientes",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "NIT",
                table: "BaseClientes",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "NombreContacto",
                table: "BaseClientes",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CorreoContabilidad",
                table: "BaseClientes",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "CorreoContabilidad", table: "BaseClientes");
            migrationBuilder.DropColumn(name: "NombreContacto", table: "BaseClientes");
            migrationBuilder.DropColumn(name: "NIT", table: "BaseClientes");
            migrationBuilder.DropColumn(name: "Calle", table: "BaseClientes");

            migrationBuilder.RenameColumn(
                name: "NumeroCuenta",
                table: "BaseClientes",
                newName: "NumeroCliente");

            migrationBuilder.RenameColumn(
                name: "NombreCliente",
                table: "BaseClientes",
                newName: "Nombre");

            migrationBuilder.RenameColumn(
                name: "CampoClas",
                table: "BaseClientes",
                newName: "CampoClasificacion");

            migrationBuilder.AddColumn<string>(name: "Addenda", table: "BaseClientes", type: "text", nullable: false, defaultValue: "");
            migrationBuilder.AddColumn<string>(name: "ClaseImpuesto", table: "BaseClientes", type: "text", nullable: false, defaultValue: "");
            migrationBuilder.AddColumn<string>(name: "CorreoElectronico", table: "BaseClientes", type: "text", nullable: false, defaultValue: "");
            migrationBuilder.AddColumn<DateTime>(name: "FechaModificacion", table: "BaseClientes", type: "timestamp with time zone", nullable: true);
            migrationBuilder.AddColumn<string>(name: "GpoClientes", table: "BaseClientes", type: "text", nullable: false, defaultValue: "");
            migrationBuilder.AddColumn<string>(name: "IdAddenda", table: "BaseClientes", type: "text", nullable: false, defaultValue: "");
            migrationBuilder.AddColumn<string>(name: "IdClaseImpuesto", table: "BaseClientes", type: "text", nullable: false, defaultValue: "");
            migrationBuilder.AddColumn<string>(name: "IdGrupoClientes", table: "BaseClientes", type: "text", nullable: false, defaultValue: "");
            migrationBuilder.AddColumn<string>(name: "IdPais", table: "BaseClientes", type: "text", nullable: false, defaultValue: "");
            migrationBuilder.AddColumn<string>(name: "IdTipoNif", table: "BaseClientes", type: "text", nullable: false, defaultValue: "");
            migrationBuilder.AddColumn<string>(name: "ModificadoPor", table: "BaseClientes", type: "text", nullable: false, defaultValue: "");
            migrationBuilder.AddColumn<string>(name: "PersonaFisica", table: "BaseClientes", type: "text", nullable: false, defaultValue: "");
            migrationBuilder.AddColumn<string>(name: "Poblacion", table: "BaseClientes", type: "text", nullable: false, defaultValue: "");
            migrationBuilder.AddColumn<string>(name: "TipoNif", table: "BaseClientes", type: "text", nullable: false, defaultValue: "");
        }
    }
}
