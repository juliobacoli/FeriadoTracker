using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace FeriadoTracker.Web.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Feriados",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Nome = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Data = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Tipo = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Feriados", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "Feriados",
                columns: new[] { "Id", "Data", "Nome", "Tipo" },
                values: new object[,]
                {
                    { 1, new DateTime(2025, 12, 25, 0, 0, 0, 0, DateTimeKind.Unspecified), "Natal", "Nacional" },
                    { 2, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Ano Novo", "Nacional" },
                    { 3, new DateTime(2026, 4, 21, 0, 0, 0, 0, DateTimeKind.Unspecified), "Tiradentes", "Nacional" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Feriados");
        }
    }
}
