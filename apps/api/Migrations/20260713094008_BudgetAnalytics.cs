using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mohandseto.Api.Migrations
{
    /// <inheritdoc />
    public partial class BudgetAnalytics : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BudgetAdjustmentRequests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    CostCenterId = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CurrentBudget = table.Column<decimal>(type: "TEXT", precision: 18, nullable: false),
                    RequestedBudget = table.Column<decimal>(type: "TEXT", precision: 18, nullable: false),
                    Reason = table.Column<string>(type: "TEXT", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    DecisionNote = table.Column<string>(type: "TEXT", nullable: true),
                    DecidedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DecidedBy = table.Column<Guid>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "TEXT", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "TEXT", nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "BLOB", nullable: true),
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BudgetAdjustmentRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BudgetAdjustmentRequests_CostCenters_CostCenterId",
                        column: x => x.CostCenterId,
                        principalTable: "CostCenters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BudgetAdjustmentRequests_CostCenterId",
                table: "BudgetAdjustmentRequests",
                column: "CostCenterId");

            migrationBuilder.CreateIndex(
                name: "IX_BudgetAdjustmentRequests_TenantId_CostCenterId_Status_CreatedAt",
                table: "BudgetAdjustmentRequests",
                columns: new[] { "TenantId", "CostCenterId", "Status", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BudgetAdjustmentRequests");
        }
    }
}
