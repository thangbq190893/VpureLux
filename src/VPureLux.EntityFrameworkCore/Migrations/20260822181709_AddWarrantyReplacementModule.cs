using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VPureLux.Migrations
{
    /// <inheritdoc />
    public partial class AddWarrantyReplacementModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AppComponentReplacementPolicies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ComponentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsEnabled = table.Column<bool>(type: "bit", nullable: false),
                    CycleMonths = table.Column<int>(type: "int", nullable: false),
                    WarningDaysBeforeDue = table.Column<int>(type: "int", nullable: false),
                    Note = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
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
                    table.PrimaryKey("PK_AppComponentReplacementPolicies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AppComponentReplacementPolicies_AppComponents_ComponentId",
                        column: x => x.ComponentId,
                        principalTable: "AppComponents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AppCustomerAssets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SalesOrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SalesOrderLineId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SalesOrderLineNoSnapshot = table.Column<int>(type: "int", nullable: false),
                    AssetNo = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    OrderNoSnapshot = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    CustomerCodeSnapshot = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    CustomerNameSnapshot = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    ProductCodeSnapshot = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ProductNameSnapshot = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    SoldDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    WarrantyStartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<byte>(type: "tinyint", nullable: false),
                    Note = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
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
                    table.PrimaryKey("PK_AppCustomerAssets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AppCustomerAssets_AppCustomers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "AppCustomers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AppCustomerAssets_AppProducts_ProductId",
                        column: x => x.ProductId,
                        principalTable: "AppProducts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AppCustomerAssets_AppSalesOrders_SalesOrderId",
                        column: x => x.SalesOrderId,
                        principalTable: "AppSalesOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AppAssetReplacementReminders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CustomerAssetId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ComponentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SalesOrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SalesOrderLineId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ComponentCodeSnapshot = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ComponentNameSnapshot = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    ComponentUnitSnapshot = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    QuantityPerProductSnapshot = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    DueDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CycleMonthsSnapshot = table.Column<int>(type: "int", nullable: false),
                    WarningDaysBeforeDueSnapshot = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<byte>(type: "tinyint", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompletedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    NextReminderId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Note = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
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
                    table.PrimaryKey("PK_AppAssetReplacementReminders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AppAssetReplacementReminders_AppComponents_ComponentId",
                        column: x => x.ComponentId,
                        principalTable: "AppComponents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AppAssetReplacementReminders_AppCustomerAssets_CustomerAssetId",
                        column: x => x.CustomerAssetId,
                        principalTable: "AppCustomerAssets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AppAssetReplacementReminders_AppSalesOrders_SalesOrderId",
                        column: x => x.SalesOrderId,
                        principalTable: "AppSalesOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AssetReplacementReminders_ComponentId",
                table: "AppAssetReplacementReminders",
                column: "ComponentId");

            migrationBuilder.CreateIndex(
                name: "IX_AssetReplacementReminders_CustomerAssetId",
                table: "AppAssetReplacementReminders",
                column: "CustomerAssetId");

            migrationBuilder.CreateIndex(
                name: "IX_AssetReplacementReminders_SalesOrderId",
                table: "AppAssetReplacementReminders",
                column: "SalesOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_AssetReplacementReminders_SalesOrderLineId",
                table: "AppAssetReplacementReminders",
                column: "SalesOrderLineId");

            migrationBuilder.CreateIndex(
                name: "IX_AssetReplacementReminders_Status_DueDate",
                table: "AppAssetReplacementReminders",
                columns: new[] { "Status", "DueDate" });

            migrationBuilder.CreateIndex(
                name: "UX_ComponentReplacementPolicies_ComponentId",
                table: "AppComponentReplacementPolicies",
                column: "ComponentId",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerAssets_CustomerId",
                table: "AppCustomerAssets",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerAssets_ProductId",
                table: "AppCustomerAssets",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerAssets_SalesOrderId",
                table: "AppCustomerAssets",
                column: "SalesOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerAssets_SalesOrderLineId",
                table: "AppCustomerAssets",
                column: "SalesOrderLineId");

            migrationBuilder.CreateIndex(
                name: "UX_CustomerAssets_AssetNo",
                table: "AppCustomerAssets",
                column: "AssetNo",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AppAssetReplacementReminders");

            migrationBuilder.DropTable(
                name: "AppComponentReplacementPolicies");

            migrationBuilder.DropTable(
                name: "AppCustomerAssets");
        }
    }
}
