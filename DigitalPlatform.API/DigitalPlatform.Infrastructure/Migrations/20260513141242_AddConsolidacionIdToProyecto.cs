using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DigitalPlatform.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddConsolidacionIdToProyecto : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ConsolidacionId",
                table: "Proyectos",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Proyectos_ConsolidacionId",
                table: "Proyectos",
                column: "ConsolidacionId");

            migrationBuilder.AddForeignKey(
                name: "FK_Proyectos_ConsolidacionLogs_ConsolidacionId",
                table: "Proyectos",
                column: "ConsolidacionId",
                principalTable: "ConsolidacionLogs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Proyectos_ConsolidacionLogs_ConsolidacionId",
                table: "Proyectos");

            migrationBuilder.DropIndex(
                name: "IX_Proyectos_ConsolidacionId",
                table: "Proyectos");

            migrationBuilder.DropColumn(
                name: "ConsolidacionId",
                table: "Proyectos");
        }
    }
}
