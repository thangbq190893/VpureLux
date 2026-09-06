using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VPureLux.Catalog;
using VPureLux.Inventory;
using VPureLux.Warranty;
using Volo.Abp;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Guids;
using Volo.Abp.Linq;
using Volo.Abp.Uow;

namespace VPureLux.Service;

public class ServiceCompletionProcessor(
    InventoryManager inventory,
    IStockItemRepository stocks,
    IInventoryLotRepository lots,
    IInventoryBalanceRepository balances,
    IInventoryTransactionRepository transactions,
    IWarehouseRepository warehouses,
    IRepository<Component, Guid> components,
    IRepository<CustomerAsset, Guid> assets,
    IRepository<CustomerAssetComponent, Guid> positions,
    CustomerAssetOperationCoordinator assetCoordinator,
    ICustomerCareServiceCompletion customerCare,
    IAsyncQueryableExecuter query,
    IGuidGenerator ids,
    IUnitOfWorkManager uowManager) : ITransientDependency
{
    public virtual async Task ApplyAsync(ServiceOrder order, ServiceCompletionCommand command, Guid? operatorId)
    {
        order.ValidateCompletion(command);
        await assetCoordinator.HoldAsync(order.CustomerAssetId);
        var asset = await assets.GetAsync(order.CustomerAssetId);
        if (asset.CustomerId != order.CustomerId || asset.Status is not (CustomerAssetStatus.Active or CustomerAssetStatus.PendingReview))
            throw new BusinessException(ServiceErrorCodes.AssetUnavailable).WithData("CustomerAssetId", asset.Id);
        var quantities = command.Lines.ToDictionary(x => x.LineId, x => x.ActualQuantity);
        var materials = order.Lines.Where(x => x.LineType == ServiceOrderLineType.Material && quantities[x.Id] > 0)
            .OrderBy(x => x.LineNo).ToList();
        var positioned = materials.Where(x => x.CustomerAssetComponentId.HasValue).ToList();
        var positionIds = positioned.Select(x => x.CustomerAssetComponentId!.Value).ToArray();
        if (positionIds.Distinct().Count() != positionIds.Length ||
            await positions.CountAsync(x => x.CustomerAssetId == asset.Id && positionIds.Contains(x.Id)) != positionIds.Length)
            throw new BusinessException(ServiceErrorCodes.MaterialPositionMismatch).WithData("ServiceOrderId", order.Id);
        InventoryTransaction? transaction = null;
        var costs = new Dictionary<Guid, (decimal Cost, Guid InventoryLineId)>();
        if (materials.Count > 0)
        {
            var warehouse = await warehouses.GetAsync(order.WarehouseId);
            if (warehouse.Status != InventoryEntityStatus.Active)
                throw new BusinessException(ServiceErrorCodes.MaterialUnavailable).WithData("WarehouseId", order.WarehouseId);
            var componentIds = materials.Select(x => x.ComponentId!.Value).Distinct().ToArray();
            var availableComponents = await components.GetListAsync(x => componentIds.Contains(x.Id));
            var stockSet = await stocks.GetListAsync(x => x.ItemType == StockItemType.Component &&
                componentIds.Contains(x.CatalogItemId) && x.IsInventoryEnabled);
            if (availableComponents.Count != componentIds.Length || stockSet.Count != componentIds.Length)
                throw new BusinessException(ServiceErrorCodes.MaterialUnavailable).WithData("ServiceOrderId", order.Id);
            var byComponent = stockSet.ToDictionary(x => x.CatalogItemId);
            var stockIds = stockSet.Select(x => x.Id).ToArray();
            var fifoLots = await query.ToListAsync((await lots.GetQueryableAsync())
                .Where(x => x.WarehouseId == order.WarehouseId && stockIds.Contains(x.StockItemId) && x.AvailableQuantity > 0)
                .OrderBy(x => x.ReceivedAt).ThenBy(x => x.CreationTime).ThenBy(x => x.Id));
            var balanceSet = (await balances.GetForStockItemsAsync(order.WarehouseId, stockIds)).ToDictionary(x => x.StockItemId);
            var fifoByStock = fifoLots.GroupBy(x => x.StockItemId).ToDictionary(x => x.Key, x => x.ToArray());
            transaction = inventory.CreateTransaction(order.WarehouseId, InventoryTransactionType.ServiceIssue,
                $"service:{order.Id:N}", command.Hash, nameof(ServiceOrder), order.Id);
            foreach (var material in materials)
            {
                var stock = byComponent[material.ComponentId!.Value];
                var requested = quantities[material.Id];
                var componentLots = fifoByStock.GetValueOrDefault(stock.Id) ?? [];
                balanceSet.TryGetValue(stock.Id, out var balance);
                var available = Math.Min(componentLots.Sum(x => x.AvailableQuantity), balance?.QuantityOnHand ?? 0m);
                if (available < requested)
                    throw new BusinessException(ServiceErrorCodes.StockShortage)
                        .WithData("ServiceOrderId", order.Id).WithData("OrderNo", order.OrderNo)
                        .WithData("Line", material.LineNo).WithData("Component", material.ItemCodeSnapshot)
                        .WithData("Warehouse", warehouse.Code).WithData("Requested", requested).WithData("Available", available);
                var issue = transaction.AddIssueLine(ids.Create(), stock.Id, requested);
                var allocated = inventory.AllocateFifo(transaction, issue, componentLots);
                var cost = allocated.Sum(x => x.TotalCost);
                balance!.ApplyMovement(-requested, -cost, command.CompletedAt);
                costs.Add(material.Id, (cost, issue.Id));
            }
            transaction.Post(command.CompletedAt);
            await transactions.InsertAsync(transaction);
            var allocatedLotIds = transaction.Lines.SelectMany(x => x.Allocations).Select(x => x.InventoryLotId).ToHashSet();
            await lots.UpdateManyAsync(fifoLots.Where(x => allocatedLotIds.Contains(x.Id)).ToList());
            // Deliberately flush stock inside the SAME transaction before the CustomerCare boundary.
            await uowManager.Current!.SaveChangesAsync();
        }
        await customerCare.ApplyAsync(new CustomerCareCompletion(order.Id, asset.Id, command.CompletedAt, operatorId,
            positioned.Select(x => new PerformedReplacement(x.Id, x.CustomerAssetComponentId!.Value,
                x.ComponentId!.Value, x.ItemCodeSnapshot, x.ItemNameSnapshot, x.UnitSnapshot, quantities[x.Id])).ToList()));
        order.Complete(command, transaction?.Id, costs);
    }
}
