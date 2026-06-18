using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using VPureLux.EntityFrameworkCore;
using Volo.Abp;
using Volo.Abp.Domain.Repositories.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore;
using Volo.Abp.Data;

namespace VPureLux.Sales;

public class EfCoreSalesOrderRepository :
    EfCoreRepository<VPureLuxDbContext, SalesOrder, Guid>,
    ISalesOrderRepository
{
    public EfCoreSalesOrderRepository(IDbContextProvider<VPureLuxDbContext> provider) : base(provider)
    {
    }

    public async Task<int> GetNextOrderSequenceAsync(
        string yearMonth,
        CancellationToken cancellationToken = default)
    {
        var dbContext = await GetDbContextAsync();
        var sequenceName = $"SalesOrder:{yearMonth}";
        if (dbContext.Database.IsSqlServer())
        {
            await dbContext.Database.OpenConnectionAsync(cancellationToken);
            await using var command = dbContext.Database.GetDbConnection().CreateCommand();
            command.Transaction = dbContext.Database.CurrentTransaction?.GetDbTransaction();
            command.CommandText = """
                SET NOCOUNT ON;
                MERGE AppNumberSequences WITH (HOLDLOCK) AS target
                USING (SELECT @name AS [Name]) AS source
                ON target.[Name] = source.[Name]
                WHEN MATCHED THEN
                    UPDATE SET [CurrentValue] = target.[CurrentValue] + 1
                WHEN NOT MATCHED THEN
                    INSERT ([Name], [CurrentValue]) VALUES (source.[Name], 1)
                OUTPUT inserted.[CurrentValue];
                """;
            var parameter = command.CreateParameter();
            parameter.ParameterName = "@name";
            parameter.DbType = DbType.String;
            parameter.Value = sequenceName;
            command.Parameters.Add(parameter);
            return Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken));
        }

        var sequence = await dbContext.NumberSequences.FindAsync([sequenceName], cancellationToken);
        var next = checked((sequence?.CurrentValue ?? 0) + 1);
        if (sequence == null)
        {
            dbContext.NumberSequences.Add(new NumberSequence(sequenceName, next));
        }
        else
        {
            dbContext.Entry(sequence).Property(x => x.CurrentValue).CurrentValue = next;
        }
        await dbContext.SaveChangesAsync(cancellationToken);
        return next;
    }

    public async Task<SalesOrder?> FindByConfirmationIdempotencyKeyAsync(
        string idempotencyKey,
        CancellationToken cancellationToken = default) =>
        await IncludeDetails(await GetDbSetAsync())
            .FirstOrDefaultAsync(
                x => x.ConfirmationIdempotencyKey == idempotencyKey,
                GetCancellationToken(cancellationToken));

    public async Task<List<SalesOrder>> GetListAsync(
        Guid? customerId = null,
        SalesOrderStatus? status = null,
        string? sorting = null,
        int maxResultCount = int.MaxValue,
        int skipCount = 0,
        CancellationToken cancellationToken = default)
    {
        var query = IncludeDetails(await GetDbSetAsync())
            .Where(x => !customerId.HasValue || x.CustomerId == customerId)
            .Where(x => !status.HasValue || x.Status == status);
        query = sorting?.StartsWith(nameof(SalesOrder.OrderDate), StringComparison.OrdinalIgnoreCase) == true &&
                sorting.Contains("asc", StringComparison.OrdinalIgnoreCase)
            ? query.OrderBy(x => x.OrderDate).ThenBy(x => x.OrderNo)
            : query.OrderByDescending(x => x.OrderDate).ThenByDescending(x => x.OrderNo);
        return await query.Skip(skipCount).Take(maxResultCount)
            .ToListAsync(GetCancellationToken(cancellationToken));
    }

    public async Task<long> GetCountAsync(
        Guid? customerId = null,
        SalesOrderStatus? status = null,
        CancellationToken cancellationToken = default) =>
        await (await GetDbSetAsync())
            .Where(x => !customerId.HasValue || x.CustomerId == customerId)
            .Where(x => !status.HasValue || x.Status == status)
            .LongCountAsync(GetCancellationToken(cancellationToken));

    public async Task<List<CustomerPurchaseHistoryRecord>> GetCustomerPurchaseHistoryAsync(
        Guid customerId,
        CancellationToken cancellationToken = default)
    {
        var orders = await IncludeDetails(await GetDbSetAsync())
            .AsNoTracking()
            .Where(x => x.CustomerId == customerId && x.Status == SalesOrderStatus.Confirmed)
            .ToListAsync(GetCancellationToken(cancellationToken));

        return orders.SelectMany(order => order.Lines.Select(line => new { Order = order, Line = line }))
            .GroupBy(x => new { x.Order.CustomerId, ProductId = x.Line.CatalogItemId })
            .Select(group =>
            {
                var latest = group.OrderByDescending(x => x.Order.ConfirmedAt).ThenByDescending(x => x.Order.Id).First();
                var totalQuantity = group.Sum(x => x.Line.Quantity);
                return new CustomerPurchaseHistoryRecord(
                    group.Key.CustomerId,
                    group.Key.ProductId,
                    latest.Order.ConfirmedAt ?? latest.Order.OrderDate,
                    latest.Line.ActualSellingPrice,
                    totalQuantity == 0 ? 0 : decimal.Round(group.Sum(x => x.Line.RevenueAmount) / totalQuantity, SalesConsts.MoneyScale),
                    group.Sum(x => x.Line.RevenueAmount),
                    group.Sum(x => x.Line.ProfitAmount));
            })
            .OrderByDescending(x => x.LastPurchaseDate)
            .ToList();
    }

    public override async Task<SalesOrder?> FindAsync(
        Guid id,
        bool includeDetails = true,
        CancellationToken cancellationToken = default) =>
        includeDetails
            ? await IncludeDetails(await GetDbSetAsync()).FirstOrDefaultAsync(
                x => x.Id == id,
                GetCancellationToken(cancellationToken))
            : await base.FindAsync(id, false, cancellationToken);

    public override async Task<SalesOrder> InsertAsync(
        SalesOrder entity,
        bool autoSave = false,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await base.InsertAsync(entity, autoSave, cancellationToken);
        }
        catch (DbUpdateException exception) when (
            Contains(exception, SalesOrderConfiguration.OrderNoUniqueIndexName) ||
            Contains(exception, "AppSalesOrders.OrderNo"))
        {
            throw new BusinessException(VPureLuxDomainErrorCodes.DuplicateOrderNo)
                .WithData(nameof(entity.OrderNo), entity.OrderNo);
        }
    }

    public override async Task<SalesOrder> UpdateAsync(
        SalesOrder entity,
        bool autoSave = false,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var dbContext = await GetDbContextAsync();
            if (dbContext.Entry(entity).State == EntityState.Detached)
            {
                dbContext.SalesOrders.Update(entity);
            }
            dbContext.ChangeTracker.DetectChanges();
            var persistedLineIds = await dbContext.SalesOrders
                .Where(x => x.Id == entity.Id)
                .SelectMany(x => x.Lines)
                .Select(x => x.Id)
                .ToListAsync(GetCancellationToken(cancellationToken));
            foreach (var line in dbContext.ChangeTracker.Entries<SalesOrderLine>()
                         .Where(x => x.State == EntityState.Modified && !persistedLineIds.Contains(x.Entity.Id)))
            {
                line.State = EntityState.Added;
            }
            foreach (var snapshot in dbContext.ChangeTracker.Entries<SalesOrderBomSnapshotItem>()
                         .Where(x => x.State == EntityState.Modified))
            {
                snapshot.State = EntityState.Added;
            }
            if (autoSave)
            {
                await dbContext.SaveChangesAsync(GetCancellationToken(cancellationToken));
            }
            return entity;
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new BusinessException(VPureLuxDomainErrorCodes.SalesConcurrentModification)
                .WithData(nameof(entity.Id), entity.Id);
        }
        catch (AbpDbConcurrencyException exception)
        {
            throw new BusinessException(VPureLuxDomainErrorCodes.SalesConcurrentModification)
                .WithData(nameof(entity.Id), entity.Id)
                .WithData("ConcurrencyEntities", GetConcurrencyEntities(exception));
        }
        catch (DbUpdateException exception) when (
            Contains(exception, SalesOrderConfiguration.ConfirmationKeyUniqueIndexName) ||
            Contains(exception, "AppSalesOrders.ConfirmationIdempotencyKey"))
        {
            throw new BusinessException(VPureLuxDomainErrorCodes.DuplicateConfirmationKey)
                .WithData(nameof(entity.ConfirmationIdempotencyKey), entity.ConfirmationIdempotencyKey ?? string.Empty);
        }
    }

    private static IQueryable<SalesOrder> IncludeDetails(IQueryable<SalesOrder> query) =>
        query.Include(x => x.Lines).ThenInclude(x => x.BomSnapshotItems);

    private static bool Contains(Exception exception, string text)
    {
        for (var current = exception; current != null; current = current.InnerException)
        {
            if (current.Message.Contains(text, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }
        return false;
    }

    private static string GetConcurrencyEntities(Exception exception)
    {
        for (var current = exception; current != null; current = current.InnerException)
        {
            if (current is DbUpdateConcurrencyException concurrency)
            {
                return string.Join(",", concurrency.Entries.Select(x => x.Metadata.ClrType.Name).Distinct());
            }
        }
        return nameof(SalesOrder);
    }
}
