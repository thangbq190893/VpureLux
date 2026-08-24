using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VPureLux.Migrations
{
    /// <inheritdoc />
    public partial class AddCustomerCareFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<DateTime>(
                name: "WarrantyStartDate",
                table: "AppCustomerAssets",
                type: "datetime2",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AlterColumn<DateTime>(
                name: "SoldDate",
                table: "AppCustomerAssets",
                type: "datetime2",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AlterColumn<int>(
                name: "SalesOrderLineNoSnapshot",
                table: "AppCustomerAssets",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<Guid>(
                name: "SalesOrderLineId",
                table: "AppCustomerAssets",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AlterColumn<Guid>(
                name: "SalesOrderId",
                table: "AppCustomerAssets",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AlterColumn<string>(
                name: "ProductNameSnapshot",
                table: "AppCustomerAssets",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(256)",
                oldMaxLength: 256);

            migrationBuilder.AlterColumn<Guid>(
                name: "ProductId",
                table: "AppCustomerAssets",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AlterColumn<string>(
                name: "ProductCodeSnapshot",
                table: "AppCustomerAssets",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(64)",
                oldMaxLength: 64);

            migrationBuilder.AlterColumn<string>(
                name: "OrderNoSnapshot",
                table: "AppCustomerAssets",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(64)",
                oldMaxLength: 64);

            migrationBuilder.AddColumn<string>(
                name: "Brand",
                table: "AppCustomerAssets",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExternalReference",
                table: "AppCustomerAssets",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InstallationAddress",
                table: "AppCustomerAssets",
                type: "nvarchar(512)",
                maxLength: 512,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InstallationIdempotencyKey",
                table: "AppCustomerAssets",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "InstalledAt",
                table: "AppCustomerAssets",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "InstalledByUserId",
                table: "AppCustomerAssets",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Model",
                table: "AppCustomerAssets",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "AppCustomerAssets",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<string>(
                name: "SerialNo",
                table: "AppCustomerAssets",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<byte>(
                name: "Source",
                table: "AppCustomerAssets",
                type: "tinyint",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SourceUnitIndex",
                table: "AppCustomerAssets",
                type: "int",
                nullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "SalesOrderLineId",
                table: "AppAssetReplacementReminders",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AlterColumn<Guid>(
                name: "SalesOrderId",
                table: "AppAssetReplacementReminders",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AddColumn<string>(
                name: "CloseReason",
                table: "AppAssetReplacementReminders",
                type: "nvarchar(512)",
                maxLength: 512,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CustomerAssetComponentId",
                table: "AppAssetReplacementReminders",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IdempotencyKey",
                table: "AppAssetReplacementReminders",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "AppAssetReplacementReminders",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<Guid>(
                name: "SourceReferenceId",
                table: "AppAssetReplacementReminders",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SourceReferenceType",
                table: "AppAssetReplacementReminders",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<byte>(
                name: "TriggerSource",
                table: "AppAssetReplacementReminders",
                type: "tinyint",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "WarningDate",
                table: "AppAssetReplacementReminders",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AppCustomerAssetComponents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CustomerAssetId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PositionCode = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    PositionName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    ComponentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ComponentCodeSnapshot = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    ComponentNameSnapshot = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ComponentUnitSnapshot = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    ReplacementBaselineDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Status = table.Column<byte>(type: "tinyint", nullable: false),
                    Note = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
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
                    table.PrimaryKey("PK_AppCustomerAssetComponents", x => x.Id);
                    table.CheckConstraint("CK_CustomerAssetComponents_Quantity", "[Quantity] > 0");
                    table.ForeignKey(
                        name: "FK_AppCustomerAssetComponents_AppComponents_ComponentId",
                        column: x => x.ComponentId,
                        principalTable: "AppComponents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AppCustomerAssetComponents_AppCustomerAssets_CustomerAssetId",
                        column: x => x.CustomerAssetId,
                        principalTable: "AppCustomerAssets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AppCustomerCareSyncFailures",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SalesOrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SalesOrderLineId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IdempotencyKey = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    ErrorCode = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    ErrorMessage = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    ErrorContext = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    AttemptCount = table.Column<int>(type: "int", nullable: false),
                    FirstOccurredAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastOccurredAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    NextRetryAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ResolvedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Status = table.Column<byte>(type: "tinyint", nullable: false),
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
                    table.PrimaryKey("PK_AppCustomerCareSyncFailures", x => x.Id);
                    table.CheckConstraint("CK_CustomerCareSyncFailures_AttemptCount", "[AttemptCount] > 0");
                    table.ForeignKey(
                        name: "FK_AppCustomerCareSyncFailures_AppSalesOrders_SalesOrderId",
                        column: x => x.SalesOrderId,
                        principalTable: "AppSalesOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AppProductMachineSettings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsMachine = table.Column<bool>(type: "bit", nullable: false),
                    Note = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
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
                    table.PrimaryKey("PK_AppProductMachineSettings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AppProductMachineSettings_AppProducts_ProductId",
                        column: x => x.ProductId,
                        principalTable: "AppProducts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AppAssetMaintenanceEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CustomerAssetId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CustomerAssetComponentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    EventType = table.Column<byte>(type: "tinyint", nullable: false),
                    SourceType = table.Column<byte>(type: "tinyint", nullable: false),
                    SourceId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ComponentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ComponentCodeSnapshot = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    ComponentNameSnapshot = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    OccurredAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IdempotencyKey = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Note = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    ExtraProperties = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppAssetMaintenanceEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AppAssetMaintenanceEvents_AppComponents_ComponentId",
                        column: x => x.ComponentId,
                        principalTable: "AppComponents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AppAssetMaintenanceEvents_AppCustomerAssetComponents_CustomerAssetComponentId",
                        column: x => x.CustomerAssetComponentId,
                        principalTable: "AppCustomerAssetComponents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AppAssetMaintenanceEvents_AppCustomerAssets_CustomerAssetId",
                        column: x => x.CustomerAssetId,
                        principalTable: "AppCustomerAssets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CustomerAssets_SerialNo_Review",
                table: "AppCustomerAssets",
                column: "SerialNo",
                filter: "[SerialNo] IS NOT NULL AND [IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "UX_CustomerAssets_InstallationIdempotencyKey",
                table: "AppCustomerAssets",
                column: "InstallationIdempotencyKey",
                unique: true,
                filter: "[InstallationIdempotencyKey] IS NOT NULL AND [IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "UX_CustomerAssets_SourceLine_UnitIndex",
                table: "AppCustomerAssets",
                columns: new[] { "SalesOrderLineId", "SourceUnitIndex" },
                unique: true,
                filter: "[SalesOrderLineId] IS NOT NULL AND [SourceUnitIndex] IS NOT NULL AND [IsDeleted] = 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_CustomerAssets_SourceReferences",
                table: "AppCustomerAssets",
                sql: "[Source] IS NULL OR [Source] = 2 OR ([ProductId] IS NOT NULL AND [SalesOrderId] IS NOT NULL AND [SalesOrderLineId] IS NOT NULL AND [SourceUnitIndex] > 0)");

            migrationBuilder.CreateIndex(
                name: "IX_AssetReplacementReminders_Status_WarningDate",
                table: "AppAssetReplacementReminders",
                columns: new[] { "Status", "WarningDate" });

            migrationBuilder.CreateIndex(
                name: "UX_AssetReplacementReminders_IdempotencyKey",
                table: "AppAssetReplacementReminders",
                column: "IdempotencyKey",
                unique: true,
                filter: "[IdempotencyKey] IS NOT NULL AND [IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "UX_AssetReplacementReminders_OpenPosition",
                table: "AppAssetReplacementReminders",
                column: "CustomerAssetComponentId",
                unique: true,
                filter: "[CustomerAssetComponentId] IS NOT NULL AND [Status] = 1 AND [IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_AppAssetMaintenanceEvents_ComponentId",
                table: "AppAssetMaintenanceEvents",
                column: "ComponentId");

            migrationBuilder.CreateIndex(
                name: "IX_AssetMaintenanceEvents_Asset_OccurredAt",
                table: "AppAssetMaintenanceEvents",
                columns: new[] { "CustomerAssetId", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_AssetMaintenanceEvents_AssetComponentId",
                table: "AppAssetMaintenanceEvents",
                column: "CustomerAssetComponentId");

            migrationBuilder.CreateIndex(
                name: "UX_AssetMaintenanceEvents_IdempotencyKey",
                table: "AppAssetMaintenanceEvents",
                column: "IdempotencyKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CustomerAssetComponents_ComponentId",
                table: "AppCustomerAssetComponents",
                column: "ComponentId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerAssetComponents_Status_BaselineDate",
                table: "AppCustomerAssetComponents",
                columns: new[] { "Status", "ReplacementBaselineDate" });

            migrationBuilder.CreateIndex(
                name: "UX_CustomerAssetComponents_ActivePosition",
                table: "AppCustomerAssetComponents",
                columns: new[] { "CustomerAssetId", "PositionCode" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_AppCustomerCareSyncFailures_SalesOrderId",
                table: "AppCustomerCareSyncFailures",
                column: "SalesOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerCareSyncFailures_SalesOrderLineId",
                table: "AppCustomerCareSyncFailures",
                column: "SalesOrderLineId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerCareSyncFailures_Status_NextRetryAt",
                table: "AppCustomerCareSyncFailures",
                columns: new[] { "Status", "NextRetryAt" });

            migrationBuilder.CreateIndex(
                name: "UX_CustomerCareSyncFailures_IdempotencyKey",
                table: "AppCustomerCareSyncFailures",
                column: "IdempotencyKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_ProductMachineSettings_ProductId",
                table: "AppProductMachineSettings",
                column: "ProductId",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.AddForeignKey(
                name: "FK_AppAssetReplacementReminders_AppCustomerAssetComponents_CustomerAssetComponentId",
                table: "AppAssetReplacementReminders",
                column: "CustomerAssetComponentId",
                principalTable: "AppCustomerAssetComponents",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AppAssetReplacementReminders_AppCustomerAssetComponents_CustomerAssetComponentId",
                table: "AppAssetReplacementReminders");

            migrationBuilder.DropTable(
                name: "AppAssetMaintenanceEvents");

            migrationBuilder.DropTable(
                name: "AppCustomerCareSyncFailures");

            migrationBuilder.DropTable(
                name: "AppProductMachineSettings");

            migrationBuilder.DropTable(
                name: "AppCustomerAssetComponents");

            migrationBuilder.DropIndex(
                name: "IX_CustomerAssets_SerialNo_Review",
                table: "AppCustomerAssets");

            migrationBuilder.DropIndex(
                name: "UX_CustomerAssets_InstallationIdempotencyKey",
                table: "AppCustomerAssets");

            migrationBuilder.DropIndex(
                name: "UX_CustomerAssets_SourceLine_UnitIndex",
                table: "AppCustomerAssets");

            migrationBuilder.DropCheckConstraint(
                name: "CK_CustomerAssets_SourceReferences",
                table: "AppCustomerAssets");

            migrationBuilder.DropIndex(
                name: "IX_AssetReplacementReminders_Status_WarningDate",
                table: "AppAssetReplacementReminders");

            migrationBuilder.DropIndex(
                name: "UX_AssetReplacementReminders_IdempotencyKey",
                table: "AppAssetReplacementReminders");

            migrationBuilder.DropIndex(
                name: "UX_AssetReplacementReminders_OpenPosition",
                table: "AppAssetReplacementReminders");

            migrationBuilder.DropColumn(
                name: "Brand",
                table: "AppCustomerAssets");

            migrationBuilder.DropColumn(
                name: "ExternalReference",
                table: "AppCustomerAssets");

            migrationBuilder.DropColumn(
                name: "InstallationAddress",
                table: "AppCustomerAssets");

            migrationBuilder.DropColumn(
                name: "InstallationIdempotencyKey",
                table: "AppCustomerAssets");

            migrationBuilder.DropColumn(
                name: "InstalledAt",
                table: "AppCustomerAssets");

            migrationBuilder.DropColumn(
                name: "InstalledByUserId",
                table: "AppCustomerAssets");

            migrationBuilder.DropColumn(
                name: "Model",
                table: "AppCustomerAssets");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "AppCustomerAssets");

            migrationBuilder.DropColumn(
                name: "SerialNo",
                table: "AppCustomerAssets");

            migrationBuilder.DropColumn(
                name: "Source",
                table: "AppCustomerAssets");

            migrationBuilder.DropColumn(
                name: "SourceUnitIndex",
                table: "AppCustomerAssets");

            migrationBuilder.DropColumn(
                name: "CloseReason",
                table: "AppAssetReplacementReminders");

            migrationBuilder.DropColumn(
                name: "CustomerAssetComponentId",
                table: "AppAssetReplacementReminders");

            migrationBuilder.DropColumn(
                name: "IdempotencyKey",
                table: "AppAssetReplacementReminders");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "AppAssetReplacementReminders");

            migrationBuilder.DropColumn(
                name: "SourceReferenceId",
                table: "AppAssetReplacementReminders");

            migrationBuilder.DropColumn(
                name: "SourceReferenceType",
                table: "AppAssetReplacementReminders");

            migrationBuilder.DropColumn(
                name: "TriggerSource",
                table: "AppAssetReplacementReminders");

            migrationBuilder.DropColumn(
                name: "WarningDate",
                table: "AppAssetReplacementReminders");

            migrationBuilder.AlterColumn<DateTime>(
                name: "WarrantyStartDate",
                table: "AppCustomerAssets",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "SoldDate",
                table: "AppCustomerAssets",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "SalesOrderLineNoSnapshot",
                table: "AppCustomerAssets",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "SalesOrderLineId",
                table: "AppCustomerAssets",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "SalesOrderId",
                table: "AppCustomerAssets",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ProductNameSnapshot",
                table: "AppCustomerAssets",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(256)",
                oldMaxLength: 256,
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "ProductId",
                table: "AppCustomerAssets",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ProductCodeSnapshot",
                table: "AppCustomerAssets",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(64)",
                oldMaxLength: 64,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "OrderNoSnapshot",
                table: "AppCustomerAssets",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(64)",
                oldMaxLength: 64,
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "SalesOrderLineId",
                table: "AppAssetReplacementReminders",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "SalesOrderId",
                table: "AppAssetReplacementReminders",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);
        }
    }
}
