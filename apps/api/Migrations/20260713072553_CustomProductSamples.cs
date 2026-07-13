using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mohandseto.Api.Migrations
{
    /// <inheritdoc />
    public partial class CustomProductSamples : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PrintColorCount",
                table: "CustomRequestItems",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "PrintHeightCm",
                table: "CustomRequestItems",
                type: "TEXT",
                precision: 18,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "PrintWidthCm",
                table: "CustomRequestItems",
                type: "TEXT",
                precision: 18,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<Guid>(
                name: "OrderId",
                table: "CustomProductRequests",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "CustomLineTotal",
                table: "CartItems",
                type: "TEXT",
                precision: 18,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CustomProductRequestId",
                table: "CartItems",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "CustomUnitPrice",
                table: "CartItems",
                type: "TEXT",
                precision: 18,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ProductionSamples",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ProductionJobId = table.Column<Guid>(type: "TEXT", nullable: false),
                    VersionNumber = table.Column<int>(type: "INTEGER", nullable: false),
                    OriginalName = table.Column<string>(type: "TEXT", nullable: false),
                    StoredPath = table.Column<string>(type: "TEXT", nullable: false),
                    ContentType = table.Column<string>(type: "TEXT", nullable: false),
                    SizeBytes = table.Column<long>(type: "INTEGER", nullable: false),
                    Note = table.Column<string>(type: "TEXT", nullable: true),
                    Decision = table.Column<int>(type: "INTEGER", nullable: false),
                    DecidedBy = table.Column<Guid>(type: "TEXT", nullable: true),
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
                    table.PrimaryKey("PK_ProductionSamples", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductionSamples_ProductionJobs_ProductionJobId",
                        column: x => x.ProductionJobId,
                        principalTable: "ProductionJobs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProductionSamples_ProductionJobId_VersionNumber",
                table: "ProductionSamples",
                columns: new[] { "ProductionJobId", "VersionNumber" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProductionSamples");

            migrationBuilder.DropColumn(
                name: "PrintColorCount",
                table: "CustomRequestItems");

            migrationBuilder.DropColumn(
                name: "PrintHeightCm",
                table: "CustomRequestItems");

            migrationBuilder.DropColumn(
                name: "PrintWidthCm",
                table: "CustomRequestItems");

            migrationBuilder.DropColumn(
                name: "OrderId",
                table: "CustomProductRequests");

            migrationBuilder.DropColumn(
                name: "CustomLineTotal",
                table: "CartItems");

            migrationBuilder.DropColumn(
                name: "CustomProductRequestId",
                table: "CartItems");

            migrationBuilder.DropColumn(
                name: "CustomUnitPrice",
                table: "CartItems");
        }
    }
}
