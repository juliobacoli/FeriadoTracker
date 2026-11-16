using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace FeriadoTracker.Web.Migrations
{
    /// <inheritdoc />
    public partial class AdicionaFeriadosRegionaisELocaisDoPush : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CodigoMunicipio",
                table: "Feriados",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CodigoUf",
                table: "Feriados",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Municipio",
                table: "Feriados",
                type: "TEXT",
                maxLength: 60,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Uf",
                table: "Feriados",
                type: "TEXT",
                maxLength: 2,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "PushSubscriptionLocais",
                columns: table => new
                {
                    SubscriptionId = table.Column<int>(type: "INTEGER", nullable: false),
                    Codigo = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PushSubscriptionLocais", x => new { x.SubscriptionId, x.Codigo });
                    table.ForeignKey(
                        name: "FK_PushSubscriptionLocais_PushSubscriptions_SubscriptionId",
                        column: x => x.SubscriptionId,
                        principalTable: "PushSubscriptions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.UpdateData(
                table: "Feriados",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CodigoMunicipio", "CodigoUf", "Municipio", "Uf" },
                values: new object[] { null, null, null, null });

            migrationBuilder.UpdateData(
                table: "Feriados",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CodigoMunicipio", "CodigoUf", "Municipio", "Uf" },
                values: new object[] { null, null, null, null });

            migrationBuilder.UpdateData(
                table: "Feriados",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "CodigoMunicipio", "CodigoUf", "Municipio", "Uf" },
                values: new object[] { null, null, null, null });

            migrationBuilder.UpdateData(
                table: "Feriados",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "CodigoMunicipio", "CodigoUf", "Municipio", "Uf" },
                values: new object[] { null, null, null, null });

            migrationBuilder.UpdateData(
                table: "Feriados",
                keyColumn: "Id",
                keyValue: 5,
                columns: new[] { "CodigoMunicipio", "CodigoUf", "Municipio", "Uf" },
                values: new object[] { null, null, null, null });

            migrationBuilder.UpdateData(
                table: "Feriados",
                keyColumn: "Id",
                keyValue: 6,
                columns: new[] { "CodigoMunicipio", "CodigoUf", "Municipio", "Uf" },
                values: new object[] { null, null, null, null });

            migrationBuilder.UpdateData(
                table: "Feriados",
                keyColumn: "Id",
                keyValue: 7,
                columns: new[] { "CodigoMunicipio", "CodigoUf", "Municipio", "Uf" },
                values: new object[] { null, null, null, null });

            migrationBuilder.UpdateData(
                table: "Feriados",
                keyColumn: "Id",
                keyValue: 8,
                columns: new[] { "CodigoMunicipio", "CodigoUf", "Municipio", "Uf" },
                values: new object[] { null, null, null, null });

            migrationBuilder.UpdateData(
                table: "Feriados",
                keyColumn: "Id",
                keyValue: 9,
                columns: new[] { "CodigoMunicipio", "CodigoUf", "Municipio", "Uf" },
                values: new object[] { null, null, null, null });

            migrationBuilder.UpdateData(
                table: "Feriados",
                keyColumn: "Id",
                keyValue: 10,
                columns: new[] { "CodigoMunicipio", "CodigoUf", "Municipio", "Uf" },
                values: new object[] { null, null, null, null });

            migrationBuilder.UpdateData(
                table: "Feriados",
                keyColumn: "Id",
                keyValue: 11,
                columns: new[] { "CodigoMunicipio", "CodigoUf", "Municipio", "Uf" },
                values: new object[] { null, null, null, null });

            migrationBuilder.UpdateData(
                table: "Feriados",
                keyColumn: "Id",
                keyValue: 12,
                columns: new[] { "CodigoMunicipio", "CodigoUf", "Municipio", "Uf" },
                values: new object[] { null, null, null, null });

            migrationBuilder.UpdateData(
                table: "Feriados",
                keyColumn: "Id",
                keyValue: 13,
                columns: new[] { "CodigoMunicipio", "CodigoUf", "Municipio", "Uf" },
                values: new object[] { null, null, null, null });

            migrationBuilder.UpdateData(
                table: "Feriados",
                keyColumn: "Id",
                keyValue: 14,
                columns: new[] { "CodigoMunicipio", "CodigoUf", "Municipio", "Uf" },
                values: new object[] { null, null, null, null });

            migrationBuilder.UpdateData(
                table: "Feriados",
                keyColumn: "Id",
                keyValue: 15,
                columns: new[] { "CodigoMunicipio", "CodigoUf", "Municipio", "Uf" },
                values: new object[] { null, null, null, null });

            migrationBuilder.UpdateData(
                table: "Feriados",
                keyColumn: "Id",
                keyValue: 16,
                columns: new[] { "CodigoMunicipio", "CodigoUf", "Municipio", "Uf" },
                values: new object[] { null, null, null, null });

            migrationBuilder.UpdateData(
                table: "Feriados",
                keyColumn: "Id",
                keyValue: 17,
                columns: new[] { "CodigoMunicipio", "CodigoUf", "Municipio", "Uf" },
                values: new object[] { null, null, null, null });

            migrationBuilder.UpdateData(
                table: "Feriados",
                keyColumn: "Id",
                keyValue: 18,
                columns: new[] { "CodigoMunicipio", "CodigoUf", "Municipio", "Uf" },
                values: new object[] { null, null, null, null });

            migrationBuilder.UpdateData(
                table: "Feriados",
                keyColumn: "Id",
                keyValue: 19,
                columns: new[] { "CodigoMunicipio", "CodigoUf", "Municipio", "Uf" },
                values: new object[] { null, null, null, null });

            migrationBuilder.UpdateData(
                table: "Feriados",
                keyColumn: "Id",
                keyValue: 20,
                columns: new[] { "CodigoMunicipio", "CodigoUf", "Municipio", "Uf" },
                values: new object[] { null, null, null, null });

            migrationBuilder.UpdateData(
                table: "Feriados",
                keyColumn: "Id",
                keyValue: 21,
                columns: new[] { "CodigoMunicipio", "CodigoUf", "Municipio", "Uf" },
                values: new object[] { null, null, null, null });

            migrationBuilder.UpdateData(
                table: "Feriados",
                keyColumn: "Id",
                keyValue: 22,
                columns: new[] { "CodigoMunicipio", "CodigoUf", "Municipio", "Uf" },
                values: new object[] { null, null, null, null });

            migrationBuilder.UpdateData(
                table: "Feriados",
                keyColumn: "Id",
                keyValue: 23,
                columns: new[] { "CodigoMunicipio", "CodigoUf", "Municipio", "Uf" },
                values: new object[] { null, null, null, null });

            migrationBuilder.UpdateData(
                table: "Feriados",
                keyColumn: "Id",
                keyValue: 24,
                columns: new[] { "CodigoMunicipio", "CodigoUf", "Municipio", "Uf" },
                values: new object[] { null, null, null, null });

            migrationBuilder.UpdateData(
                table: "Feriados",
                keyColumn: "Id",
                keyValue: 25,
                columns: new[] { "CodigoMunicipio", "CodigoUf", "Municipio", "Uf" },
                values: new object[] { null, null, null, null });

            migrationBuilder.UpdateData(
                table: "Feriados",
                keyColumn: "Id",
                keyValue: 26,
                columns: new[] { "CodigoMunicipio", "CodigoUf", "Municipio", "Uf" },
                values: new object[] { null, null, null, null });

            migrationBuilder.UpdateData(
                table: "Feriados",
                keyColumn: "Id",
                keyValue: 27,
                columns: new[] { "CodigoMunicipio", "CodigoUf", "Municipio", "Uf" },
                values: new object[] { null, null, null, null });

            migrationBuilder.InsertData(
                table: "Feriados",
                columns: new[] { "Id", "CodigoMunicipio", "CodigoUf", "Data", "Municipio", "Nome", "Tipo", "Uf" },
                values: new object[,]
                {
                    { 28, null, 32, new DateOnly(2027, 4, 5), null, "Nossa Senhora da Penha", "Estadual", "ES" },
                    { 29, null, 15, new DateOnly(2027, 8, 15), null, "Adesão do Pará à Independência", "Estadual", "PA" },
                    { 30, null, 26, new DateOnly(2027, 3, 6), null, "Revolução Pernambucana", "Estadual", "PE" },
                    { 31, null, 42, new DateOnly(2027, 8, 15), null, "Dia de Santa Catarina", "Estadual", "SC" },
                    { 32, null, 35, new DateOnly(2027, 7, 9), null, "Revolução Constitucionalista", "Estadual", "SP" },
                    { 33, 3550308, 35, new DateOnly(2027, 1, 25), "São Paulo", "Aniversário de São Paulo", "Municipal", "SP" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PushSubscriptionLocais");

            migrationBuilder.DeleteData(
                table: "Feriados",
                keyColumn: "Id",
                keyValue: 28);

            migrationBuilder.DeleteData(
                table: "Feriados",
                keyColumn: "Id",
                keyValue: 29);

            migrationBuilder.DeleteData(
                table: "Feriados",
                keyColumn: "Id",
                keyValue: 30);

            migrationBuilder.DeleteData(
                table: "Feriados",
                keyColumn: "Id",
                keyValue: 31);

            migrationBuilder.DeleteData(
                table: "Feriados",
                keyColumn: "Id",
                keyValue: 32);

            migrationBuilder.DeleteData(
                table: "Feriados",
                keyColumn: "Id",
                keyValue: 33);

            migrationBuilder.DropColumn(
                name: "CodigoMunicipio",
                table: "Feriados");

            migrationBuilder.DropColumn(
                name: "CodigoUf",
                table: "Feriados");

            migrationBuilder.DropColumn(
                name: "Municipio",
                table: "Feriados");

            migrationBuilder.DropColumn(
                name: "Uf",
                table: "Feriados");
        }
    }
}
