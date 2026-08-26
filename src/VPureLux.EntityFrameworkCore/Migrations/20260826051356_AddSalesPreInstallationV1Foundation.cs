using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VPureLux.Migrations
{
    /// <inheritdoc />
    public partial class AddSalesPreInstallationV1Foundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UX_SalesOrderLines_OrderId_LineNo",
                table: "AppSalesOrderLines");

            migrationBuilder.AddColumn<string>(
                name: "VoidReason",
                table: "AppSalesOrderPayments",
                type: "nvarchar(1000)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "VoidedAt",
                table: "AppSalesOrderPayments",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "VoidedBy",
                table: "AppSalesOrderPayments",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "EffectiveRevisionId",
                table: "AppSalesOrderLines",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsEffective",
                table: "AppSalesOrderLines",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.CreateTable(
                name: "AppSalesOrderCancellations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SalesOrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReasonGroup = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    ApprovedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    EffectiveAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    StockStatus = table.Column<byte>(type: "tinyint", nullable: false),
                    PaymentStatus = table.Column<byte>(type: "tinyint", nullable: false),
                    RefundDue = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    RefundedAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    StockReversalTransactionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    StockExceptionReason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    StockCompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PaymentCompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ClosedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
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
                    table.PrimaryKey("PK_AppSalesOrderCancellations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AppSalesOrderCancellations_AppInventoryTransactions_StockReversalTransactionId",
                        column: x => x.StockReversalTransactionId,
                        principalTable: "AppInventoryTransactions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AppSalesOrderCancellations_AppSalesOrders_SalesOrderId",
                        column: x => x.SalesOrderId,
                        principalTable: "AppSalesOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AppSalesOrderRevisions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SalesOrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RevisionNo = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<byte>(type: "tinyint", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    CustomerIdSnapshot = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BeforeTotal = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    AppliedTotal = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    RefundDue = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    ApplyIdempotencyKey = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    AppliedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AppliedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CancelledBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CancelledAt = table.Column<DateTime>(type: "datetime2", nullable: true),
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
                    table.PrimaryKey("PK_AppSalesOrderRevisions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AppSalesOrderRevisions_AppSalesOrders_SalesOrderId",
                        column: x => x.SalesOrderId,
                        principalTable: "AppSalesOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AppSalesOrderRefunds",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SalesOrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SalesOrderRevisionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SalesOrderCancellationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    RefundedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PaymentMethod = table.Column<byte>(type: "tinyint", nullable: false),
                    ReferenceNo = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    IdempotencyKey = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
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
                    table.PrimaryKey("PK_AppSalesOrderRefunds", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AppSalesOrderRefunds_AppSalesOrderCancellations_SalesOrderCancellationId",
                        column: x => x.SalesOrderCancellationId,
                        principalTable: "AppSalesOrderCancellations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AppSalesOrderRefunds_AppSalesOrderRevisions_SalesOrderRevisionId",
                        column: x => x.SalesOrderRevisionId,
                        principalTable: "AppSalesOrderRevisions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AppSalesOrderRefunds_AppSalesOrders_SalesOrderId",
                        column: x => x.SalesOrderId,
                        principalTable: "AppSalesOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AppSalesOrderRevisionLines",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SourceSalesOrderLineId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    EffectiveSalesOrderLineId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LineNo = table.Column<int>(type: "int", nullable: false),
                    ProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BomVersionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    SuggestedPriceVersionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SuggestedPriceSnapshot = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    ActualSellingPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    OverrideReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsRemoved = table.Column<bool>(type: "bit", nullable: false),
                    BeforeProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    BeforeBomVersionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    BeforeQuantity = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    BeforeActualSellingPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    BeforeCostAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    BeforeInventoryTransactionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ReturnConfirmedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReturnConfirmedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ReturnReason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    IssueInventoryTransactionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ReversalInventoryTransactionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AppliedCostAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    SalesOrderRevisionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppSalesOrderRevisionLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AppSalesOrderRevisionLines_AppBomVersions_BeforeBomVersionId",
                        column: x => x.BeforeBomVersionId,
                        principalTable: "AppBomVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AppSalesOrderRevisionLines_AppBomVersions_BomVersionId",
                        column: x => x.BomVersionId,
                        principalTable: "AppBomVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AppSalesOrderRevisionLines_AppInventoryTransactions_BeforeInventoryTransactionId",
                        column: x => x.BeforeInventoryTransactionId,
                        principalTable: "AppInventoryTransactions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AppSalesOrderRevisionLines_AppInventoryTransactions_IssueInventoryTransactionId",
                        column: x => x.IssueInventoryTransactionId,
                        principalTable: "AppInventoryTransactions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AppSalesOrderRevisionLines_AppInventoryTransactions_ReversalInventoryTransactionId",
                        column: x => x.ReversalInventoryTransactionId,
                        principalTable: "AppInventoryTransactions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AppSalesOrderRevisionLines_AppSalesOrderRevisions_SalesOrderRevisionId",
                        column: x => x.SalesOrderRevisionId,
                        principalTable: "AppSalesOrderRevisions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AppSalesOrderRevisionAllocations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StockItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InventoryLotId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    UnitCost = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    SalesOrderRevisionLineId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppSalesOrderRevisionAllocations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AppSalesOrderRevisionAllocations_AppInventoryLots_InventoryLotId",
                        column: x => x.InventoryLotId,
                        principalTable: "AppInventoryLots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AppSalesOrderRevisionAllocations_AppSalesOrderRevisionLines_SalesOrderRevisionLineId",
                        column: x => x.SalesOrderRevisionLineId,
                        principalTable: "AppSalesOrderRevisionLines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AppSalesOrderRevisionAllocations_AppStockItems_StockItemId",
                        column: x => x.StockItemId,
                        principalTable: "AppStockItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AppSalesOrderLines_EffectiveRevisionId",
                table: "AppSalesOrderLines",
                column: "EffectiveRevisionId");

            migrationBuilder.CreateIndex(
                name: "UX_SalesOrderLines_OrderId_LineNo",
                table: "AppSalesOrderLines",
                columns: new[] { "SalesOrderId", "LineNo" },
                unique: true,
                filter: "[IsEffective] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_AppSalesOrderCancellations_SalesOrderId",
                table: "AppSalesOrderCancellations",
                column: "SalesOrderId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AppSalesOrderCancellations_StockReversalTransactionId",
                table: "AppSalesOrderCancellations",
                column: "StockReversalTransactionId");

            migrationBuilder.CreateIndex(
                name: "IX_AppSalesOrderRefunds_SalesOrderCancellationId",
                table: "AppSalesOrderRefunds",
                column: "SalesOrderCancellationId");

            migrationBuilder.CreateIndex(
                name: "IX_AppSalesOrderRefunds_SalesOrderId",
                table: "AppSalesOrderRefunds",
                column: "SalesOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_AppSalesOrderRefunds_SalesOrderRevisionId",
                table: "AppSalesOrderRefunds",
                column: "SalesOrderRevisionId");

            migrationBuilder.CreateIndex(
                name: "UX_SalesOrderRefunds_IdempotencyKey",
                table: "AppSalesOrderRefunds",
                column: "IdempotencyKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AppSalesOrderRevisionAllocations_InventoryLotId",
                table: "AppSalesOrderRevisionAllocations",
                column: "InventoryLotId");

            migrationBuilder.CreateIndex(
                name: "IX_AppSalesOrderRevisionAllocations_SalesOrderRevisionLineId",
                table: "AppSalesOrderRevisionAllocations",
                column: "SalesOrderRevisionLineId");

            migrationBuilder.CreateIndex(
                name: "IX_AppSalesOrderRevisionAllocations_StockItemId",
                table: "AppSalesOrderRevisionAllocations",
                column: "StockItemId");

            migrationBuilder.CreateIndex(
                name: "IX_AppSalesOrderRevisionLines_BeforeBomVersionId",
                table: "AppSalesOrderRevisionLines",
                column: "BeforeBomVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_AppSalesOrderRevisionLines_BeforeInventoryTransactionId",
                table: "AppSalesOrderRevisionLines",
                column: "BeforeInventoryTransactionId");

            migrationBuilder.CreateIndex(
                name: "IX_AppSalesOrderRevisionLines_BomVersionId",
                table: "AppSalesOrderRevisionLines",
                column: "BomVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_AppSalesOrderRevisionLines_IssueInventoryTransactionId",
                table: "AppSalesOrderRevisionLines",
                column: "IssueInventoryTransactionId");

            migrationBuilder.CreateIndex(
                name: "IX_AppSalesOrderRevisionLines_ReversalInventoryTransactionId",
                table: "AppSalesOrderRevisionLines",
                column: "ReversalInventoryTransactionId");

            migrationBuilder.CreateIndex(
                name: "IX_AppSalesOrderRevisionLines_SalesOrderRevisionId_LineNo",
                table: "AppSalesOrderRevisionLines",
                columns: new[] { "SalesOrderRevisionId", "LineNo" });

            migrationBuilder.CreateIndex(
                name: "IX_AppSalesOrderRevisions_SalesOrderId_RevisionNo",
                table: "AppSalesOrderRevisions",
                columns: new[] { "SalesOrderId", "RevisionNo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_SalesOrderRevisions_ActiveOrder",
                table: "AppSalesOrderRevisions",
                column: "SalesOrderId",
                unique: true,
                filter: "[Status] = 1 AND [IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "UX_SalesOrderRevisions_ApplyKey",
                table: "AppSalesOrderRevisions",
                column: "ApplyIdempotencyKey",
                unique: true,
                filter: "[ApplyIdempotencyKey] IS NOT NULL AND [IsDeleted] = 0");

            migrationBuilder.AddForeignKey(
                name: "FK_AppSalesOrderLines_AppSalesOrderRevisions_EffectiveRevisionId",
                table: "AppSalesOrderLines",
                column: "EffectiveRevisionId",
                principalTable: "AppSalesOrderRevisions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            if (ActiveProvider == "Microsoft.EntityFrameworkCore.SqlServer")
            {
                migrationBuilder.Sql("""
DECLARE @ProcedureName sysname;
DECLARE @Definition nvarchar(max);
DECLARE report_cursor CURSOR LOCAL FAST_FORWARD FOR
    SELECT name
    FROM sys.procedures
    WHERE name IN (N'sp_VP_ReportSalesRevenue', N'sp_VP_ReportSalesProfit');

OPEN report_cursor;
FETCH NEXT FROM report_cursor INTO @ProcedureName;
WHILE @@FETCH_STATUS = 0
BEGIN
    SET @Definition = OBJECT_DEFINITION(OBJECT_ID(N'dbo.' + @ProcedureName));
    IF @Definition IS NOT NULL AND @Definition NOT LIKE N'%l.IsEffective = 1%'
    BEGIN
        SET @Definition = REPLACE(@Definition, N'CREATE OR ALTER PROCEDURE', N'ALTER PROCEDURE');
        SET @Definition = REPLACE(
            @Definition,
            N'INNER JOIN AppSalesOrderLines l ON l.SalesOrderId = o.Id',
            N'INNER JOIN AppSalesOrderLines l ON l.SalesOrderId = o.Id AND l.IsEffective = 1');
        EXEC sys.sp_executesql @Definition;
    END;
    FETCH NEXT FROM report_cursor INTO @ProcedureName;
END;
CLOSE report_cursor;
DEALLOCATE report_cursor;
""");
            }
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            if (ActiveProvider == "Microsoft.EntityFrameworkCore.SqlServer")
            {
                migrationBuilder.Sql("""
DECLARE @ProcedureName sysname;
DECLARE @Definition nvarchar(max);
DECLARE report_cursor CURSOR LOCAL FAST_FORWARD FOR
    SELECT name
    FROM sys.procedures
    WHERE name IN (N'sp_VP_ReportSalesRevenue', N'sp_VP_ReportSalesProfit');

OPEN report_cursor;
FETCH NEXT FROM report_cursor INTO @ProcedureName;
WHILE @@FETCH_STATUS = 0
BEGIN
    SET @Definition = OBJECT_DEFINITION(OBJECT_ID(N'dbo.' + @ProcedureName));
    IF @Definition IS NOT NULL
    BEGIN
        SET @Definition = REPLACE(@Definition, N'CREATE OR ALTER PROCEDURE', N'ALTER PROCEDURE');
        SET @Definition = REPLACE(
            @Definition,
            N'INNER JOIN AppSalesOrderLines l ON l.SalesOrderId = o.Id AND l.IsEffective = 1',
            N'INNER JOIN AppSalesOrderLines l ON l.SalesOrderId = o.Id');
        EXEC sys.sp_executesql @Definition;
    END;
    FETCH NEXT FROM report_cursor INTO @ProcedureName;
END;
CLOSE report_cursor;
DEALLOCATE report_cursor;
""");
            }

            migrationBuilder.DropForeignKey(
                name: "FK_AppSalesOrderLines_AppSalesOrderRevisions_EffectiveRevisionId",
                table: "AppSalesOrderLines");

            migrationBuilder.DropTable(
                name: "AppSalesOrderRefunds");

            migrationBuilder.DropTable(
                name: "AppSalesOrderRevisionAllocations");

            migrationBuilder.DropTable(
                name: "AppSalesOrderCancellations");

            migrationBuilder.DropTable(
                name: "AppSalesOrderRevisionLines");

            migrationBuilder.DropTable(
                name: "AppSalesOrderRevisions");

            migrationBuilder.DropIndex(
                name: "IX_AppSalesOrderLines_EffectiveRevisionId",
                table: "AppSalesOrderLines");

            migrationBuilder.DropIndex(
                name: "UX_SalesOrderLines_OrderId_LineNo",
                table: "AppSalesOrderLines");

            migrationBuilder.DropColumn(
                name: "VoidReason",
                table: "AppSalesOrderPayments");

            migrationBuilder.DropColumn(
                name: "VoidedAt",
                table: "AppSalesOrderPayments");

            migrationBuilder.DropColumn(
                name: "VoidedBy",
                table: "AppSalesOrderPayments");

            migrationBuilder.DropColumn(
                name: "EffectiveRevisionId",
                table: "AppSalesOrderLines");

            migrationBuilder.DropColumn(
                name: "IsEffective",
                table: "AppSalesOrderLines");

            migrationBuilder.CreateIndex(
                name: "UX_SalesOrderLines_OrderId_LineNo",
                table: "AppSalesOrderLines",
                columns: new[] { "SalesOrderId", "LineNo" },
                unique: true);
        }
    }
}
