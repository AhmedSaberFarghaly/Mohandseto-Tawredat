using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mohandseto.Api.Migrations
{
    /// <inheritdoc />
    public partial class ContractLifecycleOperations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ActivatedAt",
                table: "CompanyContracts",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ActivatedByUserId",
                table: "CompanyContracts",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "AnnualValue",
                table: "CompanyContracts",
                type: "TEXT",
                precision: 18,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "CreditReviewMonths",
                table: "CompanyContracts",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "CustomerNotifiedAt",
                table: "CompanyContracts",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeliveryHours",
                table: "CompanyContracts",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "DeliveryLatePenaltyPercent",
                table: "CompanyContracts",
                type: "TEXT",
                precision: 18,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "DeliveryPriority",
                table: "CompanyContracts",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "EarlyPaymentDays",
                table: "CompanyContracts",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "EarlyPaymentDiscountPercent",
                table: "CompanyContracts",
                type: "TEXT",
                precision: 18,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "ExpiryAlertDays",
                table: "CompanyContracts",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "FreeShipping",
                table: "CompanyContracts",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "LatePaymentPenaltyPercent",
                table: "CompanyContracts",
                type: "TEXT",
                precision: 18,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "MarketDiscountPercent",
                table: "CompanyContracts",
                type: "TEXT",
                precision: 18,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "PaymentMethod",
                table: "CompanyContracts",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "PricingMode",
                table: "CompanyContracts",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "RenewalRequiresApproval",
                table: "CompanyContracts",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "Type",
                table: "CompanyContracts",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "CompanyContractApprovals",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ContractId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Sequence = table.Column<int>(type: "INTEGER", nullable: false),
                    RoleCode = table.Column<string>(type: "TEXT", nullable: false),
                    LabelAr = table.Column<string>(type: "TEXT", nullable: false),
                    AssignedUserId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    DecidedByUserId = table.Column<Guid>(type: "TEXT", nullable: true),
                    DecidedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DecisionNote = table.Column<string>(type: "TEXT", nullable: true),
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
                    table.PrimaryKey("PK_CompanyContractApprovals", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CompanyContractApprovals_CompanyContracts_ContractId",
                        column: x => x.ContractId,
                        principalTable: "CompanyContracts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CompanyContractAttachments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ContractId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Type = table.Column<int>(type: "INTEGER", nullable: false),
                    FileName = table.Column<string>(type: "TEXT", nullable: false),
                    StoragePath = table.Column<string>(type: "TEXT", nullable: false),
                    ContentType = table.Column<string>(type: "TEXT", nullable: false),
                    SizeBytes = table.Column<long>(type: "INTEGER", nullable: false),
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
                    table.PrimaryKey("PK_CompanyContractAttachments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CompanyContractAttachments_CompanyContracts_ContractId",
                        column: x => x.ContractId,
                        principalTable: "CompanyContracts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CompanyContractProducts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ContractId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ProductId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ContractPrice = table.Column<decimal>(type: "TEXT", precision: 18, nullable: false),
                    EstimatedAnnualQuantity = table.Column<int>(type: "INTEGER", nullable: false),
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
                    table.PrimaryKey("PK_CompanyContractProducts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CompanyContractProducts_CompanyContracts_ContractId",
                        column: x => x.ContractId,
                        principalTable: "CompanyContracts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CompanyContractProducts_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CompanyContractQuantityTiers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ContractId = table.Column<Guid>(type: "TEXT", nullable: false),
                    MinQuantity = table.Column<int>(type: "INTEGER", nullable: false),
                    MaxQuantity = table.Column<int>(type: "INTEGER", nullable: true),
                    AdditionalDiscountPercent = table.Column<decimal>(type: "TEXT", precision: 18, nullable: false),
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
                    table.PrimaryKey("PK_CompanyContractQuantityTiers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CompanyContractQuantityTiers_CompanyContracts_ContractId",
                        column: x => x.ContractId,
                        principalTable: "CompanyContracts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ContractPriceRevisions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ContractId = table.Column<Guid>(type: "TEXT", nullable: false),
                    EffectiveAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CustomerApprovedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Reason = table.Column<string>(type: "TEXT", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
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
                    table.PrimaryKey("PK_ContractPriceRevisions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ContractPriceRevisions_CompanyContracts_ContractId",
                        column: x => x.ContractId,
                        principalTable: "CompanyContracts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ContractPriceRevisionItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    RevisionId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ProductId = table.Column<Guid>(type: "TEXT", nullable: false),
                    OldPrice = table.Column<decimal>(type: "TEXT", precision: 18, nullable: false),
                    NewPrice = table.Column<decimal>(type: "TEXT", precision: 18, nullable: false),
                    Reason = table.Column<string>(type: "TEXT", nullable: true),
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
                    table.PrimaryKey("PK_ContractPriceRevisionItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ContractPriceRevisionItems_ContractPriceRevisions_RevisionId",
                        column: x => x.RevisionId,
                        principalTable: "ContractPriceRevisions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CompanyContractApprovals_ContractId_Sequence",
                table: "CompanyContractApprovals",
                columns: new[] { "ContractId", "Sequence" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CompanyContractAttachments_ContractId",
                table: "CompanyContractAttachments",
                column: "ContractId");

            migrationBuilder.CreateIndex(
                name: "IX_CompanyContractProducts_ContractId_ProductId",
                table: "CompanyContractProducts",
                columns: new[] { "ContractId", "ProductId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CompanyContractProducts_ProductId",
                table: "CompanyContractProducts",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_CompanyContractQuantityTiers_ContractId_MinQuantity",
                table: "CompanyContractQuantityTiers",
                columns: new[] { "ContractId", "MinQuantity" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ContractPriceRevisionItems_RevisionId_ProductId",
                table: "ContractPriceRevisionItems",
                columns: new[] { "RevisionId", "ProductId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ContractPriceRevisions_ContractId_EffectiveAt",
                table: "ContractPriceRevisions",
                columns: new[] { "ContractId", "EffectiveAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CompanyContractApprovals");

            migrationBuilder.DropTable(
                name: "CompanyContractAttachments");

            migrationBuilder.DropTable(
                name: "CompanyContractProducts");

            migrationBuilder.DropTable(
                name: "CompanyContractQuantityTiers");

            migrationBuilder.DropTable(
                name: "ContractPriceRevisionItems");

            migrationBuilder.DropTable(
                name: "ContractPriceRevisions");

            migrationBuilder.DropColumn(
                name: "ActivatedAt",
                table: "CompanyContracts");

            migrationBuilder.DropColumn(
                name: "ActivatedByUserId",
                table: "CompanyContracts");

            migrationBuilder.DropColumn(
                name: "AnnualValue",
                table: "CompanyContracts");

            migrationBuilder.DropColumn(
                name: "CreditReviewMonths",
                table: "CompanyContracts");

            migrationBuilder.DropColumn(
                name: "CustomerNotifiedAt",
                table: "CompanyContracts");

            migrationBuilder.DropColumn(
                name: "DeliveryHours",
                table: "CompanyContracts");

            migrationBuilder.DropColumn(
                name: "DeliveryLatePenaltyPercent",
                table: "CompanyContracts");

            migrationBuilder.DropColumn(
                name: "DeliveryPriority",
                table: "CompanyContracts");

            migrationBuilder.DropColumn(
                name: "EarlyPaymentDays",
                table: "CompanyContracts");

            migrationBuilder.DropColumn(
                name: "EarlyPaymentDiscountPercent",
                table: "CompanyContracts");

            migrationBuilder.DropColumn(
                name: "ExpiryAlertDays",
                table: "CompanyContracts");

            migrationBuilder.DropColumn(
                name: "FreeShipping",
                table: "CompanyContracts");

            migrationBuilder.DropColumn(
                name: "LatePaymentPenaltyPercent",
                table: "CompanyContracts");

            migrationBuilder.DropColumn(
                name: "MarketDiscountPercent",
                table: "CompanyContracts");

            migrationBuilder.DropColumn(
                name: "PaymentMethod",
                table: "CompanyContracts");

            migrationBuilder.DropColumn(
                name: "PricingMode",
                table: "CompanyContracts");

            migrationBuilder.DropColumn(
                name: "RenewalRequiresApproval",
                table: "CompanyContracts");

            migrationBuilder.DropColumn(
                name: "Type",
                table: "CompanyContracts");
        }
    }
}
