using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mohandseto.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddSystemMonitoring : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BlockedIpAddresses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    IpAddress = table.Column<string>(type: "TEXT", nullable: false),
                    Reason = table.Column<string>(type: "TEXT", nullable: false),
                    Location = table.Column<string>(type: "TEXT", nullable: true),
                    FailedAttempts = table.Column<int>(type: "INTEGER", nullable: false),
                    BlockedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    BlockedBy = table.Column<Guid>(type: "TEXT", nullable: false),
                    UnblockedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    UnblockedBy = table.Column<Guid>(type: "TEXT", nullable: true),
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
                    table.PrimaryKey("PK_BlockedIpAddresses", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FeatureFlags",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Key = table.Column<string>(type: "TEXT", nullable: false),
                    NameAr = table.Column<string>(type: "TEXT", nullable: false),
                    DescriptionAr = table.Column<string>(type: "TEXT", nullable: false),
                    IsEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    Scope = table.Column<int>(type: "INTEGER", nullable: false),
                    RolloutPercent = table.Column<int>(type: "INTEGER", nullable: false),
                    TargetTenantIdsCsv = table.Column<string>(type: "TEXT", nullable: true),
                    TargetUserIdsCsv = table.Column<string>(type: "TEXT", nullable: true),
                    StartsAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    EndsAt = table.Column<DateTime>(type: "TEXT", nullable: true),
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
                    table.PrimaryKey("PK_FeatureFlags", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SuspiciousActivities",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Fingerprint = table.Column<string>(type: "TEXT", nullable: false),
                    Type = table.Column<string>(type: "TEXT", nullable: false),
                    Severity = table.Column<int>(type: "INTEGER", nullable: false),
                    TitleAr = table.Column<string>(type: "TEXT", nullable: false),
                    DescriptionAr = table.Column<string>(type: "TEXT", nullable: false),
                    Identifier = table.Column<string>(type: "TEXT", nullable: true),
                    IpAddress = table.Column<string>(type: "TEXT", nullable: true),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: true),
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    DetectedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ReviewedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ReviewedBy = table.Column<Guid>(type: "TEXT", nullable: true),
                    ReviewNote = table.Column<string>(type: "TEXT", nullable: true),
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
                    table.PrimaryKey("PK_SuspiciousActivities", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SystemErrorEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Fingerprint = table.Column<string>(type: "TEXT", nullable: false),
                    Number = table.Column<string>(type: "TEXT", nullable: false),
                    Severity = table.Column<int>(type: "INTEGER", nullable: false),
                    Service = table.Column<string>(type: "TEXT", nullable: false),
                    Message = table.Column<string>(type: "TEXT", nullable: false),
                    ExceptionType = table.Column<string>(type: "TEXT", nullable: true),
                    StackTrace = table.Column<string>(type: "TEXT", nullable: true),
                    ContextJson = table.Column<string>(type: "TEXT", nullable: true),
                    CorrelationId = table.Column<string>(type: "TEXT", nullable: true),
                    Path = table.Column<string>(type: "TEXT", nullable: true),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: true),
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: true),
                    OccurrenceCount = table.Column<int>(type: "INTEGER", nullable: false),
                    FirstOccurredAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastOccurredAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ResolvedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ResolvedBy = table.Column<Guid>(type: "TEXT", nullable: true),
                    ResolutionNote = table.Column<string>(type: "TEXT", nullable: true),
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
                    table.PrimaryKey("PK_SystemErrorEvents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SystemRestoreRequests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    BackupId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Environment = table.Column<string>(type: "TEXT", nullable: false),
                    Reason = table.Column<string>(type: "TEXT", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    VerifiedSha256 = table.Column<string>(type: "TEXT", nullable: false),
                    RequestedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    RequestedBy = table.Column<Guid>(type: "TEXT", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Error = table.Column<string>(type: "TEXT", nullable: true),
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
                    table.PrimaryKey("PK_SystemRestoreRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SystemRestoreRequests_SystemBackups_BackupId",
                        column: x => x.BackupId,
                        principalTable: "SystemBackups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SystemVersions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Version = table.Column<string>(type: "TEXT", nullable: false),
                    TitleAr = table.Column<string>(type: "TEXT", nullable: false),
                    NotesAr = table.Column<string>(type: "TEXT", nullable: true),
                    Environment = table.Column<string>(type: "TEXT", nullable: false),
                    CommitSha = table.Column<string>(type: "TEXT", nullable: true),
                    IsStable = table.Column<bool>(type: "INTEGER", nullable: false),
                    ReleasedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ReleasedBy = table.Column<Guid>(type: "TEXT", nullable: false),
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
                    table.PrimaryKey("PK_SystemVersions", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BlockedIpAddresses_IpAddress_IsActive",
                table: "BlockedIpAddresses",
                columns: new[] { "IpAddress", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_FeatureFlags_Key",
                table: "FeatureFlags",
                column: "Key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SuspiciousActivities_Fingerprint_Status",
                table: "SuspiciousActivities",
                columns: new[] { "Fingerprint", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_SuspiciousActivities_Status_DetectedAt",
                table: "SuspiciousActivities",
                columns: new[] { "Status", "DetectedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_SystemErrorEvents_Fingerprint_ResolvedAt",
                table: "SystemErrorEvents",
                columns: new[] { "Fingerprint", "ResolvedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_SystemErrorEvents_Severity_LastOccurredAt",
                table: "SystemErrorEvents",
                columns: new[] { "Severity", "LastOccurredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_SystemRestoreRequests_BackupId",
                table: "SystemRestoreRequests",
                column: "BackupId");

            migrationBuilder.CreateIndex(
                name: "IX_SystemRestoreRequests_Status_RequestedAt",
                table: "SystemRestoreRequests",
                columns: new[] { "Status", "RequestedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_SystemVersions_Version",
                table: "SystemVersions",
                column: "Version",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BlockedIpAddresses");

            migrationBuilder.DropTable(
                name: "FeatureFlags");

            migrationBuilder.DropTable(
                name: "SuspiciousActivities");

            migrationBuilder.DropTable(
                name: "SystemErrorEvents");

            migrationBuilder.DropTable(
                name: "SystemRestoreRequests");

            migrationBuilder.DropTable(
                name: "SystemVersions");
        }
    }
}
