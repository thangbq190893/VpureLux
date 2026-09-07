using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VPureLux.Sales;
using Volo.Abp;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Guids;

namespace VPureLux.Warranty;

public interface ISalesRevisionCustomerCareReconciler
{
    Task ReconcileAsync(SalesOrder order, SalesOrderRevision revision);
}

public class SalesRevisionCustomerCareReconciler : ISalesRevisionCustomerCareReconciler, ITransientDependency
{
    private readonly IProductMachineSettingRepository _machineSettings;
    private readonly IRepository<CustomerAsset, Guid> _assets;
    private readonly IRepository<CustomerAssetComponent, Guid> _assetComponents;
    private readonly IGuidGenerator _guidGenerator;

    public SalesRevisionCustomerCareReconciler(
        IProductMachineSettingRepository machineSettings,
        IRepository<CustomerAsset, Guid> assets,
        IRepository<CustomerAssetComponent, Guid> assetComponents,
        IGuidGenerator guidGenerator)
    {
        _machineSettings = machineSettings;
        _assets = assets;
        _assetComponents = assetComponents;
        _guidGenerator = guidGenerator;
    }

    public async Task ReconcileAsync(SalesOrder order, SalesOrderRevision revision)
    {
        var productIds = revision.Lines
            .SelectMany(x => new[] { x.BeforeProductId, (Guid?)x.ProductId })
            .Where(x => x.HasValue)
            .Select(x => x!.Value)
            .Distinct()
            .ToArray();
        var machineProductIds = (await _machineSettings.GetByProductIdsAsync(productIds))
            .Where(x => x.IsMachine)
            .Select(x => x.ProductId)
            .ToHashSet();
        if (machineProductIds.Count == 0)
        {
            return;
        }

        var assets = await _assets.GetListAsync(
            x => x.SalesOrderId == order.Id && x.Source == CustomerAssetSource.SoldByCompany);
        var assetsByLine = assets
            .Where(x => x.SalesOrderLineId.HasValue && x.SourceUnitIndex.HasValue)
            .GroupBy(x => x.SalesOrderLineId!.Value)
            .ToDictionary(x => x.Key, x => x.OrderBy(y => y.SourceUnitIndex).ToList());
        var effectiveLines = order.EffectiveLines.ToDictionary(x => x.Id);
        var newAssets = new List<CustomerAsset>();
        var newComponents = new List<CustomerAssetComponent>();
        var changedAssets = new List<CustomerAsset>();
        var reason = $"Sales revision {revision.RevisionNo} for {order.OrderNo}";

        foreach (var line in revision.Lines.OrderBy(x => x.LineNo))
        {
            var beforeMachine = line.BeforeProductId.HasValue && machineProductIds.Contains(line.BeforeProductId.Value);
            var afterMachine = !line.IsRemoved && machineProductIds.Contains(line.ProductId);
            if (!beforeMachine && !afterMachine)
            {
                continue;
            }

            var sourceAssets = line.SourceSalesOrderLineId.HasValue
                ? assetsByLine.GetValueOrDefault(line.SourceSalesOrderLineId.Value) ?? []
                : [];
            var pendingSourceAssets = sourceAssets
                .Where(x => !x.InstalledAt.HasValue &&
                            x.Status is CustomerAssetStatus.PendingInstallation or CustomerAssetStatus.Cancelled)
                .ToList();
            var productChanged = line.BeforeProductId.HasValue && line.BeforeProductId.Value != line.ProductId;

            if (beforeMachine && (line.IsRemoved || productChanged || !afterMachine))
            {
                foreach (var asset in pendingSourceAssets.Where(x => x.Status == CustomerAssetStatus.PendingInstallation))
                {
                    asset.CancelBeforeInstallation(reason);
                    changedAssets.Add(asset);
                }
            }

            if (!afterMachine || !line.EffectiveSalesOrderLineId.HasValue)
            {
                continue;
            }

            var effectiveLine = effectiveLines[line.EffectiveSalesOrderLineId.Value];
            var targetCount = ToPositiveInteger(line.Quantity, "SalesQuantity");
            var reusable = !productChanged && line.SourceSalesOrderLineId == line.EffectiveSalesOrderLineId
                ? pendingSourceAssets.ToDictionary(x => x.SourceUnitIndex!.Value)
                : new Dictionary<int, CustomerAsset>();

            foreach (var asset in reusable.Values.Where(x => x.SourceUnitIndex > targetCount &&
                                                              x.Status == CustomerAssetStatus.PendingInstallation))
            {
                asset.CancelBeforeInstallation(reason);
                changedAssets.Add(asset);
            }

            for (var unitIndex = 1; unitIndex <= targetCount; unitIndex++)
            {
                if (reusable.TryGetValue(unitIndex, out var existing))
                {
                    if (existing.Status == CustomerAssetStatus.Cancelled)
                    {
                        existing.ReopenPendingInstallation(reason);
                        changedAssets.Add(existing);
                    }
                    continue;
                }

                var asset = CustomerAsset.CreateSoldMachine(
                    _guidGenerator.Create(), order.CustomerId, effectiveLine.ProductId, order.Id, effectiveLine.Id,
                    effectiveLine.LineNo, unitIndex,
                    CreateAssetNo(order.OrderNo, revision.RevisionNo, effectiveLine.LineNo, unitIndex),
                    order.OrderNo, order.CustomerCodeSnapshot, order.CustomerNameSnapshot,
                    effectiveLine.ItemCodeSnapshot, effectiveLine.ItemNameSnapshot,
                    order.ConfirmedAt ?? order.OrderDate, reason);
                newAssets.Add(asset);
                AddComponents(asset, effectiveLine, newComponents);
            }
        }

        if (changedAssets.Count > 0)
        {
            await _assets.UpdateManyAsync(changedAssets.DistinctBy(x => x.Id));
        }
        if (newAssets.Count > 0)
        {
            await _assets.InsertManyAsync(newAssets);
            await _assetComponents.InsertManyAsync(newComponents);
        }
    }

    private void AddComponents(
        CustomerAsset asset,
        SalesOrderLine line,
        ICollection<CustomerAssetComponent> target)
    {
        var position = 1;
        foreach (var item in line.BomSnapshotItems.OrderBy(x => x.ComponentCode).ThenBy(x => x.Id))
        {
            target.Add(new CustomerAssetComponent(
                _guidGenerator.Create(), asset.Id, $"BOM-{position++:D2}", item.ComponentName,
                item.ComponentId, item.ComponentCode, item.ComponentName, item.Unit,
                ToPositiveInteger(item.QuantityPerProduct, $"BomQuantity:{item.ComponentCode}"),
                null, pendingInstallation: true));
        }
    }

    private static int ToPositiveInteger(decimal value, string field)
    {
        if (value <= 0 || value != decimal.Truncate(value) || value > int.MaxValue)
        {
            throw new BusinessException(VPureLuxDomainErrorCodes.ValidationFailed)
                .WithData("Field", field)
                .WithData("Value", value);
        }
        return decimal.ToInt32(value);
    }

    private static string CreateAssetNo(string orderNo, int revisionNo, int lineNo, int unitIndex) =>
        $"WA-{orderNo}-R{revisionNo:D2}-L{lineNo:D2}-{unitIndex:D2}";
}
