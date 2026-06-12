using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace DigitalPlatform.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveCargasFila : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CargasFila");

            migrationBuilder.AddColumn<string>(
                name: "RutaArchivo",
                table: "CargasArchivo",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RutaArchivo",
                table: "CargasArchivo");

            migrationBuilder.CreateTable(
                name: "CargasFila",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CargaArchivoId = table.Column<int>(type: "integer", nullable: false),
                    DatosJson = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CargasFila", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CargasFila_CargasArchivo_CargaArchivoId",
                        column: x => x.CargaArchivoId,
                        principalTable: "CargasArchivo",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CargasFila_CargaArchivoId",
                table: "CargasFila",
                column: "CargaArchivoId");
        }
    }
}
