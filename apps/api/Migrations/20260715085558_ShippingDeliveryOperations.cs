using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mohandseto.Api.Migrations
{
    /// <inheritdoc />
    public partial class ShippingDeliveryOperations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CustomerContactChannel",
                table: "Shipments",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CustomerContactedAt",
                table: "Shipments",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeliveryAttempt",
                table: "Shipments",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "DeliveryCost",
                table: "Shipments",
                type: "TEXT",
                precision: 18,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "DeliveryZone",
                table: "Shipments",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "DestinationLatitude",
                table: "Shipments",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "DestinationLongitude",
                table: "Shipments",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DriverUserId",
                table: "Shipments",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "FailedAt",
                table: "Shipments",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FailureReason",
                table: "Shipments",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "RescheduledAt",
                table: "Shipments",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ScheduledAt",
                table: "Shipments",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "StartedAt",
                table: "Shipments",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "WeightKg",
                table: "Shipments",
                type: "TEXT",
                precision: 18,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<double>(
                name: "Latitude",
                table: "DeliveryProofs",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Longitude",
                table: "DeliveryProofs",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ShipmentId",
                table: "DeliveryProofs",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "DeliveryRoutes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Code = table.Column<string>(type: "TEXT", nullable: false),
                    DriverUserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    DriverName = table.Column<string>(type: "TEXT", nullable: false),
                    RouteDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    OriginLatitude = table.Column<double>(type: "REAL", nullable: true),
                    OriginLongitude = table.Column<double>(type: "REAL", nullable: true),
                    TotalDistanceKm = table.Column<decimal>(type: "TEXT", precision: 18, nullable: false),
                    EstimatedMinutes = table.Column<int>(type: "INTEGER", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
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
                    table.PrimaryKey("PK_DeliveryRoutes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DeliveryZones",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    NameAr = table.Column<string>(type: "TEXT", nullable: false),
                    Governorate = table.Column<string>(type: "TEXT", nullable: false),
                    CitiesCsv = table.Column<string>(type: "TEXT", nullable: true),
                    BaseFee = table.Column<decimal>(type: "TEXT", precision: 18, nullable: false),
                    FeePerKg = table.Column<decimal>(type: "TEXT", precision: 18, nullable: false),
                    FeePerKm = table.Column<decimal>(type: "TEXT", precision: 18, nullable: false),
                    EstimatedDays = table.Column<int>(type: "INTEGER", nullable: false),
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
                    table.PrimaryKey("PK_DeliveryZones", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DeliveryRouteStops",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    RouteId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ShipmentId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Sequence = table.Column<int>(type: "INTEGER", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    ScheduledAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ArrivedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
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
                    table.PrimaryKey("PK_DeliveryRouteStops", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DeliveryRouteStops_DeliveryRoutes_RouteId",
                        column: x => x.RouteId,
                        principalTable: "DeliveryRoutes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Shipments_DriverUserId_Status_ScheduledAt",
                table: "Shipments",
                columns: new[] { "DriverUserId", "Status", "ScheduledAt" });

            migrationBuilder.CreateIndex(
                name: "IX_DeliveryRoutes_Code",
                table: "DeliveryRoutes",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DeliveryRoutes_DriverUserId_RouteDate",
                table: "DeliveryRoutes",
                columns: new[] { "DriverUserId", "RouteDate" });

            migrationBuilder.CreateIndex(
                name: "IX_DeliveryRouteStops_RouteId_Sequence",
                table: "DeliveryRouteStops",
                columns: new[] { "RouteId", "Sequence" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DeliveryRouteStops_ShipmentId",
                table: "DeliveryRouteStops",
                column: "ShipmentId");

            migrationBuilder.CreateIndex(
                name: "IX_DeliveryZones_Governorate_NameAr",
                table: "DeliveryZones",
                columns: new[] { "Governorate", "NameAr" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DeliveryRouteStops");

            migrationBuilder.DropTable(
                name: "DeliveryZones");

            migrationBuilder.DropTable(
                name: "DeliveryRoutes");

            migrationBuilder.DropIndex(
                name: "IX_Shipments_DriverUserId_Status_ScheduledAt",
                table: "Shipments");

            migrationBuilder.DropColumn(
                name: "CustomerContactChannel",
                table: "Shipments");

            migrationBuilder.DropColumn(
                name: "CustomerContactedAt",
                table: "Shipments");

            migrationBuilder.DropColumn(
                name: "DeliveryAttempt",
                table: "Shipments");

            migrationBuilder.DropColumn(
                name: "DeliveryCost",
                table: "Shipments");

            migrationBuilder.DropColumn(
                name: "DeliveryZone",
                table: "Shipments");

            migrationBuilder.DropColumn(
                name: "DestinationLatitude",
                table: "Shipments");

            migrationBuilder.DropColumn(
                name: "DestinationLongitude",
                table: "Shipments");

            migrationBuilder.DropColumn(
                name: "DriverUserId",
                table: "Shipments");

            migrationBuilder.DropColumn(
                name: "FailedAt",
                table: "Shipments");

            migrationBuilder.DropColumn(
                name: "FailureReason",
                table: "Shipments");

            migrationBuilder.DropColumn(
                name: "RescheduledAt",
                table: "Shipments");

            migrationBuilder.DropColumn(
                name: "ScheduledAt",
                table: "Shipments");

            migrationBuilder.DropColumn(
                name: "StartedAt",
                table: "Shipments");

            migrationBuilder.DropColumn(
                name: "WeightKg",
                table: "Shipments");

            migrationBuilder.DropColumn(
                name: "Latitude",
                table: "DeliveryProofs");

            migrationBuilder.DropColumn(
                name: "Longitude",
                table: "DeliveryProofs");

            migrationBuilder.DropColumn(
                name: "ShipmentId",
                table: "DeliveryProofs");
        }
    }
}
