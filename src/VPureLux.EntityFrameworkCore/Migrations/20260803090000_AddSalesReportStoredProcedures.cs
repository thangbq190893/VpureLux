using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;
using VPureLux.EntityFrameworkCore;

#nullable disable

namespace VPureLux.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(VPureLuxDbContext))]
    [Migration("20260803090000_AddSalesReportStoredProcedures")]
    public partial class AddSalesReportStoredProcedures : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            if (ActiveProvider != "Microsoft.EntityFrameworkCore.SqlServer")
            {
                return;
            }

            migrationBuilder.Sql(SalesRevenueProcedure);
            migrationBuilder.Sql(SalesProfitProcedure);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            if (ActiveProvider != "Microsoft.EntityFrameworkCore.SqlServer")
            {
                return;
            }

            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS dbo.sp_VP_ReportSalesRevenue;");
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS dbo.sp_VP_ReportSalesProfit;");
        }

        private const string SalesRevenueProcedure = """
CREATE OR ALTER PROCEDURE dbo.sp_VP_ReportSalesRevenue
    @FromDate datetime2,
    @ToDateExclusive datetime2,
    @GroupBy nvarchar(16) = N'Day',
    @ProductId uniqueidentifier = NULL,
    @CustomerId uniqueidentifier = NULL,
    @WarehouseId uniqueidentifier = NULL,
    @MaxDetailRows int = 500,
    @PaymentStatus tinyint = NULL
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @ConfirmedStatus tinyint = 2;
    DECLARE @ProductLineType tinyint = 1;
    DECLARE @PostedPaymentStatus tinyint = 1;
    DECLARE @ReceivableUnpaid tinyint = 1;
    DECLARE @ReceivablePartiallyPaid tinyint = 2;
    DECLARE @ReceivablePaid tinyint = 3;
    DECLARE @ReceivableOverpaid tinyint = 4;

    SELECT
        o.Id AS SalesOrderId,
        o.OrderNo,
        o.ConfirmedAt AS ConfirmationTime,
        CASE @GroupBy
            WHEN N'Week' THEN CONCAT(DATEPART(year, o.ConfirmedAt), N'-W', RIGHT(CONCAT(N'0', DATEPART(iso_week, o.ConfirmedAt)), 2))
            WHEN N'Month' THEN CONVERT(char(7), o.ConfirmedAt, 23)
            WHEN N'Quarter' THEN CONCAT(DATEPART(year, o.ConfirmedAt), N'-Q', DATEPART(quarter, o.ConfirmedAt))
            WHEN N'Year' THEN CONVERT(varchar(4), DATEPART(year, o.ConfirmedAt))
            ELSE CONVERT(char(10), o.ConfirmedAt, 23)
        END AS PeriodKey,
        o.CustomerId,
        o.CustomerCodeSnapshot AS CustomerCode,
        o.CustomerNameSnapshot AS CustomerName,
        o.WarehouseId,
        w.Code AS WarehouseCode,
        w.Name AS WarehouseName,
        l.CatalogItemId AS ProductId,
        l.ItemCodeSnapshot AS ProductCode,
        l.ItemNameSnapshot AS ProductName,
        l.Quantity,
        l.RevenueAmount AS Revenue
    INTO #LineBase
    FROM AppSalesOrders o
    INNER JOIN AppSalesOrderLines l ON l.SalesOrderId = o.Id
    INNER JOIN AppWarehouses w ON w.Id = o.WarehouseId
    WHERE o.IsDeleted = 0
      AND o.Status = @ConfirmedStatus
      AND o.ConfirmedAt >= @FromDate
      AND o.ConfirmedAt < @ToDateExclusive
      AND l.LineType = @ProductLineType
      AND (@ProductId IS NULL OR l.CatalogItemId = @ProductId)
      AND (@CustomerId IS NULL OR o.CustomerId = @CustomerId)
      AND (@WarehouseId IS NULL OR o.WarehouseId = @WarehouseId);

    SELECT
        SalesOrderId,
        MAX(OrderNo) AS OrderNo,
        MAX(ConfirmationTime) AS ConfirmationTime,
        MAX(PeriodKey) AS PeriodKey,
        MAX(CustomerId) AS CustomerId,
        MAX(CustomerCode) AS CustomerCode,
        MAX(CustomerName) AS CustomerName,
        MAX(WarehouseId) AS WarehouseId,
        MAX(WarehouseCode) AS WarehouseCode,
        MAX(WarehouseName) AS WarehouseName,
        SUM(Revenue) AS TotalAmount
    INTO #OrderBase
    FROM #LineBase
    GROUP BY SalesOrderId;

    SELECT p.SalesOrderId, SUM(p.Amount) AS PaidAmount
    INTO #PaymentTotals
    FROM AppSalesOrderPayments p
    INNER JOIN #OrderBase o ON o.SalesOrderId = p.SalesOrderId
    WHERE p.IsDeleted = 0
      AND p.Status = @PostedPaymentStatus
    GROUP BY p.SalesOrderId;

    SELECT
        o.*,
        ISNULL(p.PaidAmount, 0) AS PaidAmount,
        o.TotalAmount - ISNULL(p.PaidAmount, 0) AS RemainingAmount,
        CASE
            WHEN ISNULL(p.PaidAmount, 0) <= 0 THEN @ReceivableUnpaid
            WHEN ISNULL(p.PaidAmount, 0) < o.TotalAmount THEN @ReceivablePartiallyPaid
            WHEN ISNULL(p.PaidAmount, 0) = o.TotalAmount THEN @ReceivablePaid
            ELSE @ReceivableOverpaid
        END AS PaymentStatus
    INTO #Orders
    FROM #OrderBase o
    LEFT JOIN #PaymentTotals p ON p.SalesOrderId = o.SalesOrderId
    WHERE @PaymentStatus IS NULL OR
        CASE
            WHEN ISNULL(p.PaidAmount, 0) <= 0 THEN @ReceivableUnpaid
            WHEN ISNULL(p.PaidAmount, 0) < o.TotalAmount THEN @ReceivablePartiallyPaid
            WHEN ISNULL(p.PaidAmount, 0) = o.TotalAmount THEN @ReceivablePaid
            ELSE @ReceivableOverpaid
        END = @PaymentStatus;

    SELECT
        ISNULL(SUM(o.TotalAmount), 0) AS TotalRevenue,
        COUNT_BIG(*) AS ConfirmedOrderCount,
        ISNULL((SELECT SUM(l.Quantity) FROM #LineBase l INNER JOIN #Orders fo ON fo.SalesOrderId = l.SalesOrderId), 0) AS TotalQuantity,
        CASE WHEN COUNT_BIG(*) = 0 THEN 0 ELSE ISNULL(SUM(o.TotalAmount), 0) / COUNT_BIG(*) END AS AverageOrderValue,
        ISNULL(SUM(o.PaidAmount), 0) AS PaidAmount,
        ISNULL(SUM(o.RemainingAmount), 0) AS RemainingAmount,
        SUM(CASE WHEN o.PaymentStatus = @ReceivableUnpaid THEN 1 ELSE 0 END) AS UnpaidOrderCount,
        SUM(CASE WHEN o.PaymentStatus = @ReceivablePartiallyPaid THEN 1 ELSE 0 END) AS PartiallyPaidOrderCount,
        SUM(CASE WHEN o.PaymentStatus = @ReceivablePaid THEN 1 ELSE 0 END) AS PaidOrderCount,
        SUM(CASE WHEN o.PaymentStatus = @ReceivableOverpaid THEN 1 ELSE 0 END) AS OverpaidOrderCount
    FROM #Orders o;

    SELECT
        lp.PeriodKey,
        lp.PeriodKey AS PeriodLabel,
        lp.OrderCount,
        lp.Quantity,
        lp.Revenue,
        ISNULL(op.PaidAmount, 0) AS PaidAmount,
        ISNULL(op.RemainingAmount, 0) AS RemainingAmount
    FROM (
        SELECT l.PeriodKey, COUNT(DISTINCT l.SalesOrderId) AS OrderCount, SUM(l.Quantity) AS Quantity, SUM(l.Revenue) AS Revenue
        FROM #LineBase l
        INNER JOIN #Orders fo ON fo.SalesOrderId = l.SalesOrderId
        GROUP BY l.PeriodKey
    ) lp
    LEFT JOIN (
        SELECT PeriodKey, SUM(PaidAmount) AS PaidAmount, SUM(RemainingAmount) AS RemainingAmount
        FROM #Orders
        GROUP BY PeriodKey
    ) op ON op.PeriodKey = lp.PeriodKey
    ORDER BY lp.PeriodKey;

    SELECT
        l.ProductId,
        l.ProductCode,
        l.ProductName,
        SUM(l.Quantity) AS Quantity,
        COUNT(DISTINCT l.SalesOrderId) AS OrderCount,
        SUM(l.Revenue) AS Revenue,
        CASE WHEN (SELECT SUM(TotalAmount) FROM #Orders) = 0 THEN 0
             ELSE SUM(l.Revenue) * 100.0 / (SELECT SUM(TotalAmount) FROM #Orders)
        END AS RevenueSharePercent
    FROM #LineBase l
    INNER JOIN #Orders fo ON fo.SalesOrderId = l.SalesOrderId
    GROUP BY l.ProductId, l.ProductCode, l.ProductName
    ORDER BY Revenue DESC, ProductCode;

    SELECT
        lc.CustomerId,
        lc.CustomerCode,
        lc.CustomerName,
        lc.OrderCount,
        lc.Revenue,
        ISNULL(oc.PaidAmount, 0) AS PaidAmount,
        ISNULL(oc.RemainingAmount, 0) AS RemainingAmount
    FROM (
        SELECT l.CustomerId, l.CustomerCode, l.CustomerName, COUNT(DISTINCT l.SalesOrderId) AS OrderCount, SUM(l.Revenue) AS Revenue
        FROM #LineBase l
        INNER JOIN #Orders fo ON fo.SalesOrderId = l.SalesOrderId
        GROUP BY l.CustomerId, l.CustomerCode, l.CustomerName
    ) lc
    LEFT JOIN (
        SELECT CustomerId, SUM(PaidAmount) AS PaidAmount, SUM(RemainingAmount) AS RemainingAmount
        FROM #Orders
        GROUP BY CustomerId
    ) oc ON oc.CustomerId = lc.CustomerId
    ORDER BY lc.Revenue DESC, lc.CustomerCode;

    SELECT TOP (@MaxDetailRows)
        SalesOrderId, OrderNo, ConfirmationTime, CustomerId, CustomerCode, CustomerName,
        WarehouseId, WarehouseCode, WarehouseName, TotalAmount, PaidAmount, RemainingAmount, PaymentStatus
    FROM #Orders
    ORDER BY ConfirmationTime DESC, OrderNo DESC;
END
""";

        private const string SalesProfitProcedure = """
CREATE OR ALTER PROCEDURE dbo.sp_VP_ReportSalesProfit
    @FromDate datetime2,
    @ToDateExclusive datetime2,
    @GroupBy nvarchar(16) = N'Day',
    @ProductId uniqueidentifier = NULL,
    @CustomerId uniqueidentifier = NULL,
    @WarehouseId uniqueidentifier = NULL,
    @MaxDetailRows int = 500,
    @LossOnly bit = 0,
    @MissingCostOnly bit = 0
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @ConfirmedStatus tinyint = 2;
    DECLARE @ProductLineType tinyint = 1;
    DECLARE @PostedPaymentStatus tinyint = 1;

    SELECT
        o.Id AS SalesOrderId,
        o.OrderNo,
        o.ConfirmedAt AS ConfirmationTime,
        CASE @GroupBy
            WHEN N'Week' THEN CONCAT(DATEPART(year, o.ConfirmedAt), N'-W', RIGHT(CONCAT(N'0', DATEPART(iso_week, o.ConfirmedAt)), 2))
            WHEN N'Month' THEN CONVERT(char(7), o.ConfirmedAt, 23)
            WHEN N'Quarter' THEN CONCAT(DATEPART(year, o.ConfirmedAt), N'-Q', DATEPART(quarter, o.ConfirmedAt))
            WHEN N'Year' THEN CONVERT(varchar(4), DATEPART(year, o.ConfirmedAt))
            ELSE CONVERT(char(10), o.ConfirmedAt, 23)
        END AS PeriodKey,
        o.CustomerId,
        o.CustomerCodeSnapshot AS CustomerCode,
        o.CustomerNameSnapshot AS CustomerName,
        o.WarehouseId,
        l.CatalogItemId AS ProductId,
        l.ItemCodeSnapshot AS ProductCode,
        l.ItemNameSnapshot AS ProductName,
        l.Quantity,
        l.ActualSellingPrice AS UnitPrice,
        l.RevenueAmount AS Revenue,
        l.CostAmountSnapshot AS CostAmount,
        l.ProfitAmount AS ProfitAmount,
        CASE WHEN l.RevenueAmount = 0 THEN 0 ELSE l.ProfitAmount * 100.0 / l.RevenueAmount END AS ProfitMarginPercent,
        CAST(CASE WHEN l.InventoryTransactionId IS NULL THEN 1 ELSE 0 END AS bit) AS MissingCost
    INTO #LineBase
    FROM AppSalesOrders o
    INNER JOIN AppSalesOrderLines l ON l.SalesOrderId = o.Id
    WHERE o.IsDeleted = 0
      AND o.Status = @ConfirmedStatus
      AND o.ConfirmedAt >= @FromDate
      AND o.ConfirmedAt < @ToDateExclusive
      AND l.LineType = @ProductLineType
      AND (@ProductId IS NULL OR l.CatalogItemId = @ProductId)
      AND (@CustomerId IS NULL OR o.CustomerId = @CustomerId)
      AND (@WarehouseId IS NULL OR o.WarehouseId = @WarehouseId)
      AND (@LossOnly = 0 OR (l.InventoryTransactionId IS NOT NULL AND l.ProfitAmount < 0))
      AND (@MissingCostOnly = 0 OR l.InventoryTransactionId IS NULL);

    SELECT
        SalesOrderId,
        MAX(ConfirmationTime) AS ConfirmationTime,
        MAX(CustomerId) AS CustomerId,
        MAX(CustomerCode) AS CustomerCode,
        MAX(CustomerName) AS CustomerName,
        SUM(Revenue) AS Revenue,
        SUM(CASE WHEN MissingCost = 1 THEN 0 ELSE CostAmount END) AS CostAmount,
        SUM(CASE WHEN MissingCost = 1 THEN 0 ELSE ProfitAmount END) AS ProfitAmount,
        MAX(CASE WHEN MissingCost = 0 AND ProfitAmount < 0 THEN 1 ELSE 0 END) AS HasLoss
    INTO #Orders
    FROM #LineBase
    GROUP BY SalesOrderId;

    SELECT p.SalesOrderId, SUM(p.Amount) AS PaidAmount
    INTO #PaymentTotals
    FROM AppSalesOrderPayments p
    INNER JOIN #Orders o ON o.SalesOrderId = p.SalesOrderId
    WHERE p.IsDeleted = 0
      AND p.Status = @PostedPaymentStatus
    GROUP BY p.SalesOrderId;

    SELECT
        ISNULL(SUM(Revenue), 0) AS Revenue,
        ISNULL(SUM(CostAmount), 0) AS CostAmount,
        ISNULL(SUM(ProfitAmount), 0) AS ProfitAmount,
        CASE WHEN ISNULL(SUM(Revenue), 0) = 0 THEN 0 ELSE SUM(ProfitAmount) * 100.0 / SUM(Revenue) END AS ProfitMarginPercent,
        COUNT_BIG(*) AS ConfirmedOrderCount,
        SUM(CASE WHEN HasLoss = 1 THEN 1 ELSE 0 END) AS LossOrderCount,
        (SELECT COUNT_BIG(*) FROM #LineBase WHERE MissingCost = 1) AS MissingCostLineCount
    FROM #Orders;

    SELECT
        PeriodKey,
        PeriodKey AS PeriodLabel,
        SUM(Revenue) AS Revenue,
        SUM(CASE WHEN MissingCost = 1 THEN 0 ELSE CostAmount END) AS CostAmount,
        SUM(CASE WHEN MissingCost = 1 THEN 0 ELSE ProfitAmount END) AS ProfitAmount,
        CASE WHEN SUM(Revenue) = 0 THEN 0 ELSE SUM(CASE WHEN MissingCost = 1 THEN 0 ELSE ProfitAmount END) * 100.0 / SUM(Revenue) END AS ProfitMarginPercent,
        COUNT(DISTINCT SalesOrderId) AS OrderCount
    FROM #LineBase
    GROUP BY PeriodKey
    ORDER BY PeriodKey;

    SELECT
        ProductId,
        ProductCode,
        ProductName,
        SUM(Quantity) AS Quantity,
        SUM(Revenue) AS Revenue,
        SUM(CASE WHEN MissingCost = 1 THEN 0 ELSE CostAmount END) AS CostAmount,
        SUM(CASE WHEN MissingCost = 1 THEN 0 ELSE ProfitAmount END) AS ProfitAmount,
        CASE WHEN SUM(Revenue) = 0 THEN 0 ELSE SUM(CASE WHEN MissingCost = 1 THEN 0 ELSE ProfitAmount END) * 100.0 / SUM(Revenue) END AS ProfitMarginPercent
    FROM #LineBase
    GROUP BY ProductId, ProductCode, ProductName
    ORDER BY ProfitAmount DESC, ProductCode;

    SELECT
        o.CustomerId,
        o.CustomerCode,
        o.CustomerName,
        COUNT(*) AS OrderCount,
        SUM(o.Revenue) AS Revenue,
        SUM(o.CostAmount) AS CostAmount,
        SUM(o.ProfitAmount) AS ProfitAmount,
        CASE WHEN SUM(o.Revenue) = 0 THEN 0 ELSE SUM(o.ProfitAmount) * 100.0 / SUM(o.Revenue) END AS ProfitMarginPercent,
        SUM(o.Revenue - ISNULL(p.PaidAmount, 0)) AS RemainingAmount
    FROM #Orders o
    LEFT JOIN #PaymentTotals p ON p.SalesOrderId = o.SalesOrderId
    GROUP BY o.CustomerId, o.CustomerCode, o.CustomerName
    ORDER BY ProfitAmount DESC, CustomerCode;

    SELECT TOP (@MaxDetailRows)
        SalesOrderId, OrderNo, ConfirmationTime, CustomerId, CustomerCode, CustomerName,
        ProductId, ProductCode, ProductName, Quantity, UnitPrice, Revenue,
        CASE WHEN MissingCost = 1 THEN 0 ELSE CostAmount END AS CostAmount,
        CASE WHEN MissingCost = 1 THEN 0 ELSE ProfitAmount END AS ProfitAmount,
        CASE WHEN MissingCost = 1 THEN 0 ELSE ProfitMarginPercent END AS ProfitMarginPercent,
        MissingCost
    FROM #LineBase
    ORDER BY ConfirmationTime DESC, OrderNo DESC, ProductCode;
END
""";
    }
}
