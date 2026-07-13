using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mohandseto.Api.Migrations
{
    /// <inheritdoc />
    public partial class AdminContentOperations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ContentDispatches",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Channel = table.Column<int>(type: "INTEGER", nullable: false),
                    TitleAr = table.Column<string>(type: "TEXT", nullable: false),
                    BodyAr = table.Column<string>(type: "TEXT", nullable: false),
                    ActionUrl = table.Column<string>(type: "TEXT", nullable: true),
                    TargetTenantId = table.Column<Guid>(type: "TEXT", nullable: true),
                    ScheduledAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    SentAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    RecipientCount = table.Column<int>(type: "INTEGER", nullable: false),
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
                    table.PrimaryKey("PK_ContentDispatches", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "HomeBanners",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    TitleAr = table.Column<string>(type: "TEXT", nullable: false),
                    SubtitleAr = table.Column<string>(type: "TEXT", nullable: true),
                    ImageUrl = table.Column<string>(type: "TEXT", nullable: false),
                    ActionUrl = table.Column<string>(type: "TEXT", nullable: true),
                    TargetTenantId = table.Column<Guid>(type: "TEXT", nullable: true),
                    StartsAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    EndsAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    SortOrder = table.Column<int>(type: "INTEGER", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
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
                    table.PrimaryKey("PK_HomeBanners", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "HomeSections",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Key = table.Column<string>(type: "TEXT", nullable: false),
                    NameAr = table.Column<string>(type: "TEXT", nullable: false),
                    SortOrder = table.Column<int>(type: "INTEGER", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    SettingsJson = table.Column<string>(type: "TEXT", nullable: true),
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
                    table.PrimaryKey("PK_HomeSections", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ContentDispatches_Status_ScheduledAt",
                table: "ContentDispatches",
                columns: new[] { "Status", "ScheduledAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ContentDispatches_TargetTenantId_CreatedAt",
                table: "ContentDispatches",
                columns: new[] { "TargetTenantId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_HomeBanners_IsActive_StartsAt_EndsAt",
                table: "HomeBanners",
                columns: new[] { "IsActive", "StartsAt", "EndsAt" });

            migrationBuilder.CreateIndex(
                name: "IX_HomeBanners_TargetTenantId_SortOrder",
                table: "HomeBanners",
                columns: new[] { "TargetTenantId", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_HomeSections_Key",
                table: "HomeSections",
                column: "Key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_HomeSections_SortOrder_IsActive",
                table: "HomeSections",
                columns: new[] { "SortOrder", "IsActive" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ContentDispatches");

            migrationBuilder.DropTable(
                name: "HomeBanners");

            migrationBuilder.DropTable(
                name: "HomeSections");
        }
    }
}
