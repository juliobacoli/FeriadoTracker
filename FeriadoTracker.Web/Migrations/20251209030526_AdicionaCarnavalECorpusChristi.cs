using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace FeriadoTracker.Web.Migrations
{
    /// <inheritdoc />
    public partial class AdicionaCarnavalECorpusChristi : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Feriados",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "Data", "Nome" },
                values: new object[] { new DateTime(2026, 2, 16, 0, 0, 0, 0, DateTimeKind.Unspecified), "Carnaval" });

            migrationBuilder.UpdateData(
                table: "Feriados",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "Data", "Nome" },
                values: new object[] { new DateTime(2026, 2, 17, 0, 0, 0, 0, DateTimeKind.Unspecified), "Carnaval" });

            migrationBuilder.UpdateData(
                table: "Feriados",
                keyColumn: "Id",
                keyValue: 5,
                columns: new[] { "Data", "Nome" },
                values: new object[] { new DateTime(2026, 4, 3, 0, 0, 0, 0, DateTimeKind.Unspecified), "Sexta-feira Santa" });

            migrationBuilder.UpdateData(
                table: "Feriados",
                keyColumn: "Id",
                keyValue: 6,
                columns: new[] { "Data", "Nome" },
                values: new object[] { new DateTime(2026, 4, 21, 0, 0, 0, 0, DateTimeKind.Unspecified), "Tiradentes" });

            migrationBuilder.UpdateData(
                table: "Feriados",
                keyColumn: "Id",
                keyValue: 7,
                columns: new[] { "Data", "Nome" },
                values: new object[] { new DateTime(2026, 5, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Dia do Trabalho" });

            migrationBuilder.UpdateData(
                table: "Feriados",
                keyColumn: "Id",
                keyValue: 8,
                columns: new[] { "Data", "Nome" },
                values: new object[] { new DateTime(2026, 6, 4, 0, 0, 0, 0, DateTimeKind.Unspecified), "Corpus Christi" });

            migrationBuilder.UpdateData(
                table: "Feriados",
                keyColumn: "Id",
                keyValue: 9,
                columns: new[] { "Data", "Nome" },
                values: new object[] { new DateTime(2026, 9, 7, 0, 0, 0, 0, DateTimeKind.Unspecified), "Independência do Brasil" });

            migrationBuilder.UpdateData(
                table: "Feriados",
                keyColumn: "Id",
                keyValue: 10,
                columns: new[] { "Data", "Nome" },
                values: new object[] { new DateTime(2026, 10, 12, 0, 0, 0, 0, DateTimeKind.Unspecified), "Nossa Senhora Aparecida" });

            migrationBuilder.UpdateData(
                table: "Feriados",
                keyColumn: "Id",
                keyValue: 11,
                columns: new[] { "Data", "Nome" },
                values: new object[] { new DateTime(2026, 11, 2, 0, 0, 0, 0, DateTimeKind.Unspecified), "Finados" });

            migrationBuilder.InsertData(
                table: "Feriados",
                columns: new[] { "Id", "Data", "Nome", "Tipo" },
                values: new object[,]
                {
                    { 12, new DateTime(2026, 11, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), "Proclamação da República", "Nacional" },
                    { 13, new DateTime(2026, 11, 20, 0, 0, 0, 0, DateTimeKind.Unspecified), "Dia Nacional da Consciência Negra", "Nacional" },
                    { 14, new DateTime(2026, 12, 25, 0, 0, 0, 0, DateTimeKind.Unspecified), "Natal", "Nacional" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Feriados",
                keyColumn: "Id",
                keyValue: 12);

            migrationBuilder.DeleteData(
                table: "Feriados",
                keyColumn: "Id",
                keyValue: 13);

            migrationBuilder.DeleteData(
                table: "Feriados",
                keyColumn: "Id",
                keyValue: 14);

            migrationBuilder.UpdateData(
                table: "Feriados",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "Data", "Nome" },
                values: new object[] { new DateTime(2026, 4, 3, 0, 0, 0, 0, DateTimeKind.Unspecified), "Sexta-feira Santa" });

            migrationBuilder.UpdateData(
                table: "Feriados",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "Data", "Nome" },
                values: new object[] { new DateTime(2026, 4, 21, 0, 0, 0, 0, DateTimeKind.Unspecified), "Tiradentes" });

            migrationBuilder.UpdateData(
                table: "Feriados",
                keyColumn: "Id",
                keyValue: 5,
                columns: new[] { "Data", "Nome" },
                values: new object[] { new DateTime(2026, 5, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Dia do Trabalho" });

            migrationBuilder.UpdateData(
                table: "Feriados",
                keyColumn: "Id",
                keyValue: 6,
                columns: new[] { "Data", "Nome" },
                values: new object[] { new DateTime(2026, 9, 7, 0, 0, 0, 0, DateTimeKind.Unspecified), "Independência do Brasil" });

            migrationBuilder.UpdateData(
                table: "Feriados",
                keyColumn: "Id",
                keyValue: 7,
                columns: new[] { "Data", "Nome" },
                values: new object[] { new DateTime(2026, 10, 12, 0, 0, 0, 0, DateTimeKind.Unspecified), "Nossa Senhora Aparecida" });

            migrationBuilder.UpdateData(
                table: "Feriados",
                keyColumn: "Id",
                keyValue: 8,
                columns: new[] { "Data", "Nome" },
                values: new object[] { new DateTime(2026, 11, 2, 0, 0, 0, 0, DateTimeKind.Unspecified), "Finados" });

            migrationBuilder.UpdateData(
                table: "Feriados",
                keyColumn: "Id",
                keyValue: 9,
                columns: new[] { "Data", "Nome" },
                values: new object[] { new DateTime(2026, 11, 20, 0, 0, 0, 0, DateTimeKind.Unspecified), "Dia Nacional da Consciência Negra" });

            migrationBuilder.UpdateData(
                table: "Feriados",
                keyColumn: "Id",
                keyValue: 10,
                columns: new[] { "Data", "Nome" },
                values: new object[] { new DateTime(2026, 11, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), "Proclamação da República" });

            migrationBuilder.UpdateData(
                table: "Feriados",
                keyColumn: "Id",
                keyValue: 11,
                columns: new[] { "Data", "Nome" },
                values: new object[] { new DateTime(2026, 12, 25, 0, 0, 0, 0, DateTimeKind.Unspecified), "Natal" });
        }
    }
}
