using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FeriadoTracker.Web.Migrations
{
    /// <inheritdoc />
    public partial class UmaNotificacaoPorFeriado : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_NotificationLogs_SubscriptionId_FeriadoId_SentDate",
                table: "NotificationLogs");

            migrationBuilder.CreateIndex(
                name: "IX_NotificationLogs_SubscriptionId_FeriadoId",
                table: "NotificationLogs",
                columns: new[] { "SubscriptionId", "FeriadoId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_NotificationLogs_SubscriptionId_FeriadoId",
                table: "NotificationLogs");

            migrationBuilder.CreateIndex(
                name: "IX_NotificationLogs_SubscriptionId_FeriadoId_SentDate",
                table: "NotificationLogs",
                columns: new[] { "SubscriptionId", "FeriadoId", "SentDate" },
                unique: true);
        }
    }
}
