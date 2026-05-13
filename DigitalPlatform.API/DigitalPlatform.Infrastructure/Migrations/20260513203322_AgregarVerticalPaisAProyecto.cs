using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DigitalPlatform.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AgregarVerticalPaisAProyecto : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Pais",
                table: "Proyectos",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Vertical",
                table: "Proyectos",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Pais",
                table: "Proyectos");

            migrationBuilder.DropColumn(
                name: "Vertical",
                table: "Proyectos");
        }
    }
}
