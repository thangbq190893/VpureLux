using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using VPureLux.Catalog;
using VPureLux.EntityFrameworkCore;
using VPureLux.Inventory;
using VPureLux.Pricing;
using VPureLux.Warranty;
using Volo.Abp;
using Volo.Abp.Domain.Repositories.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore;

namespace VPureLux.Service;

public class EfCoreServiceOrderRepository : EfCoreRepository<VPureLuxDbContext, ServiceOrder, Guid>, IServiceOrderRepository
{
    public EfCoreServiceOrderRepository(IDbContextProvider<VPureLuxDbContext> provider) : base(provider)
    {
    }

    public async Task<bool> OrderNoExistsAsync(string orderNo, CancellationToken cancellationToken = default) =>
        await (await GetDbSetAsync()).AnyAsync(x => x.OrderNo == orderNo, GetCancellationToken(cancellationToken));

    public async Task<int> GetMaxOrderNoSequenceAsync(string orderNoPrefix, CancellationToken cancellationToken = default)
    {
        var values = await (await GetDbSetAsync())
            .Where(x => x.OrderNo.StartsWith(orderNoPrefix))
            .Select(x => x.OrderNo)
            .ToListAsync(GetCancellationToken(cancellationToken));
        return values.Select(value => TryParseSequence(value, orderNoPrefix, out var sequence) ? sequence : 0)
            .DefaultIfEmpty(0).Max();
    }

    public async Task<ServiceOrder?> FindByCompletionKeyAsync(string idempotencyKey, CancellationToken cancellationToken = default) =>
        await IncludeDetails(await GetDbSetAsync()).FirstOrDefaultAsync(
            x => x.CompletionIdempotencyKey == idempotencyKey,
            GetCancellationToken(cancellationToken));

    public override async Task<ServiceOrder?> FindAsync(Guid id, bool includeDetails = true, CancellationToken cancellationToken = default) =>
        includeDetails
            ? await IncludeDetails(await GetDbSetAsync()).FirstOrDefaultAsync(x => x.Id == id, GetCancellationToken(cancellationToken))
            : await base.FindAsync(id, false, cancellationToken);

    public override async Task<ServiceOrder> InsertAsync(ServiceOrder entity, bool autoSave = false, CancellationToken cancellationToken = default)
    {
        try
        {
            return await base.InsertAsync(entity, autoSave, cancellationToken);
        }
        catch (DbUpdateException exception) when (Contains(exception, ServiceOrderConfiguration.OrderNoUniqueIndexName))
        {
            throw new BusinessException(VPureLuxDomainErrorCodes.DuplicateOrderNo)
                .WithData(nameof(entity.OrderNo), entity.OrderNo);
        }
    }

    public override async Task<ServiceOrder> UpdateAsync(ServiceOrder entity, bool autoSave = false, CancellationToken cancellationToken = default)
    {
        try
        {
            var dbContext = await GetDbContextAsync();
            if (dbContext.Entry(entity).State == EntityState.Detached)
            {
                dbContext.ServiceOrders.Update(entity);
            }
            dbContext.ChangeTracker.DetectChanges();
            var persistedLineIds = await dbContext.ServiceOrders
                .Where(x => x.Id == entity.Id)
                .SelectMany(x => x.Lines)
                .Select(x => x.Id)
                .ToListAsync(GetCancellationToken(cancellationToken));
            foreach (var line in dbContext.ChangeTracker.Entries<ServiceOrderLine>()
                         .Where(x => x.State == EntityState.Modified && !persistedLineIds.Contains(x.Entity.Id)))
            {
                line.State = EntityState.Added;
            }
            if (autoSave)
            {
                await dbContext.SaveChangesAsync(GetCancellationToken(cancellationToken));
            }
            return entity;
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new BusinessException(VPureLuxDomainErrorCodes.ServiceConcurrentModification);
        }
        catch (DbUpdateException exception) when (Contains(exception, ServiceOrderConfiguration.CompletionKeyUniqueIndexName))
        {
            throw new BusinessException(VPureLuxDomainErrorCodes.ServiceOrderCompletionConflict);
        }
    }

    private static IQueryable<ServiceOrder> IncludeDetails(IQueryable<ServiceOrder> query) => query.Include(x => x.Lines);

    private static bool TryParseSequence(string value, string prefix, out int sequence)
    {
        sequence = 0;
        return value.Length > prefix.Length &&
               int.TryParse(value[prefix.Length..], NumberStyles.None, CultureInfo.InvariantCulture, out sequence);
    }

    private static bool Contains(Exception exception, string value)
    {
        for (var current = exception; current != null; current = current.InnerException)
        {
            if (current.Message.Contains(value, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }
        return false;
    }
}

public class EfCoreServiceWorkRepository : EfCoreRepository<VPureLuxDbContext, ServiceWork, Guid>, IServiceWorkRepository
{
    public EfCoreServiceWorkRepository(IDbContextProvider<VPureLuxDbContext> provider) : base(provider)
    {
    }

    public async Task<bool> CodeExistsAsync(string code, Guid? excludedId = null, CancellationToken cancellationToken = default) =>
        await (await GetDbSetAsync()).AnyAsync(
            x => x.Code == code && (!excludedId.HasValue || x.Id != excludedId.Value),
            GetCancellationToken(cancellationToken));
}

public class EfCoreServicePaymentRepository : EfCoreRepository<VPureLuxDbContext, ServicePayment, Guid>, IServicePaymentRepository
{
    public EfCoreServicePaymentRepository(IDbContextProvider<VPureLuxDbContext> provider) : base(provider)
    {
    }

    public async Task<ServicePayment?> FindByIdempotencyKeyAsync(string idempotencyKey, CancellationToken cancellationToken = default) =>
        await (await GetDbSetAsync()).FirstOrDefaultAsync(x => x.IdempotencyKey == idempotencyKey, GetCancellationToken(cancellationToken));

    public async Task<List<ServicePayment>> GetByOrderIdAsync(Guid serviceOrderId, CancellationToken cancellationToken = default) =>
        await (await GetDbSetAsync()).Where(x => x.ServiceOrderId == serviceOrderId)
            .OrderByDescending(x => x.PaymentDate).ThenByDescending(x => x.CreationTime)
            .ToListAsync(GetCancellationToken(cancellationToken));
}

public class EfCoreServiceReadRepository : IServiceReadRepository
{
    private readonly IDbContextProvider<VPureLuxDbContext> _provider;

    public EfCoreServiceReadRepository(IDbContextProvider<VPureLuxDbContext> provider)
    {
        _provider = provider;
    }

    public async Task<long> GetOrderCountAsync(ServiceOrderFilter filter, CancellationToken cancellationToken = default)
    {
        var db = await _provider.GetDbContextAsync();
        return await ApplyFilter(db.ServiceOrders.AsNoTracking(), filter).LongCountAsync(cancellationToken);
    }

    public async Task<List<ServiceOrderListItem>> GetOrderListAsync(ServiceOrderFilter filter, CancellationToken cancellationToken = default)
    {
        var db = await _provider.GetDbContextAsync();
        var query = ApplyFilter(db.ServiceOrders.AsNoTracking(), filter);
        query = filter.Sorting?.Contains("OrderDate asc", StringComparison.OrdinalIgnoreCase) == true
            ? query.OrderBy(x => x.OrderDate).ThenBy(x => x.OrderNo)
            : query.OrderByDescending(x => x.OrderDate).ThenByDescending(x => x.OrderNo);

        return await query.Skip(filter.SkipCount).Take(filter.MaxResultCount)
            .Select(order => new ServiceOrderListItem
            {
                Id = order.Id,
                OrderNo = order.OrderNo,
                OrderDate = order.OrderDate,
                ScheduledAt = order.ScheduledAt,
                CompletedAt = order.CompletedAt,
                Status = order.Status,
                CustomerId = order.CustomerId,
                CustomerCode = order.CustomerCodeSnapshot,
                CustomerName = order.CustomerNameSnapshot,
                CustomerAssetId = order.CustomerAssetId,
                AssetNo = order.AssetNoSnapshot,
                AssetName = order.AssetNameSnapshot,
                TotalRevenueAmount = order.TotalRevenueAmount,
                PaidAmount = db.ServicePayments
                    .Where(payment => payment.ServiceOrderId == order.Id && payment.Status == ServicePaymentStatus.Posted)
                    .Sum(payment => (decimal?)payment.Amount) ?? 0,
                LineCount = order.Lines.Count
            }).ToListAsync(cancellationToken);
    }

    public async Task<List<ServiceAssetOption>> GetAssetOptionsAsync(
        Guid? customerId,
        string? searchText,
        int maxResultCount,
        CancellationToken cancellationToken = default)
    {
        var db = await _provider.GetDbContextAsync();
        var search = searchText?.Trim();
        return await db.CustomerAssets.AsNoTracking()
            .Where(x => x.Status == CustomerAssetStatus.Active || x.Status == CustomerAssetStatus.PendingReview)
            .Where(x => !customerId.HasValue || x.CustomerId == customerId.Value)
            .Where(x => string.IsNullOrEmpty(search) || x.AssetNo.Contains(search) ||
                        x.CustomerCodeSnapshot.Contains(search) || x.CustomerNameSnapshot.Contains(search) ||
                        (x.SerialNo != null && x.SerialNo.Contains(search)) || (x.Model != null && x.Model.Contains(search)))
            .OrderBy(x => x.CustomerNameSnapshot).ThenBy(x => x.AssetNo)
            .Take(maxResultCount)
            .Select(x => new ServiceAssetOption
            {
                Id = x.Id,
                CustomerId = x.CustomerId,
                CustomerCode = x.CustomerCodeSnapshot,
                CustomerName = x.CustomerNameSnapshot,
                AssetNo = x.AssetNo,
                AssetName = x.ProductNameSnapshot ?? ((x.Brand ?? "") + " " + (x.Model ?? "")).Trim(),
                ServiceAddress = x.InstallationAddress
            }).ToListAsync(cancellationToken);
    }

    public async Task<List<ServiceMaterialOption>> GetMaterialOptionsAsync(string? searchText, int maxResultCount, CancellationToken cancellationToken = default)
    {
        var db = await _provider.GetDbContextAsync();
        var search = searchText?.Trim();
        return await (from component in db.Components.AsNoTracking()
                      join stock in db.StockItems.AsNoTracking()
                          on component.Id equals stock.CatalogItemId
                      where component.Status == CatalogItemStatus.Active &&
                            stock.ItemType == StockItemType.Component && stock.Status == InventoryEntityStatus.Active && stock.IsInventoryEnabled &&
                            (string.IsNullOrEmpty(search) || component.Code.Contains(search) || component.Name.Contains(search))
                      orderby component.Code
                      select new ServiceMaterialOption
                      {
                          ComponentId = component.Id,
                          StockItemId = stock.Id,
                          Code = component.Code,
                          Name = component.Name,
                          Unit = component.Unit,
                          SuggestedPrice = db.ComponentSuggestedSellingPriceVersions
                              .Where(price => price.ComponentId == component.Id && price.Status == PriceVersionStatus.Active)
                              .OrderByDescending(price => price.EffectivePeriod.EffectiveFrom)
                              .Select(price => (decimal?)price.Price.Amount)
                              .FirstOrDefault()
                      }).Take(maxResultCount).ToListAsync(cancellationToken);
    }

    public async Task<List<ServiceWorkOption>> GetWorkOptionsAsync(string? searchText, int maxResultCount, CancellationToken cancellationToken = default)
    {
        var db = await _provider.GetDbContextAsync();
        var search = searchText?.Trim();
        return await db.ServiceWorks.AsNoTracking()
            .Where(x => x.Status == ServiceWorkStatus.Active)
            .Where(x => string.IsNullOrEmpty(search) || x.Code.Contains(search) || x.Name.Contains(search))
            .OrderBy(x => x.Code).Take(maxResultCount)
            .Select(x => new ServiceWorkOption { Id = x.Id, Code = x.Code, Name = x.Name, DefaultPrice = x.DefaultPrice })
            .ToListAsync(cancellationToken);
    }

    private static IQueryable<ServiceOrder> ApplyFilter(IQueryable<ServiceOrder> query, ServiceOrderFilter filter)
    {
        var search = filter.SearchText?.Trim();
        return query
            .Where(x => string.IsNullOrEmpty(search) || x.OrderNo.Contains(search) ||
                        x.CustomerCodeSnapshot.Contains(search) || x.CustomerNameSnapshot.Contains(search) ||
                        x.AssetNoSnapshot.Contains(search) || x.AssetNameSnapshot.Contains(search))
            .Where(x => !filter.CustomerId.HasValue || x.CustomerId == filter.CustomerId.Value)
            .Where(x => !filter.CustomerAssetId.HasValue || x.CustomerAssetId == filter.CustomerAssetId.Value)
            .Where(x => !filter.Status.HasValue || x.Status == filter.Status.Value)
            .Where(x => !filter.FromDate.HasValue || x.OrderDate >= filter.FromDate.Value)
            .Where(x => !filter.ToDate.HasValue || x.OrderDate < filter.ToDate.Value.Date.AddDays(1));
    }
}
