using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FeriadoTracker.Web.Migrations
{
    /// <inheritdoc />
    public partial class AdicionaNotificationLog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "NotificationLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SubscriptionId = table.Column<int>(type: "INTEGER", nullable: false),
                    FeriadoId = table.Column<int>(type: "INTEGER", nullable: false),
                    SentDate = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    SentAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotificationLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NotificationLogs_Feriados_FeriadoId",
                        column: x => x.FeriadoId,
                        principalTable: "Feriados",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_NotificationLogs_PushSubscriptions_SubscriptionId",
                        column: x => x.SubscriptionId,
                        principalTable: "PushSubscriptions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_NotificationLogs_FeriadoId",
                table: "NotificationLogs",
                column: "FeriadoId");

            migrationBuilder.CreateIndex(
                name: "IX_NotificationLogs_SubscriptionId_FeriadoId_SentDate",
                table: "NotificationLogs",
                columns: new[] { "SubscriptionId", "FeriadoId", "SentDate" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "NotificationLogs");
        }
    }
}
