using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VPureLux.Migrations
{
    /// <inheritdoc />
    public partial class AddServiceCompletionFacts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "ActualCostAmount",
                table: "AppServiceOrders",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ActualProfitAmount",
                table: "AppServiceOrders",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CompletionCommandHash",
                table: "AppServiceOrders",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ActualCostAmount",
                table: "AppServiceOrderLines",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "InventoryTransactionLineId",
                table: "AppServiceOrderLines",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CompletionEventId",
                table: "AppAssetReplacementReminders",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ServiceOrderLineId",
                table: "AppAssetMaintenanceEvents",
                type: "uniqueidentifier",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ActualCostAmount",
                table: "AppServiceOrders");

            migrationBuilder.DropColumn(
                name: "ActualProfitAmount",
                table: "AppServiceOrders");

            migrationBuilder.DropColumn(
                name: "CompletionCommandHash",
                table: "AppServiceOrders");

            migrationBuilder.DropColumn(
                name: "ActualCostAmount",
                table: "AppServiceOrderLines");

            migrationBuilder.DropColumn(
                name: "InventoryTransactionLineId",
                table: "AppServiceOrderLines");

            migrationBuilder.DropColumn(
                name: "CompletionEventId",
                table: "AppAssetReplacementReminders");

            migrationBuilder.DropColumn(
                name: "ServiceOrderLineId",
                table: "AppAssetMaintenanceEvents");
        }
    }
}
