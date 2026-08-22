using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VPureLux.Sales;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Guids;
using Volo.Abp.Linq;

namespace VPureLux.Warranty;

public class WarrantySalesIntegrationService : ITransientDependency
{
    private readonly IRepository<CustomerAsset, Guid> _customerAssets;
    private readonly IRepository<AssetReplacementReminder, Guid> _reminders;
    private readonly IComponentReplacementPolicyRepository _policies;
    private readonly IGuidGenerator _guidGenerator;
    private readonly IAsyncQueryableExecuter _asyncExecuter;

    public WarrantySalesIntegrationService(
        IRepository<CustomerAsset, Guid> customerAssets,
        IRepository<AssetReplacementReminder, Guid> reminders,
        IComponentReplacementPolicyRepository policies,
        IGuidGenerator guidGenerator,
        IAsyncQueryableExecuter asyncExecuter)
    {
        _customerAssets = customerAssets;
        _reminders = reminders;
        _policies = policies;
        _guidGenerator = guidGenerator;
        _asyncExecuter = asyncExecuter;
    }

    public async Task CreateAssetsAndRemindersForConfirmedOrderAsync(SalesOrder order)
    {
        var componentIds = order.Lines
            .SelectMany(x => x.BomSnapshotItems)
            .Select(x => x.ComponentId)
            .Distinct()
            .ToList();

        if (componentIds.Count == 0)
        {
            return;
        }

        var policies = (await _policies.GetEnabledByComponentIdsAsync(componentIds))
            .ToDictionary(x => x.ComponentId);

        if (policies.Count == 0)
        {
            return;
        }

        var existingLineIds = (await _asyncExecuter.ToListAsync((await _customerAssets.GetQueryableAsync())
                .Where(x => x.SalesOrderId == order.Id)
                .Select(x => x.SalesOrderLineId)))
            .ToHashSet();

        var soldDate = (order.ConfirmedAt ?? order.OrderDate).Date;
        foreach (var line in order.Lines.OrderBy(x => x.LineNo))
        {
            if (existingLineIds.Contains(line.Id))
            {
                continue;
            }

            var reminderItems = line.BomSnapshotItems
                .Where(x => policies.ContainsKey(x.ComponentId))
                .OrderBy(x => x.ComponentCode)
                .ToList();

            if (reminderItems.Count == 0)
            {
                continue;
            }

            var assetCount = GetAssetCount(line.Quantity);
            for (var unitIndex = 1; unitIndex <= assetCount; unitIndex++)
            {
                var asset = new CustomerAsset(
                    _guidGenerator.Create(),
                    order.CustomerId,
                    line.ProductId,
                    order.Id,
                    line.Id,
                    line.LineNo,
                    CreateAssetNo(order.OrderNo, line.LineNo, unitIndex),
                    order.OrderNo,
                    order.CustomerCodeSnapshot,
                    order.CustomerNameSnapshot,
                    line.ItemCodeSnapshot,
                    line.ItemNameSnapshot,
                    soldDate,
                    soldDate);

                await _customerAssets.InsertAsync(asset);

                foreach (var item in reminderItems)
                {
                    var policy = policies[item.ComponentId];
                    var reminder = new AssetReplacementReminder(
                        _guidGenerator.Create(),
                        asset.Id,
                        item.ComponentId,
                        order.Id,
                        line.Id,
                        item.ComponentCode,
                        item.ComponentName,
                        item.Unit,
                        item.QuantityPerProduct,
                        soldDate.AddMonths(policy.CycleMonths),
                        policy.CycleMonths,
                        policy.WarningDaysBeforeDue);

                    await _reminders.InsertAsync(reminder);
                }
            }
        }
    }

    private static int GetAssetCount(decimal salesQuantity)
    {
        var rounded = decimal.Round(salesQuantity, 0, MidpointRounding.AwayFromZero);
        if (rounded <= 0)
        {
            return 1;
        }

        return decimal.ToInt32(rounded);
    }

    private static string CreateAssetNo(string orderNo, int lineNo, int unitIndex) =>
        $"WA-{orderNo}-L{lineNo:D2}-{unitIndex:D2}";
}
