using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace FeriadoTracker.Web.Migrations
{
    /// <inheritdoc />
    public partial class InitialSeed : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Feriados",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "Data", "Nome" },
                values: new object[] { new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Confraternização Universal" });

            migrationBuilder.UpdateData(
                table: "Feriados",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "Data", "Nome" },
                values: new object[] { new DateTime(2026, 4, 3, 0, 0, 0, 0, DateTimeKind.Unspecified), "Sexta-feira Santa" });

            migrationBuilder.InsertData(
                table: "Feriados",
                columns: new[] { "Id", "Data", "Nome", "Tipo" },
                values: new object[,]
                {
                    { 4, new DateTime(2026, 5, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Dia do Trabalho", "Nacional" },
                    { 5, new DateTime(2026, 9, 7, 0, 0, 0, 0, DateTimeKind.Unspecified), "Independência do Brasil", "Nacional" },
                    { 6, new DateTime(2026, 10, 12, 0, 0, 0, 0, DateTimeKind.Unspecified), "Nossa Senhora Aparecida", "Nacional" },
                    { 7, new DateTime(2026, 11, 2, 0, 0, 0, 0, DateTimeKind.Unspecified), "Finados", "Nacional" },
                    { 8, new DateTime(2026, 11, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), "Proclamação da República", "Nacional" },
                    { 9, new DateTime(2026, 12, 25, 0, 0, 0, 0, DateTimeKind.Unspecified), "Natal", "Nacional" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Feriados",
                keyColumn: "Id",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "Feriados",
                keyColumn: "Id",
                keyValue: 5);

            migrationBuilder.DeleteData(
                table: "Feriados",
                keyColumn: "Id",
                keyValue: 6);

            migrationBuilder.DeleteData(
                table: "Feriados",
                keyColumn: "Id",
                keyValue: 7);

            migrationBuilder.DeleteData(
                table: "Feriados",
                keyColumn: "Id",
                keyValue: 8);

            migrationBuilder.DeleteData(
                table: "Feriados",
                keyColumn: "Id",
                keyValue: 9);

            migrationBuilder.UpdateData(
                table: "Feriados",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "Data", "Nome" },
                values: new object[] { new DateTime(2025, 12, 25, 0, 0, 0, 0, DateTimeKind.Unspecified), "Natal" });

            migrationBuilder.UpdateData(
                table: "Feriados",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "Data", "Nome" },
                values: new object[] { new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Ano Novo" });
        }
    }
}
