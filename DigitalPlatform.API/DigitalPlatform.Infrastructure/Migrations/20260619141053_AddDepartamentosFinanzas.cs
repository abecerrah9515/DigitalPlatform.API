using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace DigitalPlatform.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDepartamentosFinanzas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DepartamentosFinanzas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nombre = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DepartamentosFinanzas", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "DepartamentosFinanzas",
                columns: new[] { "Id", "Nombre" },
                values: new object[,]
                {
                    { 1, "Gerencia" },
                    { 2, "Cartera" },
                    { 3, "IT" },
                    { 4, "Contabilidad" },
                    { 5, "Recursos Humanos" },
                    { 6, "Comercial" },
                    { 7, "Operaciones" },
                    { 8, "Otro" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_DepartamentosFinanzas_Nombre",
                table: "DepartamentosFinanzas",
                column: "Nombre",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DepartamentosFinanzas");
        }
    }
}
