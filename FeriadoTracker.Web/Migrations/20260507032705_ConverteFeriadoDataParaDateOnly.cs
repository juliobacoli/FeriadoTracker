using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FeriadoTracker.Web.Migrations
{
    /// <inheritdoc />
    public partial class ConverteFeriadoDataParaDateOnly : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Feriados",
                keyColumn: "Id",
                keyValue: 1,
                column: "Data",
                value: new DateOnly(2025, 12, 25));

            migrationBuilder.UpdateData(
                table: "Feriados",
                keyColumn: "Id",
                keyValue: 2,
                column: "Data",
                value: new DateOnly(2026, 1, 1));

            migrationBuilder.UpdateData(
                table: "Feriados",
                keyColumn: "Id",
                keyValue: 3,
                column: "Data",
                value: new DateOnly(2026, 2, 16));

            migrationBuilder.UpdateData(
                table: "Feriados",
                keyColumn: "Id",
                keyValue: 4,
                column: "Data",
                value: new DateOnly(2026, 2, 17));

            migrationBuilder.UpdateData(
                table: "Feriados",
                keyColumn: "Id",
                keyValue: 5,
                column: "Data",
                value: new DateOnly(2026, 4, 3));

            migrationBuilder.UpdateData(
                table: "Feriados",
                keyColumn: "Id",
                keyValue: 6,
                column: "Data",
                value: new DateOnly(2026, 4, 21));

            migrationBuilder.UpdateData(
                table: "Feriados",
                keyColumn: "Id",
                keyValue: 7,
                column: "Data",
                value: new DateOnly(2026, 5, 1));

            migrationBuilder.UpdateData(
                table: "Feriados",
                keyColumn: "Id",
                keyValue: 8,
                column: "Data",
                value: new DateOnly(2026, 6, 4));

            migrationBuilder.UpdateData(
                table: "Feriados",
                keyColumn: "Id",
                keyValue: 9,
                column: "Data",
                value: new DateOnly(2026, 9, 7));

            migrationBuilder.UpdateData(
                table: "Feriados",
                keyColumn: "Id",
                keyValue: 10,
                column: "Data",
                value: new DateOnly(2026, 10, 12));

            migrationBuilder.UpdateData(
                table: "Feriados",
                keyColumn: "Id",
                keyValue: 11,
                column: "Data",
                value: new DateOnly(2026, 11, 2));

            migrationBuilder.UpdateData(
                table: "Feriados",
                keyColumn: "Id",
                keyValue: 12,
                column: "Data",
                value: new DateOnly(2026, 11, 15));

            migrationBuilder.UpdateData(
                table: "Feriados",
                keyColumn: "Id",
                keyValue: 13,
                column: "Data",
                value: new DateOnly(2026, 11, 20));

            migrationBuilder.UpdateData(
                table: "Feriados",
                keyColumn: "Id",
                keyValue: 14,
                column: "Data",
                value: new DateOnly(2026, 12, 25));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Feriados",
                keyColumn: "Id",
                keyValue: 1,
                column: "Data",
                value: new DateTime(2025, 12, 25, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.UpdateData(
                table: "Feriados",
                keyColumn: "Id",
                keyValue: 2,
                column: "Data",
                value: new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.UpdateData(
                table: "Feriados",
                keyColumn: "Id",
                keyValue: 3,
                column: "Data",
                value: new DateTime(2026, 2, 16, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.UpdateData(
                table: "Feriados",
                keyColumn: "Id",
                keyValue: 4,
                column: "Data",
                value: new DateTime(2026, 2, 17, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.UpdateData(
                table: "Feriados",
                keyColumn: "Id",
                keyValue: 5,
                column: "Data",
                value: new DateTime(2026, 4, 3, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.UpdateData(
                table: "Feriados",
                keyColumn: "Id",
                keyValue: 6,
                column: "Data",
                value: new DateTime(2026, 4, 21, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.UpdateData(
                table: "Feriados",
                keyColumn: "Id",
                keyValue: 7,
                column: "Data",
                value: new DateTime(2026, 5, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.UpdateData(
                table: "Feriados",
                keyColumn: "Id",
                keyValue: 8,
                column: "Data",
                value: new DateTime(2026, 6, 4, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.UpdateData(
                table: "Feriados",
                keyColumn: "Id",
                keyValue: 9,
                column: "Data",
                value: new DateTime(2026, 9, 7, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.UpdateData(
                table: "Feriados",
                keyColumn: "Id",
                keyValue: 10,
                column: "Data",
                value: new DateTime(2026, 10, 12, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.UpdateData(
                table: "Feriados",
                keyColumn: "Id",
                keyValue: 11,
                column: "Data",
                value: new DateTime(2026, 11, 2, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.UpdateData(
                table: "Feriados",
                keyColumn: "Id",
                keyValue: 12,
                column: "Data",
                value: new DateTime(2026, 11, 15, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.UpdateData(
                table: "Feriados",
                keyColumn: "Id",
                keyValue: 13,
                column: "Data",
                value: new DateTime(2026, 11, 20, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.UpdateData(
                table: "Feriados",
                keyColumn: "Id",
                keyValue: 14,
                column: "Data",
                value: new DateTime(2026, 12, 25, 0, 0, 0, 0, DateTimeKind.Unspecified));
        }
    }
}
