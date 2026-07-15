using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mohandseto.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddIntegrationHub : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Endpoint",
                table: "IntegrationOperationLogs",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ErrorCode",
                table: "IntegrationOperationLogs",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsRetryable",
                table: "IntegrationOperationLogs",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "MaxAttempts",
                table: "IntegrationOperationLogs",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "NextRetryAt",
                table: "IntegrationOperationLogs",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ResolvedAt",
                table: "IntegrationOperationLogs",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "IntegrationConnections",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Code = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Provider = table.Column<string>(type: "TEXT", nullable: false),
                    ProtectedConfigJson = table.Column<string>(type: "TEXT", nullable: false),
                    IsConnected = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    Environment = table.Column<string>(type: "TEXT", nullable: true),
                    StatusMessage = table.Column<string>(type: "TEXT", nullable: true),
                    LastHealthCheckAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    LastSuccessfulSyncAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    NextSyncAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "TEXT", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "TEXT", nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "BLOB", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IntegrationConnections", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_IntegrationOperationLogs_Status_NextRetryAt",
                table: "IntegrationOperationLogs",
                columns: new[] { "Status", "NextRetryAt" });

            migrationBuilder.CreateIndex(
                name: "IX_IntegrationConnections_Code",
                table: "IntegrationConnections",
                column: "Code",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "IntegrationConnections");

            migrationBuilder.DropIndex(
                name: "IX_IntegrationOperationLogs_Status_NextRetryAt",
                table: "IntegrationOperationLogs");

            migrationBuilder.DropColumn(
                name: "Endpoint",
                table: "IntegrationOperationLogs");

            migrationBuilder.DropColumn(
                name: "ErrorCode",
                table: "IntegrationOperationLogs");

            migrationBuilder.DropColumn(
                name: "IsRetryable",
                table: "IntegrationOperationLogs");

            migrationBuilder.DropColumn(
                name: "MaxAttempts",
                table: "IntegrationOperationLogs");

            migrationBuilder.DropColumn(
                name: "NextRetryAt",
                table: "IntegrationOperationLogs");

            migrationBuilder.DropColumn(
                name: "ResolvedAt",
                table: "IntegrationOperationLogs");
        }
    }
}
