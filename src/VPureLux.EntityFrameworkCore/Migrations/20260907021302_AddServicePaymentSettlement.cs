using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VPureLux.Migrations
{
    /// <inheritdoc />
    public partial class AddServicePaymentSettlement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "RecordedAt",
                table: "AppServicePayments",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "RecordedBy",
                table: "AppServicePayments",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RequestHash",
                table: "AppServicePayments",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VoidIdempotencyKey",
                table: "AppServicePayments",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VoidReason",
                table: "AppServicePayments",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VoidRequestHash",
                table: "AppServicePayments",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "VoidedAt",
                table: "AppServicePayments",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "VoidedBy",
                table: "AppServicePayments",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AppServiceRefunds",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ServiceOrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    RefundDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Method = table.Column<byte>(type: "tinyint", nullable: false),
                    ReferenceNo = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    IdempotencyKey = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    RequestHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    RecordedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RecordedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExtraProperties = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppServiceRefunds", x => x.Id);
                    table.CheckConstraint("CK_ServiceRefunds_PositiveAmount", "[Amount] > 0");
                    table.ForeignKey(
                        name: "FK_AppServiceRefunds_AppCustomers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "AppCustomers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AppServiceRefunds_AppServiceOrders_ServiceOrderId",
                        column: x => x.ServiceOrderId,
                        principalTable: "AppServiceOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ServicePayments_Order_History",
                table: "AppServicePayments",
                columns: new[] { "ServiceOrderId", "PaymentDate", "CreationTime", "Id" });

            migrationBuilder.CreateIndex(
                name: "UX_ServicePayments_VoidIdempotencyKey",
                table: "AppServicePayments",
                column: "VoidIdempotencyKey",
                unique: true,
                filter: "[VoidIdempotencyKey] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AppServiceRefunds_CustomerId",
                table: "AppServiceRefunds",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceRefunds_Order_History",
                table: "AppServiceRefunds",
                columns: new[] { "ServiceOrderId", "RefundDate", "CreationTime", "Id" });

            migrationBuilder.CreateIndex(
                name: "UX_ServiceRefunds_IdempotencyKey",
                table: "AppServiceRefunds",
                column: "IdempotencyKey",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AppServiceRefunds");

            migrationBuilder.DropIndex(
                name: "IX_ServicePayments_Order_History",
                table: "AppServicePayments");

            migrationBuilder.DropIndex(
                name: "UX_ServicePayments_VoidIdempotencyKey",
                table: "AppServicePayments");

            migrationBuilder.DropColumn(
                name: "RecordedAt",
                table: "AppServicePayments");

            migrationBuilder.DropColumn(
                name: "RecordedBy",
                table: "AppServicePayments");

            migrationBuilder.DropColumn(
                name: "RequestHash",
                table: "AppServicePayments");

            migrationBuilder.DropColumn(
                name: "VoidIdempotencyKey",
                table: "AppServicePayments");

            migrationBuilder.DropColumn(
                name: "VoidReason",
                table: "AppServicePayments");

            migrationBuilder.DropColumn(
                name: "VoidRequestHash",
                table: "AppServicePayments");

            migrationBuilder.DropColumn(
                name: "VoidedAt",
                table: "AppServicePayments");

            migrationBuilder.DropColumn(
                name: "VoidedBy",
                table: "AppServicePayments");
        }
    }
}
