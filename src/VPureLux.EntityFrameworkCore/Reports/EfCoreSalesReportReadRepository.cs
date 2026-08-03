using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using VPureLux.EntityFrameworkCore;
using VPureLux.Sales;
using Volo.Abp.EntityFrameworkCore;

namespace VPureLux.Reports;

public class EfCoreSalesReportReadRepository : ISalesReportReadRepository
{
    private readonly IDbContextProvider<VPureLuxDbContext> _dbContextProvider;

    public EfCoreSalesReportReadRepository(IDbContextProvider<VPureLuxDbContext> dbContextProvider)
    {
        _dbContextProvider = dbContextProvider;
    }

    public async Task<SalesRevenueReportDto> GetSalesRevenueAsync(
        SalesRevenueReportInput input,
        DateTime toDateExclusive,
        CancellationToken cancellationToken = default)
    {
        var dbContext = await _dbContextProvider.GetDbContextAsync();
        if (dbContext.Database.IsSqlServer())
        {
            return await ExecuteSalesRevenueStoredProcedureAsync(dbContext, input, toDateExclusive, cancellationToken);
        }

        return await ExecuteSalesRevenueSqliteFallbackAsync(dbContext, input, toDateExclusive, cancellationToken);
    }

    public async Task<SalesProfitReportDto> GetSalesProfitAsync(
        SalesProfitReportInput input,
        DateTime toDateExclusive,
        CancellationToken cancellationToken = default)
    {
        var dbContext = await _dbContextProvider.GetDbContextAsync();
        if (dbContext.Database.IsSqlServer())
        {
            return await ExecuteSalesProfitStoredProcedureAsync(dbContext, input, toDateExclusive, cancellationToken);
        }

        return await ExecuteSalesProfitSqliteFallbackAsync(dbContext, input, toDateExclusive, cancellationToken);
    }

    private async Task<SalesRevenueReportDto> ExecuteSalesRevenueStoredProcedureAsync(
        VPureLuxDbContext dbContext,
        SalesRevenueReportInput input,
        DateTime toDateExclusive,
        CancellationToken cancellationToken)
    {
        await dbContext.Database.OpenConnectionAsync(cancellationToken);
        await using var command = CreateCommand(dbContext, "dbo.sp_VP_ReportSalesRevenue", CommandType.StoredProcedure);
        AddCommonParameters(command, input.FromDate!.Value, toDateExclusive, input.GroupBy, input.ProductId, input.CustomerId, input.WarehouseId, input.MaxDetailRows);
        AddParameter(command, "@PaymentStatus", DbType.Byte, input.PaymentStatus.HasValue ? (byte)input.PaymentStatus.Value : DBNull.Value);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await ReadSalesRevenueReportAsync(reader, cancellationToken);
    }

    private async Task<SalesProfitReportDto> ExecuteSalesProfitStoredProcedureAsync(
        VPureLuxDbContext dbContext,
        SalesProfitReportInput input,
        DateTime toDateExclusive,
        CancellationToken cancellationToken)
    {
        await dbContext.Database.OpenConnectionAsync(cancellationToken);
        await using var command = CreateCommand(dbContext, "dbo.sp_VP_ReportSalesProfit", CommandType.StoredProcedure);
        AddCommonParameters(command, input.FromDate!.Value, toDateExclusive, input.GroupBy, input.ProductId, input.CustomerId, input.WarehouseId, input.MaxDetailRows);
        AddParameter(command, "@LossOnly", DbType.Boolean, input.LossOnly);
        AddParameter(command, "@MissingCostOnly", DbType.Boolean, input.MissingCostOnly);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await ReadSalesProfitReportAsync(reader, cancellationToken);
    }

    private async Task<SalesRevenueReportDto> ExecuteSalesRevenueSqliteFallbackAsync(
        VPureLuxDbContext dbContext,
        SalesRevenueReportInput input,
        DateTime toDateExclusive,
        CancellationToken cancellationToken)
    {
        await dbContext.Database.OpenConnectionAsync(cancellationToken);
        try
        {
            await ExecuteNonQueryAsync(dbContext, """
                DROP TABLE IF EXISTS temp._VPRevenueLineBase;
                DROP TABLE IF EXISTS temp._VPRevenueOrderBase;
                DROP TABLE IF EXISTS temp._VPRevenuePaymentTotals;
                DROP TABLE IF EXISTS temp._VPRevenueOrders;

                CREATE TEMP TABLE _VPRevenueLineBase AS
                SELECT
                    o.Id AS SalesOrderId,
                    o.OrderNo,
                    o.ConfirmedAt AS ConfirmationTime,
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

                CREATE TEMP TABLE _VPRevenueOrderBase AS
                SELECT
                    SalesOrderId,
                    OrderNo,
                    ConfirmationTime,
                    CustomerId,
                    CustomerCode,
                    CustomerName,
                    WarehouseId,
                    WarehouseCode,
                    WarehouseName,
                    SUM(Revenue) AS TotalAmount
                FROM _VPRevenueLineBase
                GROUP BY SalesOrderId, OrderNo, ConfirmationTime, CustomerId, CustomerCode, CustomerName, WarehouseId, WarehouseCode, WarehouseName;

                CREATE TEMP TABLE _VPRevenuePaymentTotals AS
                SELECT SalesOrderId, SUM(Amount) AS PaidAmount
                FROM AppSalesOrderPayments
                WHERE IsDeleted = 0
                  AND Status = @PostedPaymentStatus
                  AND SalesOrderId IN (SELECT SalesOrderId FROM _VPRevenueOrderBase)
                GROUP BY SalesOrderId;

                CREATE TEMP TABLE _VPRevenueOrders AS
                SELECT
                    o.*,
                    COALESCE(p.PaidAmount, 0) AS PaidAmount,
                    o.TotalAmount - COALESCE(p.PaidAmount, 0) AS RemainingAmount,
                    CASE
                        WHEN COALESCE(p.PaidAmount, 0) <= 0 THEN @ReceivableUnpaid
                        WHEN COALESCE(p.PaidAmount, 0) < o.TotalAmount THEN @ReceivablePartiallyPaid
                        WHEN COALESCE(p.PaidAmount, 0) = o.TotalAmount THEN @ReceivablePaid
                        ELSE @ReceivableOverpaid
                    END AS PaymentStatus
                FROM _VPRevenueOrderBase o
                LEFT JOIN _VPRevenuePaymentTotals p ON p.SalesOrderId = o.SalesOrderId
                WHERE @PaymentStatus IS NULL OR
                    CASE
                        WHEN COALESCE(p.PaidAmount, 0) <= 0 THEN @ReceivableUnpaid
                        WHEN COALESCE(p.PaidAmount, 0) < o.TotalAmount THEN @ReceivablePartiallyPaid
                        WHEN COALESCE(p.PaidAmount, 0) = o.TotalAmount THEN @ReceivablePaid
                        ELSE @ReceivableOverpaid
                    END = @PaymentStatus;
                """, command =>
            {
                AddRevenueFallbackParameters(command, input, toDateExclusive);
            }, cancellationToken);

            return new SalesRevenueReportDto
            {
                Summary = await ReadSingleAsync(dbContext, """
                    SELECT
                        COALESCE(SUM(o.TotalAmount), 0) AS TotalRevenue,
                        COUNT(*) AS ConfirmedOrderCount,
                        COALESCE((SELECT SUM(l.Quantity) FROM _VPRevenueLineBase l INNER JOIN _VPRevenueOrders fo ON fo.SalesOrderId = l.SalesOrderId), 0) AS TotalQuantity,
                        CASE WHEN COUNT(*) = 0 THEN 0 ELSE COALESCE(SUM(o.TotalAmount), 0) / COUNT(*) END AS AverageOrderValue,
                        COALESCE(SUM(o.PaidAmount), 0) AS PaidAmount,
                        COALESCE(SUM(o.RemainingAmount), 0) AS RemainingAmount,
                        SUM(CASE WHEN o.PaymentStatus = @ReceivableUnpaid THEN 1 ELSE 0 END) AS UnpaidOrderCount,
                        SUM(CASE WHEN o.PaymentStatus = @ReceivablePartiallyPaid THEN 1 ELSE 0 END) AS PartiallyPaidOrderCount,
                        SUM(CASE WHEN o.PaymentStatus = @ReceivablePaid THEN 1 ELSE 0 END) AS PaidOrderCount,
                        SUM(CASE WHEN o.PaymentStatus = @ReceivableOverpaid THEN 1 ELSE 0 END) AS OverpaidOrderCount
                    FROM _VPRevenueOrders o;
                    """, ReadSalesRevenueSummary, AddReceivableParameters, cancellationToken),
                ByPeriod = await ReadListAsync(dbContext, $"""
                    WITH LinePeriod AS (
                        SELECT {SqlitePeriodExpression("l.ConfirmationTime", input.GroupBy)} AS PeriodKey,
                               COUNT(DISTINCT l.SalesOrderId) AS OrderCount,
                               SUM(l.Quantity) AS Quantity,
                               SUM(l.Revenue) AS Revenue
                        FROM _VPRevenueLineBase l
                        INNER JOIN _VPRevenueOrders fo ON fo.SalesOrderId = l.SalesOrderId
                        GROUP BY {SqlitePeriodExpression("l.ConfirmationTime", input.GroupBy)}
                    ),
                    OrderPeriod AS (
                        SELECT {SqlitePeriodExpression("o.ConfirmationTime", input.GroupBy)} AS PeriodKey,
                               SUM(o.PaidAmount) AS PaidAmount,
                               SUM(o.RemainingAmount) AS RemainingAmount
                        FROM _VPRevenueOrders o
                        GROUP BY {SqlitePeriodExpression("o.ConfirmationTime", input.GroupBy)}
                    )
                    SELECT lp.PeriodKey, lp.PeriodKey AS PeriodLabel, lp.OrderCount, lp.Quantity, lp.Revenue,
                           COALESCE(op.PaidAmount, 0) AS PaidAmount, COALESCE(op.RemainingAmount, 0) AS RemainingAmount
                    FROM LinePeriod lp
                    LEFT JOIN OrderPeriod op ON op.PeriodKey = lp.PeriodKey
                    ORDER BY lp.PeriodKey;
                    """, ReadSalesRevenuePeriodRow, null, cancellationToken),
                ByProduct = await ReadListAsync(dbContext, """
                    SELECT
                        l.ProductId,
                        l.ProductCode,
                        l.ProductName,
                        SUM(l.Quantity) AS Quantity,
                        COUNT(DISTINCT l.SalesOrderId) AS OrderCount,
                        SUM(l.Revenue) AS Revenue,
                        CASE WHEN (SELECT SUM(TotalAmount) FROM _VPRevenueOrders) = 0 THEN 0
                             ELSE SUM(l.Revenue) * 100.0 / (SELECT SUM(TotalAmount) FROM _VPRevenueOrders)
                        END AS RevenueSharePercent
                    FROM _VPRevenueLineBase l
                    INNER JOIN _VPRevenueOrders fo ON fo.SalesOrderId = l.SalesOrderId
                    GROUP BY l.ProductId, l.ProductCode, l.ProductName
                    ORDER BY Revenue DESC, ProductCode;
                    """, ReadSalesRevenueProductRow, null, cancellationToken),
                ByCustomer = await ReadListAsync(dbContext, """
                    WITH LineCustomer AS (
                        SELECT l.CustomerId, l.CustomerCode, l.CustomerName,
                               COUNT(DISTINCT l.SalesOrderId) AS OrderCount,
                               SUM(l.Revenue) AS Revenue
                        FROM _VPRevenueLineBase l
                        INNER JOIN _VPRevenueOrders fo ON fo.SalesOrderId = l.SalesOrderId
                        GROUP BY l.CustomerId, l.CustomerCode, l.CustomerName
                    ),
                    OrderCustomer AS (
                        SELECT CustomerId, SUM(PaidAmount) AS PaidAmount, SUM(RemainingAmount) AS RemainingAmount
                        FROM _VPRevenueOrders
                        GROUP BY CustomerId
                    )
                    SELECT lc.CustomerId, lc.CustomerCode, lc.CustomerName, lc.OrderCount, lc.Revenue,
                           COALESCE(oc.PaidAmount, 0) AS PaidAmount,
                           COALESCE(oc.RemainingAmount, 0) AS RemainingAmount
                    FROM LineCustomer lc
                    LEFT JOIN OrderCustomer oc ON oc.CustomerId = lc.CustomerId
                    ORDER BY lc.Revenue DESC, lc.CustomerCode;
                    """, ReadSalesRevenueCustomerRow, null, cancellationToken),
                Orders = await ReadListAsync(dbContext, """
                    SELECT SalesOrderId, OrderNo, ConfirmationTime, CustomerId, CustomerCode, CustomerName,
                           WarehouseId, WarehouseCode, WarehouseName, TotalAmount, PaidAmount, RemainingAmount, PaymentStatus
                    FROM _VPRevenueOrders
                    ORDER BY ConfirmationTime DESC, OrderNo DESC
                    LIMIT @MaxDetailRows;
                    """, ReadSalesRevenueOrderRow, command => AddParameter(command, "@MaxDetailRows", DbType.Int32, input.MaxDetailRows), cancellationToken)
            };
        }
        finally
        {
            await ExecuteNonQueryAsync(dbContext, """
                DROP TABLE IF EXISTS temp._VPRevenueLineBase;
                DROP TABLE IF EXISTS temp._VPRevenueOrderBase;
                DROP TABLE IF EXISTS temp._VPRevenuePaymentTotals;
                DROP TABLE IF EXISTS temp._VPRevenueOrders;
                """, null, cancellationToken);
        }
    }

    private async Task<SalesProfitReportDto> ExecuteSalesProfitSqliteFallbackAsync(
        VPureLuxDbContext dbContext,
        SalesProfitReportInput input,
        DateTime toDateExclusive,
        CancellationToken cancellationToken)
    {
        await dbContext.Database.OpenConnectionAsync(cancellationToken);
        try
        {
            await ExecuteNonQueryAsync(dbContext, """
                DROP TABLE IF EXISTS temp._VPProfitLineBase;
                DROP TABLE IF EXISTS temp._VPProfitOrders;
                DROP TABLE IF EXISTS temp._VPProfitPaymentTotals;

                CREATE TEMP TABLE _VPProfitLineBase AS
                SELECT
                    o.Id AS SalesOrderId,
                    o.OrderNo,
                    o.ConfirmedAt AS ConfirmationTime,
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
                    0 AS MissingCost
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
                  AND (@LossOnly = 0 OR l.ProfitAmount < 0)
                  AND (@MissingCostOnly = 0 OR 1 = 0);

                CREATE TEMP TABLE _VPProfitOrders AS
                SELECT SalesOrderId, ConfirmationTime, CustomerId, CustomerCode, CustomerName,
                       SUM(Revenue) AS Revenue,
                       SUM(CostAmount) AS CostAmount,
                       SUM(ProfitAmount) AS ProfitAmount,
                       MAX(CASE WHEN ProfitAmount < 0 THEN 1 ELSE 0 END) AS HasLoss
                FROM _VPProfitLineBase
                GROUP BY SalesOrderId, ConfirmationTime, CustomerId, CustomerCode, CustomerName;

                CREATE TEMP TABLE _VPProfitPaymentTotals AS
                SELECT SalesOrderId, SUM(Amount) AS PaidAmount
                FROM AppSalesOrderPayments
                WHERE IsDeleted = 0
                  AND Status = @PostedPaymentStatus
                  AND SalesOrderId IN (SELECT SalesOrderId FROM _VPProfitOrders)
                GROUP BY SalesOrderId;
                """, command =>
            {
                AddProfitFallbackParameters(command, input, toDateExclusive);
            }, cancellationToken);

            return new SalesProfitReportDto
            {
                Summary = await ReadSingleAsync(dbContext, """
                    SELECT
                        COALESCE(SUM(Revenue), 0) AS Revenue,
                        COALESCE(SUM(CostAmount), 0) AS CostAmount,
                        COALESCE(SUM(ProfitAmount), 0) AS ProfitAmount,
                        CASE WHEN COALESCE(SUM(Revenue), 0) = 0 THEN 0 ELSE SUM(ProfitAmount) * 100.0 / SUM(Revenue) END AS ProfitMarginPercent,
                        COUNT(*) AS ConfirmedOrderCount,
                        SUM(CASE WHEN HasLoss = 1 THEN 1 ELSE 0 END) AS LossOrderCount,
                        0 AS MissingCostLineCount
                    FROM _VPProfitOrders;
                    """, ReadSalesProfitSummary, null, cancellationToken),
                ByPeriod = await ReadListAsync(dbContext, $"""
                    SELECT {SqlitePeriodExpression("ConfirmationTime", input.GroupBy)} AS PeriodKey,
                           {SqlitePeriodExpression("ConfirmationTime", input.GroupBy)} AS PeriodLabel,
                           SUM(Revenue) AS Revenue,
                           SUM(CostAmount) AS CostAmount,
                           SUM(ProfitAmount) AS ProfitAmount,
                           CASE WHEN SUM(Revenue) = 0 THEN 0 ELSE SUM(ProfitAmount) * 100.0 / SUM(Revenue) END AS ProfitMarginPercent,
                           COUNT(DISTINCT SalesOrderId) AS OrderCount
                    FROM _VPProfitLineBase
                    GROUP BY {SqlitePeriodExpression("ConfirmationTime", input.GroupBy)}
                    ORDER BY PeriodKey;
                    """, ReadSalesProfitPeriodRow, null, cancellationToken),
                ByProduct = await ReadListAsync(dbContext, """
                    SELECT ProductId, ProductCode, ProductName,
                           SUM(Quantity) AS Quantity,
                           SUM(Revenue) AS Revenue,
                           SUM(CostAmount) AS CostAmount,
                           SUM(ProfitAmount) AS ProfitAmount,
                           CASE WHEN SUM(Revenue) = 0 THEN 0 ELSE SUM(ProfitAmount) * 100.0 / SUM(Revenue) END AS ProfitMarginPercent
                    FROM _VPProfitLineBase
                    GROUP BY ProductId, ProductCode, ProductName
                    ORDER BY ProfitAmount DESC, ProductCode;
                    """, ReadSalesProfitProductRow, null, cancellationToken),
                ByCustomer = await ReadListAsync(dbContext, """
                    SELECT o.CustomerId, o.CustomerCode, o.CustomerName,
                           COUNT(*) AS OrderCount,
                           SUM(o.Revenue) AS Revenue,
                           SUM(o.CostAmount) AS CostAmount,
                           SUM(o.ProfitAmount) AS ProfitAmount,
                           CASE WHEN SUM(o.Revenue) = 0 THEN 0 ELSE SUM(o.ProfitAmount) * 100.0 / SUM(o.Revenue) END AS ProfitMarginPercent,
                           SUM(o.Revenue - COALESCE(p.PaidAmount, 0)) AS RemainingAmount
                    FROM _VPProfitOrders o
                    LEFT JOIN _VPProfitPaymentTotals p ON p.SalesOrderId = o.SalesOrderId
                    GROUP BY o.CustomerId, o.CustomerCode, o.CustomerName
                    ORDER BY ProfitAmount DESC, CustomerCode;
                    """, ReadSalesProfitCustomerRow, null, cancellationToken),
                Lines = await ReadListAsync(dbContext, """
                    SELECT SalesOrderId, OrderNo, ConfirmationTime, CustomerId, CustomerCode, CustomerName,
                           ProductId, ProductCode, ProductName, Quantity, UnitPrice, Revenue, CostAmount,
                           ProfitAmount, ProfitMarginPercent, MissingCost
                    FROM _VPProfitLineBase
                    ORDER BY ConfirmationTime DESC, OrderNo DESC, ProductCode
                    LIMIT @MaxDetailRows;
                    """, ReadSalesProfitLineRow, command => AddParameter(command, "@MaxDetailRows", DbType.Int32, input.MaxDetailRows), cancellationToken)
            };
        }
        finally
        {
            await ExecuteNonQueryAsync(dbContext, """
                DROP TABLE IF EXISTS temp._VPProfitLineBase;
                DROP TABLE IF EXISTS temp._VPProfitOrders;
                DROP TABLE IF EXISTS temp._VPProfitPaymentTotals;
                """, null, cancellationToken);
        }
    }

    private static async Task<SalesRevenueReportDto> ReadSalesRevenueReportAsync(DbDataReader reader, CancellationToken cancellationToken)
    {
        var result = new SalesRevenueReportDto();
        result.Summary = await ReadSummaryFromReaderAsync(reader, ReadSalesRevenueSummary, cancellationToken);
        await reader.NextResultAsync(cancellationToken);
        result.ByPeriod = await ReadListFromReaderAsync(reader, ReadSalesRevenuePeriodRow, cancellationToken);
        await reader.NextResultAsync(cancellationToken);
        result.ByProduct = await ReadListFromReaderAsync(reader, ReadSalesRevenueProductRow, cancellationToken);
        await reader.NextResultAsync(cancellationToken);
        result.ByCustomer = await ReadListFromReaderAsync(reader, ReadSalesRevenueCustomerRow, cancellationToken);
        await reader.NextResultAsync(cancellationToken);
        result.Orders = await ReadListFromReaderAsync(reader, ReadSalesRevenueOrderRow, cancellationToken);
        return result;
    }

    private static async Task<SalesProfitReportDto> ReadSalesProfitReportAsync(DbDataReader reader, CancellationToken cancellationToken)
    {
        var result = new SalesProfitReportDto();
        result.Summary = await ReadSummaryFromReaderAsync(reader, ReadSalesProfitSummary, cancellationToken);
        await reader.NextResultAsync(cancellationToken);
        result.ByPeriod = await ReadListFromReaderAsync(reader, ReadSalesProfitPeriodRow, cancellationToken);
        await reader.NextResultAsync(cancellationToken);
        result.ByProduct = await ReadListFromReaderAsync(reader, ReadSalesProfitProductRow, cancellationToken);
        await reader.NextResultAsync(cancellationToken);
        result.ByCustomer = await ReadListFromReaderAsync(reader, ReadSalesProfitCustomerRow, cancellationToken);
        await reader.NextResultAsync(cancellationToken);
        result.Lines = await ReadListFromReaderAsync(reader, ReadSalesProfitLineRow, cancellationToken);
        return result;
    }

    private static async Task<T> ReadSummaryFromReaderAsync<T>(
        DbDataReader reader,
        Func<DbDataReader, T> map,
        CancellationToken cancellationToken)
        where T : new() =>
        await reader.ReadAsync(cancellationToken) ? map(reader) : new T();

    private static async Task<List<T>> ReadListFromReaderAsync<T>(
        DbDataReader reader,
        Func<DbDataReader, T> map,
        CancellationToken cancellationToken)
    {
        var rows = new List<T>();
        while (await reader.ReadAsync(cancellationToken))
        {
            rows.Add(map(reader));
        }
        return rows;
    }

    private async Task<T> ReadSingleAsync<T>(
        VPureLuxDbContext dbContext,
        string sql,
        Func<DbDataReader, T> map,
        Action<DbCommand>? configure,
        CancellationToken cancellationToken)
        where T : new()
    {
        await using var command = CreateCommand(dbContext, sql, CommandType.Text);
        configure?.Invoke(command);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await ReadSummaryFromReaderAsync(reader, map, cancellationToken);
    }

    private async Task<List<T>> ReadListAsync<T>(
        VPureLuxDbContext dbContext,
        string sql,
        Func<DbDataReader, T> map,
        Action<DbCommand>? configure,
        CancellationToken cancellationToken)
    {
        await using var command = CreateCommand(dbContext, sql, CommandType.Text);
        configure?.Invoke(command);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await ReadListFromReaderAsync(reader, map, cancellationToken);
    }

    private async Task ExecuteNonQueryAsync(
        VPureLuxDbContext dbContext,
        string sql,
        Action<DbCommand>? configure,
        CancellationToken cancellationToken)
    {
        await using var command = CreateCommand(dbContext, sql, CommandType.Text);
        configure?.Invoke(command);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static DbCommand CreateCommand(VPureLuxDbContext dbContext, string commandText, CommandType commandType)
    {
        var command = dbContext.Database.GetDbConnection().CreateCommand();
        command.Transaction = dbContext.Database.CurrentTransaction?.GetDbTransaction();
        command.CommandText = commandText;
        command.CommandType = commandType;
        command.CommandTimeout = 120;
        return command;
    }

    private static void AddCommonParameters(
        DbCommand command,
        DateTime fromDate,
        DateTime toDateExclusive,
        ReportPeriodGroup groupBy,
        Guid? productId,
        Guid? customerId,
        Guid? warehouseId,
        int maxDetailRows)
    {
        AddParameter(command, "@FromDate", DbType.DateTime, fromDate);
        AddParameter(command, "@ToDateExclusive", DbType.DateTime, toDateExclusive);
        AddParameter(command, "@GroupBy", DbType.String, groupBy.ToString());
        AddParameter(command, "@ProductId", DbType.Guid, productId.HasValue ? productId.Value : DBNull.Value);
        AddParameter(command, "@CustomerId", DbType.Guid, customerId.HasValue ? customerId.Value : DBNull.Value);
        AddParameter(command, "@WarehouseId", DbType.Guid, warehouseId.HasValue ? warehouseId.Value : DBNull.Value);
        AddParameter(command, "@MaxDetailRows", DbType.Int32, maxDetailRows);
    }

    private static void AddRevenueFallbackParameters(DbCommand command, SalesRevenueReportInput input, DateTime toDateExclusive)
    {
        AddBaseFallbackParameters(command, input.FromDate!.Value, toDateExclusive, input.ProductId, input.CustomerId, input.WarehouseId);
        AddReceivableParameters(command);
        AddParameter(command, "@PaymentStatus", DbType.Byte, input.PaymentStatus.HasValue ? (byte)input.PaymentStatus.Value : DBNull.Value);
    }

    private static void AddProfitFallbackParameters(DbCommand command, SalesProfitReportInput input, DateTime toDateExclusive)
    {
        AddBaseFallbackParameters(command, input.FromDate!.Value, toDateExclusive, input.ProductId, input.CustomerId, input.WarehouseId);
        AddParameter(command, "@LossOnly", DbType.Int32, input.LossOnly ? 1 : 0);
        AddParameter(command, "@MissingCostOnly", DbType.Int32, input.MissingCostOnly ? 1 : 0);
    }

    private static void AddBaseFallbackParameters(
        DbCommand command,
        DateTime fromDate,
        DateTime toDateExclusive,
        Guid? productId,
        Guid? customerId,
        Guid? warehouseId)
    {
        AddParameter(command, "@FromDate", DbType.DateTime, fromDate);
        AddParameter(command, "@ToDateExclusive", DbType.DateTime, toDateExclusive);
        AddParameter(command, "@ProductId", DbType.Guid, productId.HasValue ? productId.Value : DBNull.Value);
        AddParameter(command, "@CustomerId", DbType.Guid, customerId.HasValue ? customerId.Value : DBNull.Value);
        AddParameter(command, "@WarehouseId", DbType.Guid, warehouseId.HasValue ? warehouseId.Value : DBNull.Value);
        AddParameter(command, "@ConfirmedStatus", DbType.Byte, (byte)SalesOrderStatus.Confirmed);
        AddParameter(command, "@ProductLineType", DbType.Byte, (byte)SalesOrderLineType.Product);
        AddParameter(command, "@PostedPaymentStatus", DbType.Byte, (byte)SalesOrderPaymentStatus.Posted);
    }

    private static void AddReceivableParameters(DbCommand command)
    {
        AddParameter(command, "@ReceivableUnpaid", DbType.Byte, (byte)SalesOrderReceivableStatus.Unpaid);
        AddParameter(command, "@ReceivablePartiallyPaid", DbType.Byte, (byte)SalesOrderReceivableStatus.PartiallyPaid);
        AddParameter(command, "@ReceivablePaid", DbType.Byte, (byte)SalesOrderReceivableStatus.Paid);
        AddParameter(command, "@ReceivableOverpaid", DbType.Byte, (byte)SalesOrderReceivableStatus.Overpaid);
    }

    private static void AddParameter(DbCommand command, string name, DbType type, object value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.DbType = type;
        parameter.Value = value;
        command.Parameters.Add(parameter);
    }

    private static string SqlitePeriodExpression(string column, ReportPeriodGroup groupBy) =>
        groupBy switch
        {
            ReportPeriodGroup.Week => $"strftime('%Y-W%W', {column})",
            ReportPeriodGroup.Month => $"strftime('%Y-%m', {column})",
            ReportPeriodGroup.Quarter => $"strftime('%Y', {column}) || '-Q' || (((CAST(strftime('%m', {column}) AS INTEGER) - 1) / 3) + 1)",
            ReportPeriodGroup.Year => $"strftime('%Y', {column})",
            _ => $"strftime('%Y-%m-%d', {column})"
        };

    private static SalesRevenueSummaryDto ReadSalesRevenueSummary(DbDataReader reader) => new()
    {
        TotalRevenue = Decimal(reader, "TotalRevenue"),
        ConfirmedOrderCount = Int(reader, "ConfirmedOrderCount"),
        TotalQuantity = Decimal(reader, "TotalQuantity"),
        AverageOrderValue = Decimal(reader, "AverageOrderValue"),
        PaidAmount = Decimal(reader, "PaidAmount"),
        RemainingAmount = Decimal(reader, "RemainingAmount"),
        UnpaidOrderCount = Int(reader, "UnpaidOrderCount"),
        PartiallyPaidOrderCount = Int(reader, "PartiallyPaidOrderCount"),
        PaidOrderCount = Int(reader, "PaidOrderCount"),
        OverpaidOrderCount = Int(reader, "OverpaidOrderCount")
    };

    private static SalesRevenuePeriodRowDto ReadSalesRevenuePeriodRow(DbDataReader reader) => new()
    {
        PeriodKey = String(reader, "PeriodKey"),
        PeriodLabel = String(reader, "PeriodLabel"),
        OrderCount = Int(reader, "OrderCount"),
        Quantity = Decimal(reader, "Quantity"),
        Revenue = Decimal(reader, "Revenue"),
        PaidAmount = Decimal(reader, "PaidAmount"),
        RemainingAmount = Decimal(reader, "RemainingAmount")
    };

    private static SalesRevenueProductRowDto ReadSalesRevenueProductRow(DbDataReader reader) => new()
    {
        ProductId = Guid(reader, "ProductId"),
        ProductCode = String(reader, "ProductCode"),
        ProductName = String(reader, "ProductName"),
        Quantity = Decimal(reader, "Quantity"),
        OrderCount = Int(reader, "OrderCount"),
        Revenue = Decimal(reader, "Revenue"),
        RevenueSharePercent = Decimal(reader, "RevenueSharePercent")
    };

    private static SalesRevenueCustomerRowDto ReadSalesRevenueCustomerRow(DbDataReader reader) => new()
    {
        CustomerId = Guid(reader, "CustomerId"),
        CustomerCode = String(reader, "CustomerCode"),
        CustomerName = String(reader, "CustomerName"),
        OrderCount = Int(reader, "OrderCount"),
        Revenue = Decimal(reader, "Revenue"),
        PaidAmount = Decimal(reader, "PaidAmount"),
        RemainingAmount = Decimal(reader, "RemainingAmount")
    };

    private static SalesRevenueOrderRowDto ReadSalesRevenueOrderRow(DbDataReader reader) => new()
    {
        SalesOrderId = Guid(reader, "SalesOrderId"),
        OrderNo = String(reader, "OrderNo"),
        ConfirmationTime = DateTime(reader, "ConfirmationTime"),
        CustomerId = Guid(reader, "CustomerId"),
        CustomerCode = String(reader, "CustomerCode"),
        CustomerName = String(reader, "CustomerName"),
        WarehouseId = Guid(reader, "WarehouseId"),
        WarehouseCode = String(reader, "WarehouseCode"),
        WarehouseName = String(reader, "WarehouseName"),
        TotalAmount = Decimal(reader, "TotalAmount"),
        PaidAmount = Decimal(reader, "PaidAmount"),
        RemainingAmount = Decimal(reader, "RemainingAmount"),
        PaymentStatus = (SalesOrderReceivableStatus)Byte(reader, "PaymentStatus")
    };

    private static SalesProfitSummaryDto ReadSalesProfitSummary(DbDataReader reader) => new()
    {
        Revenue = Decimal(reader, "Revenue"),
        CostAmount = Decimal(reader, "CostAmount"),
        ProfitAmount = Decimal(reader, "ProfitAmount"),
        ProfitMarginPercent = Decimal(reader, "ProfitMarginPercent"),
        ConfirmedOrderCount = Int(reader, "ConfirmedOrderCount"),
        LossOrderCount = Int(reader, "LossOrderCount"),
        MissingCostLineCount = Int(reader, "MissingCostLineCount")
    };

    private static SalesProfitPeriodRowDto ReadSalesProfitPeriodRow(DbDataReader reader) => new()
    {
        PeriodKey = String(reader, "PeriodKey"),
        PeriodLabel = String(reader, "PeriodLabel"),
        Revenue = Decimal(reader, "Revenue"),
        CostAmount = Decimal(reader, "CostAmount"),
        ProfitAmount = Decimal(reader, "ProfitAmount"),
        ProfitMarginPercent = Decimal(reader, "ProfitMarginPercent"),
        OrderCount = Int(reader, "OrderCount")
    };

    private static SalesProfitProductRowDto ReadSalesProfitProductRow(DbDataReader reader) => new()
    {
        ProductId = Guid(reader, "ProductId"),
        ProductCode = String(reader, "ProductCode"),
        ProductName = String(reader, "ProductName"),
        Quantity = Decimal(reader, "Quantity"),
        Revenue = Decimal(reader, "Revenue"),
        CostAmount = Decimal(reader, "CostAmount"),
        ProfitAmount = Decimal(reader, "ProfitAmount"),
        ProfitMarginPercent = Decimal(reader, "ProfitMarginPercent")
    };

    private static SalesProfitCustomerRowDto ReadSalesProfitCustomerRow(DbDataReader reader) => new()
    {
        CustomerId = Guid(reader, "CustomerId"),
        CustomerCode = String(reader, "CustomerCode"),
        CustomerName = String(reader, "CustomerName"),
        OrderCount = Int(reader, "OrderCount"),
        Revenue = Decimal(reader, "Revenue"),
        CostAmount = Decimal(reader, "CostAmount"),
        ProfitAmount = Decimal(reader, "ProfitAmount"),
        ProfitMarginPercent = Decimal(reader, "ProfitMarginPercent"),
        RemainingAmount = Decimal(reader, "RemainingAmount")
    };

    private static SalesProfitLineRowDto ReadSalesProfitLineRow(DbDataReader reader) => new()
    {
        SalesOrderId = Guid(reader, "SalesOrderId"),
        OrderNo = String(reader, "OrderNo"),
        ConfirmationTime = DateTime(reader, "ConfirmationTime"),
        CustomerId = Guid(reader, "CustomerId"),
        CustomerCode = String(reader, "CustomerCode"),
        CustomerName = String(reader, "CustomerName"),
        ProductId = Guid(reader, "ProductId"),
        ProductCode = String(reader, "ProductCode"),
        ProductName = String(reader, "ProductName"),
        Quantity = Decimal(reader, "Quantity"),
        UnitPrice = Decimal(reader, "UnitPrice"),
        Revenue = Decimal(reader, "Revenue"),
        CostAmount = Decimal(reader, "CostAmount"),
        ProfitAmount = Decimal(reader, "ProfitAmount"),
        ProfitMarginPercent = Decimal(reader, "ProfitMarginPercent"),
        MissingCost = Boolean(reader, "MissingCost")
    };

    private static int Ordinal(DbDataReader reader, string name) => reader.GetOrdinal(name);
    private static string String(DbDataReader reader, string name) => reader.IsDBNull(Ordinal(reader, name)) ? string.Empty : Convert.ToString(reader.GetValue(Ordinal(reader, name))) ?? string.Empty;
    private static int Int(DbDataReader reader, string name) => reader.IsDBNull(Ordinal(reader, name)) ? 0 : Convert.ToInt32(reader.GetValue(Ordinal(reader, name)));
    private static byte Byte(DbDataReader reader, string name) => reader.IsDBNull(Ordinal(reader, name)) ? (byte)0 : Convert.ToByte(reader.GetValue(Ordinal(reader, name)));
    private static decimal Decimal(DbDataReader reader, string name) => reader.IsDBNull(Ordinal(reader, name)) ? 0 : Convert.ToDecimal(reader.GetValue(Ordinal(reader, name)));
    private static bool Boolean(DbDataReader reader, string name) => !reader.IsDBNull(Ordinal(reader, name)) && Convert.ToInt32(reader.GetValue(Ordinal(reader, name))) != 0;
    private static Guid Guid(DbDataReader reader, string name)
    {
        if (reader.IsDBNull(Ordinal(reader, name)))
        {
            return System.Guid.Empty;
        }

        var value = reader.GetValue(Ordinal(reader, name));
        return value is System.Guid guid ? guid : System.Guid.Parse(Convert.ToString(value)!);
    }
    private static DateTime DateTime(DbDataReader reader, string name) => reader.IsDBNull(Ordinal(reader, name)) ? default : Convert.ToDateTime(reader.GetValue(Ordinal(reader, name)));
}
