using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace FeriadoTracker.Web.Migrations
{
    /// <inheritdoc />
    public partial class AdicionaFeriados2027 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Feriados",
                columns: new[] { "Id", "Data", "Nome", "Tipo" },
                values: new object[,]
                {
                    { 15, new DateOnly(2027, 1, 1), "Confraternização Universal", "Nacional" },
                    { 16, new DateOnly(2027, 2, 8), "Carnaval", "Nacional" },
                    { 17, new DateOnly(2027, 2, 9), "Carnaval", "Nacional" },
                    { 18, new DateOnly(2027, 3, 26), "Sexta-feira Santa", "Nacional" },
                    { 19, new DateOnly(2027, 4, 21), "Tiradentes", "Nacional" },
                    { 20, new DateOnly(2027, 5, 1), "Dia do Trabalho", "Nacional" },
                    { 21, new DateOnly(2027, 5, 27), "Corpus Christi", "Nacional" },
                    { 22, new DateOnly(2027, 9, 7), "Independência do Brasil", "Nacional" },
                    { 23, new DateOnly(2027, 10, 12), "Nossa Senhora Aparecida", "Nacional" },
                    { 24, new DateOnly(2027, 11, 2), "Finados", "Nacional" },
                    { 25, new DateOnly(2027, 11, 15), "Proclamação da República", "Nacional" },
                    { 26, new DateOnly(2027, 11, 20), "Dia Nacional da Consciência Negra", "Nacional" },
                    { 27, new DateOnly(2027, 12, 25), "Natal", "Nacional" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Feriados",
                keyColumn: "Id",
                keyValue: 15);

            migrationBuilder.DeleteData(
                table: "Feriados",
                keyColumn: "Id",
                keyValue: 16);

            migrationBuilder.DeleteData(
                table: "Feriados",
                keyColumn: "Id",
                keyValue: 17);

            migrationBuilder.DeleteData(
                table: "Feriados",
                keyColumn: "Id",
                keyValue: 18);

            migrationBuilder.DeleteData(
                table: "Feriados",
                keyColumn: "Id",
                keyValue: 19);

            migrationBuilder.DeleteData(
                table: "Feriados",
                keyColumn: "Id",
                keyValue: 20);

            migrationBuilder.DeleteData(
                table: "Feriados",
                keyColumn: "Id",
                keyValue: 21);

            migrationBuilder.DeleteData(
                table: "Feriados",
                keyColumn: "Id",
                keyValue: 22);

            migrationBuilder.DeleteData(
                table: "Feriados",
                keyColumn: "Id",
                keyValue: 23);

            migrationBuilder.DeleteData(
                table: "Feriados",
                keyColumn: "Id",
                keyValue: 24);

            migrationBuilder.DeleteData(
                table: "Feriados",
                keyColumn: "Id",
                keyValue: 25);

            migrationBuilder.DeleteData(
                table: "Feriados",
                keyColumn: "Id",
                keyValue: 26);

            migrationBuilder.DeleteData(
                table: "Feriados",
                keyColumn: "Id",
                keyValue: 27);
        }
    }
}
