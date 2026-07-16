using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace DigitalPlatform.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPnlCuentasYMovimientosGR55 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CuentasPnl",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    LineItemId = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    AccountName = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    ParentId = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    Nivel = table.Column<int>(type: "integer", nullable: false),
                    TipoFinanciero = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Referencia = table.Column<string>(type: "text", nullable: false),
                    Orden = table.Column<int>(type: "integer", nullable: false),
                    ConsolidacionId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CuentasPnl", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CuentasPnl_ConsolidacionLogs_ConsolidacionId",
                        column: x => x.ConsolidacionId,
                        principalTable: "ConsolidacionLogs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MovimientosGR55",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    NumeroCuenta = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    Año = table.Column<int>(type: "integer", nullable: false),
                    Mes = table.Column<int>(type: "integer", nullable: false),
                    CodProyecto = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Cliente = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Vertical = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Valor = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    ConsolidacionId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MovimientosGR55", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MovimientosGR55_ConsolidacionLogs_ConsolidacionId",
                        column: x => x.ConsolidacionId,
                        principalTable: "ConsolidacionLogs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CuentasPnl_ConsolidacionId_ParentId",
                table: "CuentasPnl",
                columns: new[] { "ConsolidacionId", "ParentId" });

            migrationBuilder.CreateIndex(
                name: "IX_MovimientosGR55_ConsolidacionId_NumeroCuenta_Año_Mes",
                table: "MovimientosGR55",
                columns: new[] { "ConsolidacionId", "NumeroCuenta", "Año", "Mes" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CuentasPnl");

            migrationBuilder.DropTable(
                name: "MovimientosGR55");
        }
    }
}
