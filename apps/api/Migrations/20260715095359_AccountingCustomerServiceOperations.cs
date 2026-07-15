using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mohandseto.Api.Migrations
{
    /// <inheritdoc />
    public partial class AccountingCustomerServiceOperations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "EscalatedAt",
                table: "SupportTickets",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "EscalatedBy",
                table: "SupportTickets",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EscalationReason",
                table: "SupportTickets",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "FirstResponseDueAt",
                table: "SupportTickets",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ResolutionDueAt",
                table: "SupportTickets",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Disposition",
                table: "ReturnItems",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DispositionAt",
                table: "ReturnItems",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DispositionBy",
                table: "ReturnItems",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InspectedCondition",
                table: "ReturnItems",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AccountingEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Number = table.Column<string>(type: "TEXT", nullable: false),
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: true),
                    CompanyId = table.Column<Guid>(type: "TEXT", nullable: true),
                    InvoiceId = table.Column<Guid>(type: "TEXT", nullable: true),
                    ReturnRequestId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Type = table.Column<int>(type: "INTEGER", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    Category = table.Column<string>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: false),
                    Reference = table.Column<string>(type: "TEXT", nullable: true),
                    Amount = table.Column<decimal>(type: "TEXT", precision: 18, nullable: false),
                    TaxAmount = table.Column<decimal>(type: "TEXT", precision: 18, nullable: false),
                    OccurredAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    PostedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    PostedBy = table.Column<Guid>(type: "TEXT", nullable: true),
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
                    table.PrimaryKey("PK_AccountingEntries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FinancialPeriods",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    StartsAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    EndsAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    Revenue = table.Column<decimal>(type: "TEXT", precision: 18, nullable: false),
                    Collections = table.Column<decimal>(type: "TEXT", precision: 18, nullable: false),
                    Expenses = table.Column<decimal>(type: "TEXT", precision: 18, nullable: false),
                    SalesTax = table.Column<decimal>(type: "TEXT", precision: 18, nullable: false),
                    NetProfit = table.Column<decimal>(type: "TEXT", precision: 18, nullable: false),
                    ClosedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ClosedBy = table.Column<Guid>(type: "TEXT", nullable: true),
                    ClosingNote = table.Column<string>(type: "TEXT", nullable: true),
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
                    table.PrimaryKey("PK_FinancialPeriods", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SupportReplyTemplates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Title = table.Column<string>(type: "TEXT", nullable: false),
                    Type = table.Column<int>(type: "INTEGER", nullable: true),
                    Body = table.Column<string>(type: "TEXT", nullable: false),
                    UsageCount = table.Column<int>(type: "INTEGER", nullable: false),
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
                    table.PrimaryKey("PK_SupportReplyTemplates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SupportSlaPolicies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Type = table.Column<int>(type: "INTEGER", nullable: false),
                    Priority = table.Column<int>(type: "INTEGER", nullable: false),
                    FirstResponseMinutes = table.Column<int>(type: "INTEGER", nullable: false),
                    ResolutionMinutes = table.Column<int>(type: "INTEGER", nullable: false),
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
                    table.PrimaryKey("PK_SupportSlaPolicies", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AccountingEntries_Number",
                table: "AccountingEntries",
                column: "Number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AccountingEntries_Type_Status_OccurredAt",
                table: "AccountingEntries",
                columns: new[] { "Type", "Status", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_FinancialPeriods_StartsAt_EndsAt",
                table: "FinancialPeriods",
                columns: new[] { "StartsAt", "EndsAt" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SupportReplyTemplates_Type_Title",
                table: "SupportReplyTemplates",
                columns: new[] { "Type", "Title" });

            migrationBuilder.CreateIndex(
                name: "IX_SupportSlaPolicies_Type_Priority",
                table: "SupportSlaPolicies",
                columns: new[] { "Type", "Priority" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AccountingEntries");

            migrationBuilder.DropTable(
                name: "FinancialPeriods");

            migrationBuilder.DropTable(
                name: "SupportReplyTemplates");

            migrationBuilder.DropTable(
                name: "SupportSlaPolicies");

            migrationBuilder.DropColumn(
                name: "EscalatedAt",
                table: "SupportTickets");

            migrationBuilder.DropColumn(
                name: "EscalatedBy",
                table: "SupportTickets");

            migrationBuilder.DropColumn(
                name: "EscalationReason",
                table: "SupportTickets");

            migrationBuilder.DropColumn(
                name: "FirstResponseDueAt",
                table: "SupportTickets");

            migrationBuilder.DropColumn(
                name: "ResolutionDueAt",
                table: "SupportTickets");

            migrationBuilder.DropColumn(
                name: "Disposition",
                table: "ReturnItems");

            migrationBuilder.DropColumn(
                name: "DispositionAt",
                table: "ReturnItems");

            migrationBuilder.DropColumn(
                name: "DispositionBy",
                table: "ReturnItems");

            migrationBuilder.DropColumn(
                name: "InspectedCondition",
                table: "ReturnItems");
        }
    }
}
