using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using VPureLux.Catalog;
using VPureLux.EntityFrameworkCore;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EntityFrameworkCore;

namespace VPureLux.Warranty;

public class EfCoreWarrantyReadRepository : IWarrantyReadRepository, ITransientDependency
{
    private readonly IDbContextProvider<VPureLuxDbContext> _dbContextProvider;

    public EfCoreWarrantyReadRepository(IDbContextProvider<VPureLuxDbContext> dbContextProvider)
    {
        _dbContextProvider = dbContextProvider;
    }

    public async Task<long> GetPolicyCountAsync(WarrantyPolicyFilter filter, CancellationToken cancellationToken = default)
    {
        var query = await CreatePolicyQueryAsync(filter);
        return await query.LongCountAsync(cancellationToken);
    }

    public async Task<List<WarrantyPolicyListItem>> GetPolicyListAsync(WarrantyPolicyFilter filter, CancellationToken cancellationToken = default)
    {
        var query = await CreatePolicyQueryAsync(filter);
        return await ApplyPolicySorting(query, filter.Sorting)
            .Skip(filter.SkipCount)
            .Take(filter.MaxResultCount)
            .ToListAsync(cancellationToken);
    }

    public async Task<long> GetMachineSettingCountAsync(
        ProductMachineSettingFilter filter,
        CancellationToken cancellationToken = default)
    {
        var query = await CreateMachineSettingQueryAsync(filter);
        return await query.LongCountAsync(cancellationToken);
    }

    public async Task<List<ProductMachineSettingListItem>> GetMachineSettingListAsync(
        ProductMachineSettingFilter filter,
        CancellationToken cancellationToken = default)
    {
        var query = await CreateMachineSettingQueryAsync(filter);
        return await ApplyMachineSettingSorting(query, filter.Sorting)
            .Skip(filter.SkipCount)
            .Take(filter.MaxResultCount)
            .ToListAsync(cancellationToken);
    }

    public async Task<long> GetSyncFailureCountAsync(
        CustomerCareSyncFailureFilter filter,
        CancellationToken cancellationToken = default)
    {
        var query = await CreateSyncFailureQueryAsync(filter);
        return await query.LongCountAsync(cancellationToken);
    }

    public async Task<List<CustomerCareSyncFailureListItem>> GetSyncFailureListAsync(
        CustomerCareSyncFailureFilter filter,
        CancellationToken cancellationToken = default)
    {
        var query = await CreateSyncFailureQueryAsync(filter);
        return await ApplySyncFailureSorting(query, filter.Sorting)
            .Skip(filter.SkipCount)
            .Take(filter.MaxResultCount)
            .ToListAsync(cancellationToken);
    }

    public async Task<long> GetPendingInstallationCountAsync(
        PendingInstallationFilter filter,
        CancellationToken cancellationToken = default)
    {
        var query = await CreatePendingInstallationQueryAsync(filter);
        return await query.LongCountAsync(cancellationToken);
    }

    public async Task<List<PendingInstallationListItem>> GetPendingInstallationListAsync(
        PendingInstallationFilter filter,
        CancellationToken cancellationToken = default)
    {
        var query = await CreatePendingInstallationQueryAsync(filter);
        return await ApplyPendingInstallationSorting(query, filter.Sorting)
            .Skip(filter.SkipCount)
            .Take(filter.MaxResultCount)
            .ToListAsync(cancellationToken);
    }

    public async Task<long> GetAssetCountAsync(
        CustomerAssetFilter filter,
        CancellationToken cancellationToken = default)
    {
        var query = await CreateAssetQueryAsync(filter);
        return await query.LongCountAsync(cancellationToken);
    }

    public async Task<List<CustomerAssetListItem>> GetAssetListAsync(
        CustomerAssetFilter filter,
        CancellationToken cancellationToken = default)
    {
        var query = await CreateAssetQueryAsync(filter);
        return await ApplyAssetSorting(query, filter.Sorting)
            .Skip(filter.SkipCount)
            .Take(filter.MaxResultCount)
            .ToListAsync(cancellationToken);
    }

    public async Task<long> GetAssetHistoryCountAsync(
        AssetMaintenanceHistoryFilter filter,
        CancellationToken cancellationToken = default)
    {
        var query = await CreateAssetHistoryQueryAsync(filter);
        return await query.LongCountAsync(cancellationToken);
    }

    public async Task<List<AssetMaintenanceEventListItem>> GetAssetHistoryListAsync(
        AssetMaintenanceHistoryFilter filter,
        CancellationToken cancellationToken = default)
    {
        var query = await CreateAssetHistoryQueryAsync(filter);
        return await ApplyAssetHistorySorting(query, filter.Sorting)
            .Skip(filter.SkipCount)
            .Take(filter.MaxResultCount)
            .ToListAsync(cancellationToken);
    }

    public async Task<long> GetReminderCountAsync(WarrantyReminderFilter filter, CancellationToken cancellationToken = default)
    {
        var query = await CreateReminderQueryAsync(filter);
        return await query.LongCountAsync(cancellationToken);
    }

    public async Task<List<WarrantyReminderListItem>> GetReminderListAsync(WarrantyReminderFilter filter, CancellationToken cancellationToken = default)
    {
        var query = await CreateReminderQueryAsync(filter);
        return await ApplyReminderSorting(query, filter.Sorting)
            .Skip(filter.SkipCount)
            .Take(filter.MaxResultCount)
            .ToListAsync(cancellationToken);
    }

    private async Task<IQueryable<WarrantyPolicyListItem>> CreatePolicyQueryAsync(WarrantyPolicyFilter filter)
    {
        var dbContext = await _dbContextProvider.GetDbContextAsync();
        var query =
            from component in dbContext.Components.AsNoTracking()
            join policy in dbContext.ComponentReplacementPolicies.AsNoTracking()
                on component.Id equals policy.ComponentId into policyJoin
            from policy in policyJoin.DefaultIfEmpty()
            where component.Status == CatalogItemStatus.Active
            select new WarrantyPolicyListItem
            {
                ComponentId = component.Id,
                ComponentCode = component.Code,
                ComponentName = component.Name,
                ComponentUnit = component.Unit,
                PolicyId = policy == null ? null : policy.Id,
                IsEnabled = policy != null && policy.IsEnabled,
                CycleMonths = policy == null ? null : policy.CycleMonths,
                WarningDaysBeforeDue = policy == null ? null : policy.WarningDaysBeforeDue,
                Note = policy == null ? null : policy.Note
            };

        if (!filter.SearchText.IsNullOrWhiteSpace())
        {
            query = query.Where(x =>
                x.ComponentCode.Contains(filter.SearchText!) ||
                x.ComponentName.Contains(filter.SearchText!));
        }

        if (filter.IsEnabled.HasValue)
        {
            query = query.Where(x => x.IsEnabled == filter.IsEnabled.Value);
        }

        return query;
    }

    private async Task<IQueryable<WarrantyReminderListItem>> CreateReminderQueryAsync(WarrantyReminderFilter filter)
    {
        var dbContext = await _dbContextProvider.GetDbContextAsync();
        var query =
            from reminder in dbContext.AssetReplacementReminders.AsNoTracking()
            join asset in dbContext.CustomerAssets.AsNoTracking()
                on reminder.CustomerAssetId equals asset.Id
            select new WarrantyReminderListItem
            {
                Id = reminder.Id,
                CustomerAssetId = asset.Id,
                AssetNo = asset.AssetNo,
                CustomerCode = asset.CustomerCodeSnapshot,
                CustomerName = asset.CustomerNameSnapshot,
                ProductCode = asset.ProductCodeSnapshot ?? string.Empty,
                ProductName = asset.ProductNameSnapshot ?? asset.Model ?? string.Empty,
                ComponentCode = reminder.ComponentCodeSnapshot,
                ComponentName = reminder.ComponentNameSnapshot,
                ComponentUnit = reminder.ComponentUnitSnapshot,
                QuantityPerProduct = reminder.QuantityPerProductSnapshot,
                DueDate = reminder.DueDate,
                WarningDate = reminder.WarningDate,
                CycleMonths = reminder.CycleMonthsSnapshot,
                WarningDaysBeforeDue = reminder.WarningDaysBeforeDueSnapshot,
                Status = reminder.Status,
                TimingStatus = reminder.Status != AssetReplacementReminderStatus.Pending
                    ? WarrantyReminderTimingStatus.Closed
                    : reminder.DueDate < filter.AsOfDate
                        ? WarrantyReminderTimingStatus.Overdue
                        : reminder.WarningDate.HasValue && reminder.WarningDate.Value <= filter.AsOfDate
                            ? WarrantyReminderTimingStatus.Warning
                            : WarrantyReminderTimingStatus.NotDue,
                OrderNo = asset.OrderNoSnapshot ?? string.Empty,
                LineNo = asset.SalesOrderLineNoSnapshot ?? 0,
                Note = reminder.Note
            };

        if (!filter.SearchText.IsNullOrWhiteSpace())
        {
            query = query.Where(x =>
                x.AssetNo.Contains(filter.SearchText!) ||
                x.CustomerCode.Contains(filter.SearchText!) ||
                x.CustomerName.Contains(filter.SearchText!) ||
                x.ProductCode.Contains(filter.SearchText!) ||
                x.ProductName.Contains(filter.SearchText!) ||
                x.ComponentCode.Contains(filter.SearchText!) ||
                x.ComponentName.Contains(filter.SearchText!) ||
                x.OrderNo.Contains(filter.SearchText!));
        }

        if (filter.Status.HasValue)
        {
            query = query.Where(x => x.Status == filter.Status.Value);
        }

        if (filter.TimingStatus.HasValue)
        {
            query = query.Where(x => x.TimingStatus == filter.TimingStatus.Value);
        }

        if (filter.DueFrom.HasValue)
        {
            query = query.Where(x => x.DueDate >= filter.DueFrom.Value.Date);
        }

        if (filter.DueTo.HasValue)
        {
            query = query.Where(x => x.DueDate <= filter.DueTo.Value.Date);
        }

        return query;
    }

    private async Task<IQueryable<ProductMachineSettingListItem>> CreateMachineSettingQueryAsync(
        ProductMachineSettingFilter filter)
    {
        var dbContext = await _dbContextProvider.GetDbContextAsync();
        var query =
            from product in dbContext.Products.AsNoTracking()
            join setting in dbContext.ProductMachineSettings.AsNoTracking()
                on product.Id equals setting.ProductId into settingJoin
            from setting in settingJoin.DefaultIfEmpty()
            select new ProductMachineSettingListItem
            {
                ProductId = product.Id,
                ProductCode = product.Code,
                ProductName = product.Name,
                ProductStatus = product.Status,
                SettingId = setting == null ? null : setting.Id,
                IsMachine = setting != null && setting.IsMachine,
                Note = setting == null ? null : setting.Note
            };

        if (!filter.SearchText.IsNullOrWhiteSpace())
        {
            query = query.Where(x =>
                x.ProductCode.Contains(filter.SearchText!) ||
                x.ProductName.Contains(filter.SearchText!));
        }

        if (filter.IsMachine.HasValue)
        {
            query = query.Where(x => x.IsMachine == filter.IsMachine.Value);
        }

        return query;
    }

    private async Task<IQueryable<CustomerCareSyncFailureListItem>> CreateSyncFailureQueryAsync(
        CustomerCareSyncFailureFilter filter)
    {
        var db = await _dbContextProvider.GetDbContextAsync();
        var query =
            from failure in db.CustomerCareSyncFailures.AsNoTracking()
            join order in db.SalesOrders.AsNoTracking()
                on failure.SalesOrderId equals order.Id into orderJoin
            from order in orderJoin.DefaultIfEmpty()
            let line = order == null
                ? null
                : order.Lines.FirstOrDefault(item => item.Id == failure.SalesOrderLineId)
            select new CustomerCareSyncFailureListItem
            {
                Id = failure.Id,
                OrderNo = order == null ? string.Empty : order.OrderNo,
                LineNo = line == null ? null : line.LineNo,
                ProductCode = line == null ? string.Empty : line.ItemCodeSnapshot,
                ProductName = line == null ? string.Empty : line.ItemNameSnapshot,
                Quantity = line == null ? null : line.Quantity,
                ErrorCode = failure.ErrorCode,
                ErrorMessage = failure.ErrorMessage,
                ErrorContext = failure.ErrorContext,
                AttemptCount = failure.AttemptCount,
                LastOccurredAt = failure.LastOccurredAt,
                NextRetryAt = failure.NextRetryAt,
                Status = failure.Status
            };

        if (!filter.SearchText.IsNullOrWhiteSpace())
        {
            query = query.Where(x =>
                x.OrderNo.Contains(filter.SearchText!) ||
                x.ProductCode.Contains(filter.SearchText!) ||
                x.ProductName.Contains(filter.SearchText!) ||
                x.ErrorMessage.Contains(filter.SearchText!) ||
                (x.ErrorCode != null && x.ErrorCode.Contains(filter.SearchText!)));
        }

        if (filter.Status.HasValue)
        {
            query = query.Where(x => x.Status == filter.Status.Value);
        }

        return query;
    }

    private async Task<IQueryable<PendingInstallationListItem>> CreatePendingInstallationQueryAsync(
        PendingInstallationFilter filter)
    {
        var db = await _dbContextProvider.GetDbContextAsync();
        var query =
            from asset in db.CustomerAssets.AsNoTracking()
            where asset.Status == CustomerAssetStatus.PendingInstallation
            select new PendingInstallationListItem
            {
                Id = asset.Id,
                AssetNo = asset.AssetNo,
                CustomerCode = asset.CustomerCodeSnapshot,
                CustomerName = asset.CustomerNameSnapshot,
                ProductCode = asset.ProductCodeSnapshot ?? string.Empty,
                ProductName = asset.ProductNameSnapshot ?? string.Empty,
                OrderNo = asset.OrderNoSnapshot ?? string.Empty,
                LineNo = asset.SalesOrderLineNoSnapshot,
                UnitIndex = asset.SourceUnitIndex,
                SoldDate = asset.SoldDate,
                PositionCount = db.CustomerAssetComponents.Count(position =>
                    position.CustomerAssetId == asset.Id &&
                    position.Status == CustomerAssetComponentStatus.PendingInstallation)
            };

        if (!filter.SearchText.IsNullOrWhiteSpace())
        {
            query = query.Where(x =>
                x.AssetNo.Contains(filter.SearchText!) ||
                x.CustomerCode.Contains(filter.SearchText!) ||
                x.CustomerName.Contains(filter.SearchText!) ||
                x.ProductCode.Contains(filter.SearchText!) ||
                x.ProductName.Contains(filter.SearchText!) ||
                x.OrderNo.Contains(filter.SearchText!));
        }

        return query;
    }

    private async Task<IQueryable<CustomerAssetListItem>> CreateAssetQueryAsync(CustomerAssetFilter filter)
    {
        var db = await _dbContextProvider.GetDbContextAsync();
        var query =
            from asset in db.CustomerAssets.AsNoTracking()
            select new CustomerAssetListItem
            {
                Id = asset.Id,
                AssetNo = asset.AssetNo,
                CustomerCode = asset.CustomerCodeSnapshot,
                CustomerName = asset.CustomerNameSnapshot,
                Source = asset.Source,
                ProductCode = asset.ProductCodeSnapshot,
                ProductName = asset.ProductNameSnapshot,
                Brand = asset.Brand,
                Model = asset.Model,
                SerialNo = asset.SerialNo,
                Status = asset.Status,
                PositionCount = db.CustomerAssetComponents.Count(position =>
                    position.CustomerAssetId == asset.Id &&
                    position.Status != CustomerAssetComponentStatus.Inactive),
                NextDueDate = db.AssetReplacementReminders
                    .Where(reminder =>
                        reminder.CustomerAssetId == asset.Id &&
                        reminder.Status == AssetReplacementReminderStatus.Pending)
                    .Select(reminder => (DateTime?)reminder.DueDate)
                    .Min()
            };

        if (!filter.SearchText.IsNullOrWhiteSpace())
        {
            query = query.Where(x =>
                x.AssetNo.Contains(filter.SearchText!) ||
                x.CustomerCode.Contains(filter.SearchText!) ||
                x.CustomerName.Contains(filter.SearchText!) ||
                (x.ProductCode != null && x.ProductCode.Contains(filter.SearchText!)) ||
                (x.ProductName != null && x.ProductName.Contains(filter.SearchText!)) ||
                (x.Brand != null && x.Brand.Contains(filter.SearchText!)) ||
                (x.Model != null && x.Model.Contains(filter.SearchText!)) ||
                (x.SerialNo != null && x.SerialNo.Contains(filter.SearchText!)));
        }

        if (filter.Source.HasValue)
        {
            query = query.Where(x => x.Source == filter.Source.Value);
        }

        return query;
    }

    private async Task<IQueryable<AssetMaintenanceEventListItem>> CreateAssetHistoryQueryAsync(
        AssetMaintenanceHistoryFilter filter)
    {
        var db = await _dbContextProvider.GetDbContextAsync();
        return db.AssetMaintenanceEvents.AsNoTracking()
            .Where(maintenanceEvent => maintenanceEvent.CustomerAssetId == filter.CustomerAssetId)
            .Select(maintenanceEvent => new AssetMaintenanceEventListItem
            {
                Id = maintenanceEvent.Id,
                CustomerAssetId = maintenanceEvent.CustomerAssetId,
                CustomerAssetComponentId = maintenanceEvent.CustomerAssetComponentId,
                EventType = maintenanceEvent.EventType,
                SourceType = maintenanceEvent.SourceType,
                ComponentCode = maintenanceEvent.ComponentCodeSnapshot,
                ComponentName = maintenanceEvent.ComponentNameSnapshot,
                OccurredAt = maintenanceEvent.OccurredAt,
                CreatorId = maintenanceEvent.CreatorId,
                Note = maintenanceEvent.Note
            });
    }

    private static IQueryable<WarrantyPolicyListItem> ApplyPolicySorting(IQueryable<WarrantyPolicyListItem> query, string? sorting) =>
        sorting switch
        {
            "componentCode desc" => query.OrderByDescending(x => x.ComponentCode),
            "componentName asc" => query.OrderBy(x => x.ComponentName),
            "componentName desc" => query.OrderByDescending(x => x.ComponentName),
            "cycleMonths asc" => query.OrderBy(x => x.CycleMonths),
            "cycleMonths desc" => query.OrderByDescending(x => x.CycleMonths),
            _ => query.OrderBy(x => x.ComponentCode)
        };

    private static IQueryable<ProductMachineSettingListItem> ApplyMachineSettingSorting(
        IQueryable<ProductMachineSettingListItem> query,
        string? sorting) =>
        sorting switch
        {
            "productCode desc" => query.OrderByDescending(x => x.ProductCode),
            "productName asc" => query.OrderBy(x => x.ProductName),
            "productName desc" => query.OrderByDescending(x => x.ProductName),
            "isMachine asc" => query.OrderBy(x => x.IsMachine).ThenBy(x => x.ProductCode),
            "isMachine desc" => query.OrderByDescending(x => x.IsMachine).ThenBy(x => x.ProductCode),
            _ => query.OrderBy(x => x.ProductCode)
        };

    private static IQueryable<CustomerCareSyncFailureListItem> ApplySyncFailureSorting(
        IQueryable<CustomerCareSyncFailureListItem> query,
        string? sorting) =>
        sorting switch
        {
            "lastOccurredAt asc" => query.OrderBy(x => x.LastOccurredAt),
            "attemptCount asc" => query.OrderBy(x => x.AttemptCount),
            "attemptCount desc" => query.OrderByDescending(x => x.AttemptCount),
            "orderNo asc" => query.OrderBy(x => x.OrderNo),
            "orderNo desc" => query.OrderByDescending(x => x.OrderNo),
            _ => query.OrderByDescending(x => x.LastOccurredAt).ThenBy(x => x.OrderNo)
        };

    private static IQueryable<PendingInstallationListItem> ApplyPendingInstallationSorting(
        IQueryable<PendingInstallationListItem> query,
        string? sorting) =>
        sorting switch
        {
            "assetNo desc" => query.OrderByDescending(x => x.AssetNo),
            "customerName asc" => query.OrderBy(x => x.CustomerName),
            "customerName desc" => query.OrderByDescending(x => x.CustomerName),
            "productName asc" => query.OrderBy(x => x.ProductName),
            "productName desc" => query.OrderByDescending(x => x.ProductName),
            "soldDate asc" => query.OrderBy(x => x.SoldDate),
            "soldDate desc" => query.OrderByDescending(x => x.SoldDate),
            _ => query.OrderBy(x => x.SoldDate).ThenBy(x => x.OrderNo).ThenBy(x => x.UnitIndex)
        };

    private static IQueryable<CustomerAssetListItem> ApplyAssetSorting(
        IQueryable<CustomerAssetListItem> query,
        string? sorting) =>
        sorting switch
        {
            "assetNo desc" => query.OrderByDescending(x => x.AssetNo),
            "customerName asc" => query.OrderBy(x => x.CustomerName),
            "customerName desc" => query.OrderByDescending(x => x.CustomerName),
            "model asc" => query.OrderBy(x => x.Model),
            "model desc" => query.OrderByDescending(x => x.Model),
            "nextDueDate asc" => query.OrderBy(x => x.NextDueDate),
            "nextDueDate desc" => query.OrderByDescending(x => x.NextDueDate),
            _ => query.OrderBy(x => x.CustomerName).ThenBy(x => x.AssetNo)
        };

    private static IQueryable<AssetMaintenanceEventListItem> ApplyAssetHistorySorting(
        IQueryable<AssetMaintenanceEventListItem> query,
        string? sorting) =>
        sorting switch
        {
            "occurredAt asc" => query.OrderBy(x => x.OccurredAt),
            "eventType asc" => query.OrderBy(x => x.EventType).ThenByDescending(x => x.OccurredAt),
            "eventType desc" => query.OrderByDescending(x => x.EventType).ThenByDescending(x => x.OccurredAt),
            _ => query.OrderByDescending(x => x.OccurredAt).ThenByDescending(x => x.Id)
        };

    private static IQueryable<WarrantyReminderListItem> ApplyReminderSorting(IQueryable<WarrantyReminderListItem> query, string? sorting) =>
        sorting switch
        {
            "dueDate asc" => query.OrderBy(x => x.DueDate),
            "dueDate desc" => query.OrderByDescending(x => x.DueDate),
            "customerName asc" => query.OrderBy(x => x.CustomerName),
            "customerName desc" => query.OrderByDescending(x => x.CustomerName),
            "productName asc" => query.OrderBy(x => x.ProductName),
            "productName desc" => query.OrderByDescending(x => x.ProductName),
            "componentName asc" => query.OrderBy(x => x.ComponentName),
            "componentName desc" => query.OrderByDescending(x => x.ComponentName),
            _ => query.OrderBy(x => x.DueDate).ThenBy(x => x.CustomerCode).ThenBy(x => x.ComponentCode)
        };
}
