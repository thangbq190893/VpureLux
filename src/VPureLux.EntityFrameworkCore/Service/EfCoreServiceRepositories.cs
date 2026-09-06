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
        await (await GetDbSetAsync()).AnyAsync(order => order.OrderNo == orderNo, GetCancellationToken(cancellationToken));

    public async Task<int> GetMaxOrderNoSequenceAsync(string orderNoPrefix, CancellationToken cancellationToken = default)
    {
        var maxValue = await (await GetDbSetAsync())
            .Where(order => order.OrderNo.StartsWith(orderNoPrefix))
            .OrderByDescending(order => order.OrderNo.Length)
            .ThenByDescending(order => order.OrderNo)
            .Select(order => order.OrderNo)
            .FirstOrDefaultAsync(GetCancellationToken(cancellationToken));

        return maxValue != null && maxValue.Length > orderNoPrefix.Length &&
               int.TryParse(maxValue[orderNoPrefix.Length..], NumberStyles.None, CultureInfo.InvariantCulture, out var sequence)
            ? sequence
            : 0;
    }

    public override async Task<ServiceOrder?> FindAsync(
        Guid id,
        bool includeDetails = true,
        CancellationToken cancellationToken = default)
    {
        if (!includeDetails)
        {
            return await base.FindAsync(id, false, cancellationToken);
        }

        return await (await GetDbSetAsync())
            .Include(order => order.Lines)
            .FirstOrDefaultAsync(order => order.Id == id, GetCancellationToken(cancellationToken));
    }

    public override async Task<ServiceOrder> GetAsync(
        Guid id,
        bool includeDetails = true,
        CancellationToken cancellationToken = default) =>
        await FindAsync(id, includeDetails, cancellationToken)
        ?? throw new BusinessException(ServiceErrorCodes.OrderNotFound).WithData("ServiceOrderId", id);

    public override async Task<ServiceOrder> InsertAsync(
        ServiceOrder entity,
        bool autoSave = false,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await base.InsertAsync(entity, autoSave, cancellationToken);
        }
        catch (DbUpdateException exception) when (Contains(exception, ServiceOrderConfiguration.OrderNoUniqueIndexName))
        {
            throw new BusinessException(ServiceErrorCodes.DuplicateOrderNo).WithData("OrderNo", entity.OrderNo);
        }
    }

    public override async Task<ServiceOrder> UpdateAsync(
        ServiceOrder entity,
        bool autoSave = false,
        CancellationToken cancellationToken = default)
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
                .Where(order => order.Id == entity.Id)
                .SelectMany(order => order.Lines)
                .Select(line => line.Id)
                .ToListAsync(GetCancellationToken(cancellationToken));

            foreach (var line in dbContext.ChangeTracker.Entries<ServiceOrderLine>()
                         .Where(entry => entry.State == EntityState.Modified && !persistedLineIds.Contains(entry.Entity.Id)))
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
            throw new BusinessException(ServiceErrorCodes.ConcurrentModification)
                .WithData("ServiceOrderId", entity.Id)
                .WithData("OrderNo", entity.OrderNo);
        }
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
        return await ApplyOrderFilter(db.ServiceOrders.AsNoTracking(), filter).LongCountAsync(cancellationToken);
    }

    public async Task<List<ServiceOrderListItem>> GetOrderListAsync(
        ServiceOrderFilter filter,
        CancellationToken cancellationToken = default)
    {
        var db = await _provider.GetDbContextAsync();
        var query = ApplySorting(ApplyOrderFilter(db.ServiceOrders.AsNoTracking(), filter), filter.Sorting);
        return await query
            .Skip(filter.SkipCount)
            .Take(filter.MaxResultCount)
            .Select(order => new ServiceOrderListItem(
                order.Id,
                order.OrderNo,
                order.OrderDate,
                order.ScheduledAt,
                order.Status,
                order.CustomerId,
                order.CustomerCodeSnapshot,
                order.CustomerNameSnapshot,
                order.CustomerAssetId,
                order.AssetNoSnapshot,
                order.AssetNameSnapshot,
                order.Lines.Sum(line => line.UnitPrice * line.PlannedQuantity),
                order.Lines.Count))
            .ToListAsync(cancellationToken);
    }

    public async Task<long> GetAssetCountAsync(
        Guid? customerId,
        string? searchText,
        CancellationToken cancellationToken = default)
    {
        var db = await _provider.GetDbContextAsync();
        return await ApplyAssetFilter(db.CustomerAssets.AsNoTracking(), customerId, searchText)
            .LongCountAsync(cancellationToken);
    }

    public async Task<List<ServiceAssetOption>> GetAssetOptionsAsync(
        Guid? customerId,
        string? searchText,
        int skipCount,
        int maxResultCount,
        CancellationToken cancellationToken = default)
    {
        var db = await _provider.GetDbContextAsync();
        return await ApplyAssetFilter(db.CustomerAssets.AsNoTracking(), customerId, searchText)
            .OrderBy(asset => asset.CustomerNameSnapshot)
            .ThenBy(asset => asset.AssetNo)
            .ThenBy(asset => asset.Id)
            .Skip(skipCount)
            .Take(maxResultCount)
            .Select(asset => new ServiceAssetOption(
                asset.Id,
                asset.CustomerId,
                asset.CustomerCodeSnapshot,
                asset.CustomerNameSnapshot,
                asset.AssetNo,
                asset.ProductNameSnapshot ?? ((asset.Brand ?? "") + " " + (asset.Model ?? "")).Trim(),
                asset.InstallationAddress))
            .ToListAsync(cancellationToken);
    }

    public async Task<long> GetMaterialCountAsync(string? searchText, CancellationToken cancellationToken = default)
    {
        var db = await _provider.GetDbContextAsync();
        return await ApplyMaterialFilter(db, searchText).LongCountAsync(cancellationToken);
    }

    public async Task<List<ServiceMaterialOption>> GetMaterialOptionsAsync(
        string? searchText,
        int skipCount,
        int maxResultCount,
        CancellationToken cancellationToken = default)
    {
        var db = await _provider.GetDbContextAsync();
        return await ApplyMaterialFilter(db, searchText)
            .OrderBy(item => item.Code)
            .ThenBy(item => item.Id)
            .Skip(skipCount)
            .Take(maxResultCount)
            .Select(item => new ServiceMaterialOption(
                item.Id,
                item.Code,
                item.Name,
                item.Unit,
                db.ComponentSuggestedSellingPriceVersions
                    .Where(price => price.ComponentId == item.Id && price.Status == PriceVersionStatus.Active)
                    .OrderByDescending(price => price.EffectivePeriod.EffectiveFrom)
                    .Select(price => (decimal?)price.Price.Amount)
                    .FirstOrDefault()))
            .ToListAsync(cancellationToken);
    }

    public async Task<long> GetWorkCountAsync(string? searchText, CancellationToken cancellationToken = default)
    {
        var db = await _provider.GetDbContextAsync();
        return await ApplyWorkFilter(db.ServiceWorks.AsNoTracking(), searchText).LongCountAsync(cancellationToken);
    }

    public async Task<List<ServiceWorkOption>> GetWorkOptionsAsync(
        string? searchText,
        int skipCount,
        int maxResultCount,
        CancellationToken cancellationToken = default)
    {
        var db = await _provider.GetDbContextAsync();
        return await ApplyWorkFilter(db.ServiceWorks.AsNoTracking(), searchText)
            .OrderBy(work => work.Code)
            .ThenBy(work => work.Id)
            .Skip(skipCount)
            .Take(maxResultCount)
            .Select(work => new ServiceWorkOption(
                work.Id, work.Code, work.Name, work.Unit!, work.DefaultPrice, work.StandardCost))
            .ToListAsync(cancellationToken);
    }

    public async Task<long> GetTechnicianCountAsync(string? searchText, CancellationToken cancellationToken = default)
    {
        var db = await _provider.GetDbContextAsync();
        return await ApplyTechnicianFilter(db.Users.AsNoTracking(), searchText).LongCountAsync(cancellationToken);
    }

    public async Task<List<ServiceTechnicianOption>> GetTechnicianOptionsAsync(
        string? searchText,
        int skipCount,
        int maxResultCount,
        CancellationToken cancellationToken = default)
    {
        var db = await _provider.GetDbContextAsync();
        return await ApplyTechnicianFilter(db.Users.AsNoTracking(), searchText)
            .OrderBy(user => user.Name ?? user.UserName)
            .ThenBy(user => user.Id)
            .Skip(skipCount)
            .Take(maxResultCount)
            .Select(user => new ServiceTechnicianOption(user.Id, user.Name ?? user.UserName))
            .ToListAsync(cancellationToken);
    }

    private static IQueryable<ServiceOrder> ApplyOrderFilter(IQueryable<ServiceOrder> query, ServiceOrderFilter filter)
    {
        var search = filter.SearchText?.Trim();
        return query
            .Where(order => string.IsNullOrEmpty(search) ||
                            order.OrderNo.Contains(search) ||
                            order.CustomerCodeSnapshot.Contains(search) ||
                            order.CustomerNameSnapshot.Contains(search) ||
                            order.AssetNoSnapshot.Contains(search) ||
                            order.AssetNameSnapshot.Contains(search))
            .Where(order => !filter.CustomerId.HasValue || order.CustomerId == filter.CustomerId.Value)
            .Where(order => !filter.Status.HasValue || order.Status == filter.Status.Value)
            .Where(order => !filter.FromDate.HasValue || order.OrderDate >= filter.FromDate.Value.Date)
            .Where(order => !filter.ToDate.HasValue || order.OrderDate < filter.ToDate.Value.Date.AddDays(1));
    }

    private static IOrderedQueryable<ServiceOrder> ApplySorting(IQueryable<ServiceOrder> query, string? sorting)
    {
        var normalized = sorting?.Trim().ToLowerInvariant();
        return normalized switch
        {
            "orderno asc" => query.OrderBy(order => order.OrderNo).ThenBy(order => order.Id),
            "orderno desc" => query.OrderByDescending(order => order.OrderNo).ThenByDescending(order => order.Id),
            "orderdate asc" => query.OrderBy(order => order.OrderDate).ThenBy(order => order.Id),
            "status asc" => query.OrderBy(order => order.Status).ThenByDescending(order => order.OrderDate).ThenBy(order => order.Id),
            "status desc" => query.OrderByDescending(order => order.Status).ThenByDescending(order => order.OrderDate).ThenBy(order => order.Id),
            "customer asc" => query.OrderBy(order => order.CustomerNameSnapshot).ThenBy(order => order.Id),
            "customer desc" => query.OrderByDescending(order => order.CustomerNameSnapshot).ThenByDescending(order => order.Id),
            _ => query.OrderByDescending(order => order.OrderDate).ThenByDescending(order => order.Id)
        };
    }

    private static IQueryable<CustomerAsset> ApplyAssetFilter(
        IQueryable<CustomerAsset> query,
        Guid? customerId,
        string? searchText)
    {
        var search = searchText?.Trim();
        return query
            .Where(asset => asset.Status == CustomerAssetStatus.Active || asset.Status == CustomerAssetStatus.PendingReview)
            .Where(asset => !customerId.HasValue || asset.CustomerId == customerId.Value)
            .Where(asset => string.IsNullOrEmpty(search) ||
                            asset.AssetNo.Contains(search) ||
                            asset.CustomerCodeSnapshot.Contains(search) ||
                            asset.CustomerNameSnapshot.Contains(search) ||
                            (asset.SerialNo != null && asset.SerialNo.Contains(search)) ||
                            (asset.Model != null && asset.Model.Contains(search)));
    }

    private static IQueryable<Component> ApplyMaterialFilter(VPureLuxDbContext db, string? searchText)
    {
        var search = searchText?.Trim();
        return db.Components.AsNoTracking()
            .Where(component => component.Status == CatalogItemStatus.Active)
            .Where(component => db.StockItems.Any(stock =>
                stock.CatalogItemId == component.Id &&
                stock.ItemType == StockItemType.Component &&
                stock.Status == InventoryEntityStatus.Active &&
                stock.IsInventoryEnabled))
            .Where(component => string.IsNullOrEmpty(search) ||
                                component.Code.Contains(search) ||
                                component.Name.Contains(search));
    }

    private static IQueryable<ServiceWork> ApplyWorkFilter(IQueryable<ServiceWork> query, string? searchText)
    {
        var search = searchText?.Trim();
        return query.Where(work => work.Status == ServiceWorkStatus.Active && work.Unit != null && work.Unit != "")
            .Where(work => string.IsNullOrEmpty(search) || work.Code.Contains(search) || work.Name.Contains(search));
    }

    private static IQueryable<Volo.Abp.Identity.IdentityUser> ApplyTechnicianFilter(
        IQueryable<Volo.Abp.Identity.IdentityUser> query,
        string? searchText)
    {
        var search = searchText?.Trim();
        return query.Where(user => user.IsActive)
            .Where(user => string.IsNullOrEmpty(search) ||
                           user.UserName.Contains(search) ||
                           (user.Name != null && user.Name.Contains(search)) ||
                           (user.Email != null && user.Email.Contains(search)));
    }
}
