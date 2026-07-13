using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mohandseto.Api.Migrations
{
    /// <inheritdoc />
    public partial class RfqWorkflow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "SourceRfqId",
                table: "Orders",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Rfqs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Number = table.Column<string>(type: "TEXT", nullable: false),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Title = table.Column<string>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    RequiredDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    QuoteDeadline = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    DeliveryGovernorate = table.Column<string>(type: "TEXT", nullable: true),
                    AcceptedQuoteId = table.Column<Guid>(type: "TEXT", nullable: true),
                    ConvertedOrderId = table.Column<Guid>(type: "TEXT", nullable: true),
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
                    table.PrimaryKey("PK_Rfqs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SupplierQuoteRequests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    RfqId = table.Column<Guid>(type: "TEXT", nullable: false),
                    SupplierId = table.Column<Guid>(type: "TEXT", nullable: false),
                    SentAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Deadline = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Status = table.Column<string>(type: "TEXT", nullable: false),
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
                    table.PrimaryKey("PK_SupplierQuoteRequests", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SupplierQuotes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    RfqId = table.Column<Guid>(type: "TEXT", nullable: false),
                    SupplierId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Number = table.Column<string>(type: "TEXT", nullable: false),
                    Total = table.Column<decimal>(type: "TEXT", precision: 18, nullable: false),
                    ValidUntil = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Status = table.Column<string>(type: "TEXT", nullable: false),
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
                    table.PrimaryKey("PK_SupplierQuotes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CustomerQuotes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    RfqId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Number = table.Column<string>(type: "TEXT", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    CurrentVersion = table.Column<int>(type: "INTEGER", nullable: false),
                    AcceptedVersionId = table.Column<Guid>(type: "TEXT", nullable: true),
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
                    table.PrimaryKey("PK_CustomerQuotes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CustomerQuotes_Rfqs_RfqId",
                        column: x => x.RfqId,
                        principalTable: "Rfqs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "QuoteNegotiations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    RfqId = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    IsStaff = table.Column<bool>(type: "INTEGER", nullable: false),
                    Type = table.Column<int>(type: "INTEGER", nullable: false),
                    Message = table.Column<string>(type: "TEXT", nullable: false),
                    ProposedTotal = table.Column<decimal>(type: "TEXT", precision: 18, nullable: true),
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
                    table.PrimaryKey("PK_QuoteNegotiations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_QuoteNegotiations_Rfqs_RfqId",
                        column: x => x.RfqId,
                        principalTable: "Rfqs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RfqAttachments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    RfqId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Type = table.Column<int>(type: "INTEGER", nullable: false),
                    OriginalName = table.Column<string>(type: "TEXT", nullable: false),
                    StoredPath = table.Column<string>(type: "TEXT", nullable: false),
                    ContentType = table.Column<string>(type: "TEXT", nullable: false),
                    SizeBytes = table.Column<long>(type: "INTEGER", nullable: false),
                    ExtractionStatus = table.Column<string>(type: "TEXT", nullable: false),
                    ExtractionError = table.Column<string>(type: "TEXT", nullable: true),
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
                    table.PrimaryKey("PK_RfqAttachments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RfqAttachments_Rfqs_RfqId",
                        column: x => x.RfqId,
                        principalTable: "Rfqs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RfqItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    RfqId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ProductId = table.Column<Guid>(type: "TEXT", nullable: true),
                    DescriptionAr = table.Column<string>(type: "TEXT", nullable: false),
                    SkuHint = table.Column<string>(type: "TEXT", nullable: true),
                    Quantity = table.Column<decimal>(type: "TEXT", precision: 18, nullable: false),
                    UnitName = table.Column<string>(type: "TEXT", nullable: false),
                    Specifications = table.Column<string>(type: "TEXT", nullable: true),
                    PreferredBrand = table.Column<string>(type: "TEXT", nullable: true),
                    AllowAlternatives = table.Column<bool>(type: "INTEGER", nullable: false),
                    Source = table.Column<int>(type: "INTEGER", nullable: false),
                    ExtractionConfidence = table.Column<decimal>(type: "TEXT", precision: 18, nullable: false),
                    IsReviewed = table.Column<bool>(type: "INTEGER", nullable: false),
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
                    table.PrimaryKey("PK_RfqItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RfqItems_Rfqs_RfqId",
                        column: x => x.RfqId,
                        principalTable: "Rfqs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SupplierQuoteItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    SupplierQuoteId = table.Column<Guid>(type: "TEXT", nullable: false),
                    RfqItemId = table.Column<Guid>(type: "TEXT", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "TEXT", precision: 18, nullable: false),
                    Quantity = table.Column<decimal>(type: "TEXT", precision: 18, nullable: false),
                    LineTotal = table.Column<decimal>(type: "TEXT", precision: 18, nullable: false),
                    AlternativeDescription = table.Column<string>(type: "TEXT", nullable: true),
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
                    table.PrimaryKey("PK_SupplierQuoteItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SupplierQuoteItems_SupplierQuotes_SupplierQuoteId",
                        column: x => x.SupplierQuoteId,
                        principalTable: "SupplierQuotes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CustomerQuoteVersions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    QuoteId = table.Column<Guid>(type: "TEXT", nullable: false),
                    VersionNumber = table.Column<int>(type: "INTEGER", nullable: false),
                    Subtotal = table.Column<decimal>(type: "TEXT", precision: 18, nullable: false),
                    Tax = table.Column<decimal>(type: "TEXT", precision: 18, nullable: false),
                    Shipping = table.Column<decimal>(type: "TEXT", precision: 18, nullable: false),
                    Total = table.Column<decimal>(type: "TEXT", precision: 18, nullable: false),
                    ValidUntil = table.Column<DateTime>(type: "TEXT", nullable: false),
                    DeliveryDays = table.Column<int>(type: "INTEGER", nullable: false),
                    Terms = table.Column<string>(type: "TEXT", nullable: true),
                    ChangeSummary = table.Column<string>(type: "TEXT", nullable: true),
                    SentAt = table.Column<DateTime>(type: "TEXT", nullable: false),
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
                    table.PrimaryKey("PK_CustomerQuoteVersions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CustomerQuoteVersions_CustomerQuotes_QuoteId",
                        column: x => x.QuoteId,
                        principalTable: "CustomerQuotes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CustomerQuoteItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    VersionId = table.Column<Guid>(type: "TEXT", nullable: false),
                    RfqItemId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ProductId = table.Column<Guid>(type: "TEXT", nullable: true),
                    DescriptionAr = table.Column<string>(type: "TEXT", nullable: false),
                    Quantity = table.Column<decimal>(type: "TEXT", precision: 18, nullable: false),
                    UnitName = table.Column<string>(type: "TEXT", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "TEXT", precision: 18, nullable: false),
                    LineTotal = table.Column<decimal>(type: "TEXT", precision: 18, nullable: false),
                    IsAlternative = table.Column<bool>(type: "INTEGER", nullable: false),
                    AlternativeReason = table.Column<string>(type: "TEXT", nullable: true),
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
                    table.PrimaryKey("PK_CustomerQuoteItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CustomerQuoteItems_CustomerQuoteVersions_VersionId",
                        column: x => x.VersionId,
                        principalTable: "CustomerQuoteVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CustomerQuoteItems_VersionId",
                table: "CustomerQuoteItems",
                column: "VersionId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerQuotes_Number",
                table: "CustomerQuotes",
                column: "Number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CustomerQuotes_RfqId",
                table: "CustomerQuotes",
                column: "RfqId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerQuoteVersions_QuoteId_VersionNumber",
                table: "CustomerQuoteVersions",
                columns: new[] { "QuoteId", "VersionNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_QuoteNegotiations_RfqId",
                table: "QuoteNegotiations",
                column: "RfqId");

            migrationBuilder.CreateIndex(
                name: "IX_RfqAttachments_RfqId",
                table: "RfqAttachments",
                column: "RfqId");

            migrationBuilder.CreateIndex(
                name: "IX_RfqItems_RfqId",
                table: "RfqItems",
                column: "RfqId");

            migrationBuilder.CreateIndex(
                name: "IX_Rfqs_Number",
                table: "Rfqs",
                column: "Number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Rfqs_TenantId_UserId_Status_CreatedAt",
                table: "Rfqs",
                columns: new[] { "TenantId", "UserId", "Status", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_SupplierQuoteItems_SupplierQuoteId",
                table: "SupplierQuoteItems",
                column: "SupplierQuoteId");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierQuotes_Number",
                table: "SupplierQuotes",
                column: "Number",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CustomerQuoteItems");

            migrationBuilder.DropTable(
                name: "QuoteNegotiations");

            migrationBuilder.DropTable(
                name: "RfqAttachments");

            migrationBuilder.DropTable(
                name: "RfqItems");

            migrationBuilder.DropTable(
                name: "SupplierQuoteItems");

            migrationBuilder.DropTable(
                name: "SupplierQuoteRequests");

            migrationBuilder.DropTable(
                name: "CustomerQuoteVersions");

            migrationBuilder.DropTable(
                name: "SupplierQuotes");

            migrationBuilder.DropTable(
                name: "CustomerQuotes");

            migrationBuilder.DropTable(
                name: "Rfqs");

            migrationBuilder.DropColumn(
                name: "SourceRfqId",
                table: "Orders");
        }
    }
}
