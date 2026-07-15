using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mohandseto.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddReportingEngine : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SavedReports",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Source = table.Column<string>(type: "TEXT", nullable: false),
                    FieldsJson = table.Column<string>(type: "TEXT", nullable: false),
                    FiltersJson = table.Column<string>(type: "TEXT", nullable: false),
                    GroupBy = table.Column<string>(type: "TEXT", nullable: true),
                    ChartType = table.Column<string>(type: "TEXT", nullable: false),
                    IsFavorite = table.Column<bool>(type: "INTEGER", nullable: false),
                    OwnerUserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ScheduleFrequency = table.Column<int>(type: "INTEGER", nullable: false),
                    ScheduleDay = table.Column<int>(type: "INTEGER", nullable: true),
                    ScheduleTime = table.Column<TimeOnly>(type: "TEXT", nullable: true),
                    FormatsCsv = table.Column<string>(type: "TEXT", nullable: false),
                    RecipientsCsv = table.Column<string>(type: "TEXT", nullable: true),
                    IsScheduleActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    NextRunAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    LastRunAt = table.Column<DateTime>(type: "TEXT", nullable: true),
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
                    table.PrimaryKey("PK_SavedReports", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ReportRuns",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    SavedReportId = table.Column<Guid>(type: "TEXT", nullable: true),
                    ReportCode = table.Column<string>(type: "TEXT", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    RowCount = table.Column<int>(type: "INTEGER", nullable: false),
                    FormatsCsv = table.Column<string>(type: "TEXT", nullable: false),
                    RecipientsCsv = table.Column<string>(type: "TEXT", nullable: true),
                    StartedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
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
                    table.PrimaryKey("PK_ReportRuns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReportRuns_SavedReports_SavedReportId",
                        column: x => x.SavedReportId,
                        principalTable: "SavedReports",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_ReportRuns_SavedReportId_StartedAt",
                table: "ReportRuns",
                columns: new[] { "SavedReportId", "StartedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_SavedReports_IsScheduleActive_NextRunAt",
                table: "SavedReports",
                columns: new[] { "IsScheduleActive", "NextRunAt" });

            migrationBuilder.CreateIndex(
                name: "IX_SavedReports_OwnerUserId_Name",
                table: "SavedReports",
                columns: new[] { "OwnerUserId", "Name" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ReportRuns");

            migrationBuilder.DropTable(
                name: "SavedReports");
        }
    }
}
