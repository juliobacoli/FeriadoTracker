using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FeriadoTracker.Web.Migrations
{
    /// <inheritdoc />
    public partial class ThirdSeed : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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

            migrationBuilder.InsertData(
                table: "Feriados",
                columns: new[] { "Id", "Data", "Nome", "Tipo" },
                values: new object[] { 11, new DateTime(2026, 12, 25, 0, 0, 0, 0, DateTimeKind.Unspecified), "Natal", "Nacional" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Feriados",
                keyColumn: "Id",
                keyValue: 11);

            migrationBuilder.UpdateData(
                table: "Feriados",
                keyColumn: "Id",
                keyValue: 9,
                columns: new[] { "Data", "Nome" },
                values: new object[] { new DateTime(2026, 11, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), "Proclamação da República" });

            migrationBuilder.UpdateData(
                table: "Feriados",
                keyColumn: "Id",
                keyValue: 10,
                columns: new[] { "Data", "Nome" },
                values: new object[] { new DateTime(2026, 12, 25, 0, 0, 0, 0, DateTimeKind.Unspecified), "Natal" });
        }
    }
}
