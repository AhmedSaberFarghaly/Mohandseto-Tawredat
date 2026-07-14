using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mohandseto.Api.Migrations
{
    /// <inheritdoc />
    public partial class PrintingDesignOperations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ActualCompletion",
                table: "ProductionJobs",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ActualStart",
                table: "ProductionJobs",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DispatchReference",
                table: "ProductionJobs",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PackageCount",
                table: "ProductionJobs",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "PackagingType",
                table: "ProductionJobs",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ProducedQuantity",
                table: "ProductionJobs",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "UnitsPerPackage",
                table: "ProductionJobs",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "HasSimpleEffects",
                table: "LogoAssets",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "HasSufficientResolution",
                table: "LogoAssets",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "HasTransparentBackground",
                table: "LogoAssets",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsCmykReady",
                table: "LogoAssets",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsVector",
                table: "LogoAssets",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "QualityScore",
                table: "LogoAssets",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "QualityStatus",
                table: "LogoAssets",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "ReviewNote",
                table: "LogoAssets",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ReviewedAt",
                table: "LogoAssets",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ReviewedBy",
                table: "LogoAssets",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SentByUserId",
                table: "DesignVersions",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "SentToCustomerAt",
                table: "DesignVersions",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "AssignedDesignerId",
                table: "CustomProductRequests",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DesignDueAt",
                table: "CustomProductRequests",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DesignSentAt",
                table: "CustomProductRequests",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ReadyAt",
                table: "CustomProductRequests",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_CustomProductRequests_AssignedDesignerId_DesignDueAt",
                table: "CustomProductRequests",
                columns: new[] { "AssignedDesignerId", "DesignDueAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CustomProductRequests_AssignedDesignerId_DesignDueAt",
                table: "CustomProductRequests");

            migrationBuilder.DropColumn(
                name: "ActualCompletion",
                table: "ProductionJobs");

            migrationBuilder.DropColumn(
                name: "ActualStart",
                table: "ProductionJobs");

            migrationBuilder.DropColumn(
                name: "DispatchReference",
                table: "ProductionJobs");

            migrationBuilder.DropColumn(
                name: "PackageCount",
                table: "ProductionJobs");

            migrationBuilder.DropColumn(
                name: "PackagingType",
                table: "ProductionJobs");

            migrationBuilder.DropColumn(
                name: "ProducedQuantity",
                table: "ProductionJobs");

            migrationBuilder.DropColumn(
                name: "UnitsPerPackage",
                table: "ProductionJobs");

            migrationBuilder.DropColumn(
                name: "HasSimpleEffects",
                table: "LogoAssets");

            migrationBuilder.DropColumn(
                name: "HasSufficientResolution",
                table: "LogoAssets");

            migrationBuilder.DropColumn(
                name: "HasTransparentBackground",
                table: "LogoAssets");

            migrationBuilder.DropColumn(
                name: "IsCmykReady",
                table: "LogoAssets");

            migrationBuilder.DropColumn(
                name: "IsVector",
                table: "LogoAssets");

            migrationBuilder.DropColumn(
                name: "QualityScore",
                table: "LogoAssets");

            migrationBuilder.DropColumn(
                name: "QualityStatus",
                table: "LogoAssets");

            migrationBuilder.DropColumn(
                name: "ReviewNote",
                table: "LogoAssets");

            migrationBuilder.DropColumn(
                name: "ReviewedAt",
                table: "LogoAssets");

            migrationBuilder.DropColumn(
                name: "ReviewedBy",
                table: "LogoAssets");

            migrationBuilder.DropColumn(
                name: "SentByUserId",
                table: "DesignVersions");

            migrationBuilder.DropColumn(
                name: "SentToCustomerAt",
                table: "DesignVersions");

            migrationBuilder.DropColumn(
                name: "AssignedDesignerId",
                table: "CustomProductRequests");

            migrationBuilder.DropColumn(
                name: "DesignDueAt",
                table: "CustomProductRequests");

            migrationBuilder.DropColumn(
                name: "DesignSentAt",
                table: "CustomProductRequests");

            migrationBuilder.DropColumn(
                name: "ReadyAt",
                table: "CustomProductRequests");
        }
    }
}
