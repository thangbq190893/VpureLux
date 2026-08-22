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
                AssetNo = asset.AssetNo,
                CustomerCode = asset.CustomerCodeSnapshot,
                CustomerName = asset.CustomerNameSnapshot,
                ProductCode = asset.ProductCodeSnapshot,
                ProductName = asset.ProductNameSnapshot,
                ComponentCode = reminder.ComponentCodeSnapshot,
                ComponentName = reminder.ComponentNameSnapshot,
                ComponentUnit = reminder.ComponentUnitSnapshot,
                QuantityPerProduct = reminder.QuantityPerProductSnapshot,
                DueDate = reminder.DueDate,
                CycleMonths = reminder.CycleMonthsSnapshot,
                WarningDaysBeforeDue = reminder.WarningDaysBeforeDueSnapshot,
                Status = reminder.Status,
                OrderNo = asset.OrderNoSnapshot,
                LineNo = asset.SalesOrderLineNoSnapshot,
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
