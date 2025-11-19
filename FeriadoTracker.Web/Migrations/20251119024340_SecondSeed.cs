using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FeriadoTracker.Web.Migrations
{
    /// <inheritdoc />
    public partial class SecondSeed : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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
                values: new object[] { new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Confraternização Universal" });

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
                values: new object[] { new DateTime(2026, 11, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), "Proclamação da República" });

            migrationBuilder.InsertData(
                table: "Feriados",
                columns: new[] { "Id", "Data", "Nome", "Tipo" },
                values: new object[] { 10, new DateTime(2026, 12, 25, 0, 0, 0, 0, DateTimeKind.Unspecified), "Natal", "Nacional" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Feriados",
                keyColumn: "Id",
                keyValue: 10);

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

            migrationBuilder.UpdateData(
                table: "Feriados",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "Data", "Nome" },
                values: new object[] { new DateTime(2026, 4, 21, 0, 0, 0, 0, DateTimeKind.Unspecified), "Tiradentes" });

            migrationBuilder.UpdateData(
                table: "Feriados",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "Data", "Nome" },
                values: new object[] { new DateTime(2026, 5, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Dia do Trabalho" });

            migrationBuilder.UpdateData(
                table: "Feriados",
                keyColumn: "Id",
                keyValue: 5,
                columns: new[] { "Data", "Nome" },
                values: new object[] { new DateTime(2026, 9, 7, 0, 0, 0, 0, DateTimeKind.Unspecified), "Independência do Brasil" });

            migrationBuilder.UpdateData(
                table: "Feriados",
                keyColumn: "Id",
                keyValue: 6,
                columns: new[] { "Data", "Nome" },
                values: new object[] { new DateTime(2026, 10, 12, 0, 0, 0, 0, DateTimeKind.Unspecified), "Nossa Senhora Aparecida" });

            migrationBuilder.UpdateData(
                table: "Feriados",
                keyColumn: "Id",
                keyValue: 7,
                columns: new[] { "Data", "Nome" },
                values: new object[] { new DateTime(2026, 11, 2, 0, 0, 0, 0, DateTimeKind.Unspecified), "Finados" });

            migrationBuilder.UpdateData(
                table: "Feriados",
                keyColumn: "Id",
                keyValue: 8,
                columns: new[] { "Data", "Nome" },
                values: new object[] { new DateTime(2026, 11, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), "Proclamação da República" });

            migrationBuilder.UpdateData(
                table: "Feriados",
                keyColumn: "Id",
                keyValue: 9,
                columns: new[] { "Data", "Nome" },
                values: new object[] { new DateTime(2026, 12, 25, 0, 0, 0, 0, DateTimeKind.Unspecified), "Natal" });
        }
    }
}
