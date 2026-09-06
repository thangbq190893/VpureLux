using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VPureLux.Migrations
{
    /// <inheritdoc />
    public partial class AddServiceModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AppServiceOrders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrderNo = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    CustomerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CustomerAssetId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WarehouseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrderDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ScheduledAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TechnicianUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Status = table.Column<byte>(type: "tinyint", nullable: false),
                    Currency = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    CustomerCodeSnapshot = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    CustomerNameSnapshot = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    AssetNoSnapshot = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    AssetNameSnapshot = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    ServiceAddress = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    Note = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ConfirmedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    StartedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CancelledAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompletionIdempotencyKey = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    InventoryTransactionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    TotalRevenueAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalCostAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalProfitAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    ExtraProperties = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifierId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DeleterId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DeletionTime = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppServiceOrders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AppServiceOrders_AbpUsers_TechnicianUserId",
                        column: x => x.TechnicianUserId,
                        principalTable: "AbpUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AppServiceOrders_AppCustomerAssets_CustomerAssetId",
                        column: x => x.CustomerAssetId,
                        principalTable: "AppCustomerAssets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AppServiceOrders_AppCustomers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "AppCustomers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AppServiceOrders_AppInventoryTransactions_InventoryTransactionId",
                        column: x => x.InventoryTransactionId,
                        principalTable: "AppInventoryTransactions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AppServiceOrders_AppWarehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalTable: "AppWarehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AppServiceWorks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    DefaultPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Status = table.Column<byte>(type: "tinyint", nullable: false),
                    Note = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ExtraProperties = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifierId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DeleterId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DeletionTime = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppServiceWorks", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AppServicePayments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ServiceOrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    PaymentDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PaymentMethod = table.Column<byte>(type: "tinyint", nullable: false),
                    ReferenceNo = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Note = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Status = table.Column<byte>(type: "tinyint", nullable: false),
                    IdempotencyKey = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    ExtraProperties = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifierId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DeleterId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DeletionTime = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppServicePayments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AppServicePayments_AppCustomers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "AppCustomers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AppServicePayments_AppServiceOrders_ServiceOrderId",
                        column: x => x.ServiceOrderId,
                        principalTable: "AppServiceOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AppServiceOrderLines",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LineNo = table.Column<int>(type: "int", nullable: false),
                    LineType = table.Column<byte>(type: "tinyint", nullable: false),
                    ComponentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CustomerAssetComponentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ServiceWorkId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ItemCodeSnapshot = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ItemNameSnapshot = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    UnitSnapshot = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    PlannedQuantity = table.Column<int>(type: "int", nullable: false),
                    ActualQuantity = table.Column<int>(type: "int", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    RevenueAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CostAmountSnapshot = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Note = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ServiceOrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppServiceOrderLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AppServiceOrderLines_AppComponents_ComponentId",
                        column: x => x.ComponentId,
                        principalTable: "AppComponents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AppServiceOrderLines_AppCustomerAssetComponents_CustomerAssetComponentId",
                        column: x => x.CustomerAssetComponentId,
                        principalTable: "AppCustomerAssetComponents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AppServiceOrderLines_AppServiceOrders_ServiceOrderId",
                        column: x => x.ServiceOrderId,
                        principalTable: "AppServiceOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AppServiceOrderLines_AppServiceWorks_ServiceWorkId",
                        column: x => x.ServiceWorkId,
                        principalTable: "AppServiceWorks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AppServiceOrderLines_ComponentId",
                table: "AppServiceOrderLines",
                column: "ComponentId");

            migrationBuilder.CreateIndex(
                name: "IX_AppServiceOrderLines_CustomerAssetComponentId",
                table: "AppServiceOrderLines",
                column: "CustomerAssetComponentId");

            migrationBuilder.CreateIndex(
                name: "IX_AppServiceOrderLines_ServiceWorkId",
                table: "AppServiceOrderLines",
                column: "ServiceWorkId");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceOrderLines_Type_ComponentId",
                table: "AppServiceOrderLines",
                columns: new[] { "LineType", "ComponentId" });

            migrationBuilder.CreateIndex(
                name: "UX_ServiceOrderLines_OrderId_LineNo",
                table: "AppServiceOrderLines",
                columns: new[] { "ServiceOrderId", "LineNo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AppServiceOrders_CustomerId",
                table: "AppServiceOrders",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_AppServiceOrders_InventoryTransactionId",
                table: "AppServiceOrders",
                column: "InventoryTransactionId");

            migrationBuilder.CreateIndex(
                name: "IX_AppServiceOrders_TechnicianUserId",
                table: "AppServiceOrders",
                column: "TechnicianUserId");

            migrationBuilder.CreateIndex(
                name: "IX_AppServiceOrders_WarehouseId",
                table: "AppServiceOrders",
                column: "WarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceOrders_AssetId_OrderDate",
                table: "AppServiceOrders",
                columns: new[] { "CustomerAssetId", "OrderDate" });

            migrationBuilder.CreateIndex(
                name: "IX_ServiceOrders_Status_OrderDate",
                table: "AppServiceOrders",
                columns: new[] { "Status", "OrderDate" });

            migrationBuilder.CreateIndex(
                name: "UX_ServiceOrders_CompletionIdempotencyKey",
                table: "AppServiceOrders",
                column: "CompletionIdempotencyKey",
                unique: true,
                filter: "[CompletionIdempotencyKey] IS NOT NULL AND [IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "UX_ServiceOrders_OrderNo",
                table: "AppServiceOrders",
                column: "OrderNo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ServicePayments_CustomerId_PaymentDate",
                table: "AppServicePayments",
                columns: new[] { "CustomerId", "PaymentDate" });

            migrationBuilder.CreateIndex(
                name: "IX_ServicePayments_OrderId",
                table: "AppServicePayments",
                column: "ServiceOrderId");

            migrationBuilder.CreateIndex(
                name: "UX_ServicePayments_IdempotencyKey",
                table: "AppServicePayments",
                column: "IdempotencyKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ServiceWorks_Status_Name",
                table: "AppServiceWorks",
                columns: new[] { "Status", "Name" });

            migrationBuilder.CreateIndex(
                name: "UX_ServiceWorks_Code",
                table: "AppServiceWorks",
                column: "Code",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AppServiceOrderLines");

            migrationBuilder.DropTable(
                name: "AppServicePayments");

            migrationBuilder.DropTable(
                name: "AppServiceWorks");

            migrationBuilder.DropTable(
                name: "AppServiceOrders");
        }
    }
}
