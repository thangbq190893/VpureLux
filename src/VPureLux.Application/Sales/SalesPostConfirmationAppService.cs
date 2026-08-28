using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using VPureLux.Bom;
using VPureLux.Catalog;
using VPureLux.Inventory;
using VPureLux.Permissions;
using VPureLux.Pricing;
using VPureLux.Warranty;
using Volo.Abp;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Uow;

namespace VPureLux.Sales;

[Authorize(VPureLuxPermissions.Sales.View)]
public class SalesPostConfirmationAppService : ApplicationService, ISalesPostConfirmationAppService
{
    private readonly ISalesOrderRepository _orders;
    private readonly ISalesOrderRevisionRepository _revisions;
    private readonly ISalesOrderCancellationRepository _cancellations;
    private readonly ISalesOrderRefundRepository _refunds;
    private readonly ISalesOrderPaymentRepository _payments;
    private readonly IRepository<CustomerAsset, Guid> _assets;
    private readonly IProductRepository _products;
    private readonly IBomVersionRepository _boms;
    private readonly IComponentRepository _components;
    private readonly IProductSuggestedPriceVersionRepository _prices;
    private readonly IStockItemRepository _stockItems;
    private readonly IInventoryLotRepository _lots;
    private readonly IInventoryTransactionRepository _transactions;
    private readonly IInventoryBalanceRepository _balances;
    private readonly InventoryManager _inventoryManager;
    private readonly SalesOrderOperationCoordinator _coordinator;
    private readonly IUnitOfWorkManager _unitOfWorkManager;

    public SalesPostConfirmationAppService(
        ISalesOrderRepository orders,
        ISalesOrderRevisionRepository revisions,
        ISalesOrderCancellationRepository cancellations,
        ISalesOrderRefundRepository refunds,
        ISalesOrderPaymentRepository payments,
        IRepository<CustomerAsset, Guid> assets,
        IProductRepository products,
        IBomVersionRepository boms,
        IComponentRepository components,
        IProductSuggestedPriceVersionRepository prices,
        IStockItemRepository stockItems,
        IInventoryLotRepository lots,
        IInventoryTransactionRepository transactions,
        IInventoryBalanceRepository balances,
        InventoryManager inventoryManager,
        SalesOrderOperationCoordinator coordinator,
        IUnitOfWorkManager unitOfWorkManager)
    {
        _orders = orders;
        _revisions = revisions;
        _cancellations = cancellations;
        _refunds = refunds;
        _payments = payments;
        _assets = assets;
        _products = products;
        _boms = boms;
        _components = components;
        _prices = prices;
        _stockItems = stockItems;
        _lots = lots;
        _transactions = transactions;
        _balances = balances;
        _inventoryManager = inventoryManager;
        _coordinator = coordinator;
        _unitOfWorkManager = unitOfWorkManager;
    }

    [Authorize(VPureLuxPermissions.Sales.AdjustConfirmedBeforeInstallation)]
    [UnitOfWork(IsDisabled = true)]
    public async Task<SalesOrderRevisionDto> OpenRevisionAsync(Guid salesOrderId, OpenSalesOrderRevisionDto input) =>
        await _coordinator.ExecuteAsync(salesOrderId, async () =>
        {
            var order = await GetOrderAsync(salesOrderId);
            await EnsureModificationAllowedAsync(order);
            if (await _revisions.FindActiveByOrderIdAsync(order.Id) != null)
            {
                throw new BusinessException(VPureLuxDomainErrorCodes.SalesRevisionAlreadyActive);
            }
            if (await _cancellations.FindByOrderIdAsync(order.Id) != null)
            {
                throw new BusinessException(VPureLuxDomainErrorCodes.SalesCancellationAlreadyActive);
            }

            var revision = new SalesOrderRevision(
                GuidGenerator.Create(), order.Id, await _revisions.GetNextRevisionNoAsync(order.Id),
                input.Reason, order.CustomerId, order.TotalRevenueAmount, order.EffectiveLines);
            await _revisions.InsertAsync(revision, autoSave: true);
            return ToDto(revision);
        });

    public async Task<SalesOrderRevisionDto> GetRevisionAsync(Guid revisionId) =>
        ToDto(await _revisions.GetAsync(revisionId, includeDetails: true));

    [Authorize(VPureLuxPermissions.Sales.AdjustConfirmedBeforeInstallation)]
    [UnitOfWork(IsDisabled = true)]
    public async Task<SalesOrderRevisionDto> UpdateRevisionAsync(Guid revisionId, UpdateSalesOrderRevisionDto input)
    {
        var salesOrderId = await ReadRevisionOrderIdAsync(revisionId);
        return await _coordinator.ExecuteAsync(salesOrderId, async () =>
        {
            var revision = await _revisions.GetAsync(revisionId, includeDetails: true);
            var order = await GetOrderAsync(salesOrderId);
            await EnsureModificationAllowedAsync(order);
            EnsureActiveRevision(revision, order);
            if (input.CustomerId != order.CustomerId || revision.CustomerIdSnapshot != order.CustomerId)
            {
                throw new BusinessException(VPureLuxDomainErrorCodes.SalesRevisionNotAllowed)
                    .WithData("Reason", "CustomerId is immutable after confirmation.");
            }

            var existing = revision.Lines.ToDictionary(x => x.Id);
            var suppliedIds = input.Lines.Where(x => x.RevisionLineId.HasValue).Select(x => x.RevisionLineId!.Value).ToList();
            if (suppliedIds.Count != suppliedIds.Distinct().Count() || suppliedIds.Any(id => !existing.ContainsKey(id)) ||
                existing.Keys.Except(suppliedIds).Any())
            {
                throw new BusinessException(VPureLuxDomainErrorCodes.ValidationFailed);
            }

            var productIds = input.Lines.Where(x => !x.IsRemoved).Select(x => x.ProductId).Distinct().ToArray();
            var productMap = await LoadActiveProductsAsync(productIds);
            var publishedBomMap = await LoadPublishedBomMapAsync(productIds);
            var priceMap = await LoadPriceMapAsync(productIds, order.OrderDate);

            foreach (var inputLine in input.Lines)
            {
                if (inputLine.RevisionLineId.HasValue && inputLine.IsRemoved)
                {
                    revision.RemoveLine(inputLine.RevisionLineId.Value);
                    continue;
                }
                if (!productMap.ContainsKey(inputLine.ProductId))
                {
                    throw new BusinessException(VPureLuxDomainErrorCodes.ProductNotFound);
                }

                SalesOrderRevisionLine? current = inputLine.RevisionLineId.HasValue
                    ? existing[inputLine.RevisionLineId.Value]
                    : null;
                var sameProduct = current != null && current.ProductId == inputLine.ProductId;
                var bomId = sameProduct ? current!.BomVersionId : publishedBomMap.GetValueOrDefault(inputLine.ProductId)?.Id
                    ?? throw new BusinessException(VPureLuxDomainErrorCodes.SalesBomMustBePublished);
                var price = sameProduct
                    ? (SuggestedPriceVersionId: current!.SuggestedPriceVersionId, SuggestedPrice: current.SuggestedPriceSnapshot)
                    : priceMap.GetValueOrDefault(inputLine.ProductId);
                await EnsureOverridePermissionAsync(price.SuggestedPrice, inputLine.ActualSellingPrice);

                if (current == null)
                {
                    revision.AddLine(
                        GuidGenerator.Create(), inputLine.ProductId, bomId, inputLine.Quantity,
                        price.SuggestedPriceVersionId, price.SuggestedPrice,
                        inputLine.ActualSellingPrice, inputLine.OverrideReason);
                }
                else
                {
                    revision.UpdateLine(
                        current.Id, inputLine.ProductId, bomId, inputLine.Quantity,
                        price.SuggestedPriceVersionId, price.SuggestedPrice,
                        inputLine.ActualSellingPrice, inputLine.OverrideReason);
                }
            }

            await _revisions.UpdateAsync(revision, autoSave: true);
            return ToDto(revision);
        });
    }

    [Authorize(VPureLuxPermissions.Sales.ConfirmReturnedGoods)]
    [UnitOfWork(IsDisabled = true)]
    public async Task ConfirmRevisionReturnedGoodsAsync(Guid revisionId, ConfirmRevisionReturnedGoodsDto input)
    {
        var salesOrderId = await ReadRevisionOrderIdAsync(revisionId);
        await _coordinator.ExecuteAsync(salesOrderId, async () =>
        {
            var revision = await _revisions.GetAsync(revisionId, includeDetails: true);
            var order = await GetOrderAsync(salesOrderId);
            await EnsureModificationAllowedAsync(order);
            EnsureActiveRevision(revision, order);
            if (input.RevisionLineIds.Count == 0 || input.RevisionLineIds.Distinct().Count() != input.RevisionLineIds.Count)
            {
                throw new BusinessException(VPureLuxDomainErrorCodes.ValidationFailed);
            }
            foreach (var lineId in input.RevisionLineIds)
            {
                revision.ConfirmReturnedGoods(lineId, CurrentUser.Id, Clock.Now, input.Reason);
            }
            await _revisions.UpdateAsync(revision, autoSave: true);
        });
    }

    [Authorize(VPureLuxPermissions.Sales.AdjustConfirmedBeforeInstallation)]
    [UnitOfWork(IsDisabled = true)]
    public async Task<SalesOrderRevisionDto> ApplyRevisionAsync(Guid revisionId, ApplySalesOrderRevisionDto input)
    {
        var salesOrderId = await ReadRevisionOrderIdAsync(revisionId);
        return await _coordinator.ExecuteAsync(salesOrderId, async () =>
        {
            var revision = await _revisions.GetAsync(revisionId, includeDetails: true);
            if (revision.Status == SalesOrderRevisionStatus.Applied)
            {
                revision.Apply(input.IdempotencyKey, CurrentUser.Id, revision.AppliedAt ?? Clock.Now,
                    revision.AppliedTotal ?? revision.BeforeTotal, 0);
                return ToDto(revision);
            }
            var duplicate = await _revisions.FindByApplyIdempotencyKeyAsync(input.IdempotencyKey);
            if (duplicate != null && duplicate.Id != revision.Id)
            {
                throw new BusinessException(VPureLuxDomainErrorCodes.SalesRevisionIdempotencyConflict);
            }

            var order = await GetOrderAsync(salesOrderId);
            await EnsureModificationAllowedAsync(order);
            EnsureActiveRevision(revision, order);
            if (revision.Lines.Where(x => x.RequiresReturnConfirmation).Any(x => !x.ReturnConfirmedAt.HasValue))
            {
                throw new BusinessException(VPureLuxDomainErrorCodes.SalesRevisionReturnConfirmationRequired);
            }

            var context = await LoadApplyContextAsync(order, revision);
            var sourceLineOrder = order.EffectiveLines.ToDictionary(x => x.Id, x => x.LineNo);
            var existingLines = revision.Lines
                .Where(x => x.SourceSalesOrderLineId.HasValue)
                .OrderByDescending(x => x.IsRemoved)
                .ThenBy(x => sourceLineOrder[x.SourceSalesOrderLineId!.Value])
                .ToList();
            foreach (var revisionLine in existingLines)
            {
                await ApplyLineAsync(order, revision, revisionLine, context);
                // Flush each existing line inside the transaction so filtered unique line numbers
                // are released or compacted before a new effective line takes their place.
                await _orders.UpdateAsync(order);
                await _revisions.UpdateAsync(revision, autoSave: true);
            }
            foreach (var revisionLine in revision.Lines
                         .Where(x => !x.SourceSalesOrderLineId.HasValue)
                         .OrderBy(x => x.LineNo))
            {
                await ApplyLineAsync(order, revision, revisionLine, context);
            }
            order.RecalculateEffectiveTotals();
            var netPaid = await _payments.GetPostedPaidAmountsAsync([order.Id]);
            revision.Apply(input.IdempotencyKey, CurrentUser.Id, Clock.Now, order.TotalRevenueAmount,
                netPaid.GetValueOrDefault(order.Id));
            await _orders.UpdateAsync(order);
            await _revisions.UpdateAsync(revision, autoSave: true);
            return ToDto(revision);
        });
    }

    [Authorize(VPureLuxPermissions.Sales.AdjustConfirmedBeforeInstallation)]
    [UnitOfWork(IsDisabled = true)]
    public async Task CancelRevisionAsync(Guid revisionId, ReasonDto input)
    {
        var salesOrderId = await ReadRevisionOrderIdAsync(revisionId);
        await _coordinator.ExecuteAsync(salesOrderId, async () =>
        {
            var revision = await _revisions.GetAsync(revisionId, includeDetails: true);
            revision.Cancel(CurrentUser.Id, Clock.Now, input.Reason);
            await _revisions.UpdateAsync(revision, autoSave: true);
        });
    }

    [Authorize(VPureLuxPermissions.Sales.CancelConfirmedBeforeInstallation)]
    [UnitOfWork(IsDisabled = true)]
    public async Task<SalesOrderCancellationDto> CancelConfirmedAsync(Guid salesOrderId, CancelConfirmedSalesOrderDto input) =>
        await _coordinator.ExecuteAsync(salesOrderId, async () =>
        {
            var order = await GetOrderAsync(salesOrderId);
            var existing = await _cancellations.FindByOrderIdAsync(order.Id);
            if (existing != null)
            {
                return ToDto(existing);
            }
            await EnsureModificationAllowedAsync(order);
            if (await _revisions.FindActiveByOrderIdAsync(order.Id) != null)
            {
                throw new BusinessException(VPureLuxDomainErrorCodes.SalesRevisionAlreadyActive);
            }
            var posted = await _payments.GetPostedPaidAmountsAsync([order.Id]);
            var cancellation = new SalesOrderCancellation(
                GuidGenerator.Create(), order.Id, input.ReasonGroup, input.Reason,
                CurrentUser.Id, Clock.Now, order.EffectiveLines.Any(x => x.InventoryTransactionId.HasValue),
                posted.GetValueOrDefault(order.Id));
            order.CancelConfirmed(Clock.Now);
            await CancelPendingAssetsAsync(order.Id, input.Reason);
            await _cancellations.InsertAsync(cancellation);
            await _orders.UpdateAsync(order, autoSave: true);
            return ToDto(cancellation);
        });

    public async Task<SalesOrderCancellationDto> GetCancellationAsync(Guid cancellationId) =>
        ToDto(await _cancellations.GetAsync(cancellationId));

    [Authorize(VPureLuxPermissions.Sales.ConfirmReturnedGoods)]
    [UnitOfWork(IsDisabled = true)]
    public async Task<SalesOrderCancellationDto> ConfirmReturnedGoodsAsync(Guid cancellationId, ConfirmCancellationReturnedGoodsDto input)
    {
        var salesOrderId = await ReadCancellationOrderIdAsync(cancellationId);
        return await _coordinator.ExecuteAsync(salesOrderId, async () =>
        {
            var cancellation = await _cancellations.GetAsync(cancellationId);
            if (cancellation.StockStatus == SalesOrderCancellationStockStatus.Completed)
            {
                return ToDto(cancellation);
            }
            if (!input.IsEligibleForRestock)
            {
                cancellation.MarkStockException(input.Reason);
            }
            else
            {
                var order = await GetOrderAsync(salesOrderId);
                var reversalId = await ReverseEntireOrderAsync(order, cancellation, input);
                cancellation.CompleteStockReturn(reversalId, Clock.Now);
            }
            await _cancellations.UpdateAsync(cancellation, autoSave: true);
            return ToDto(cancellation);
        });
    }

    [Authorize(VPureLuxPermissions.Sales.ManageRefunds)]
    [UnitOfWork(IsDisabled = true)]
    public async Task<SalesOrderRefundDto> RecordCancellationRefundAsync(Guid cancellationId, RecordSalesOrderRefundDto input)
    {
        var salesOrderId = await ReadCancellationOrderIdAsync(cancellationId);
        return await _coordinator.ExecuteAsync(salesOrderId, async () =>
        {
            var existing = await _refunds.FindByIdempotencyKeyAsync(input.IdempotencyKey);
            if (existing != null)
            {
                if (existing.SalesOrderCancellationId != cancellationId)
                {
                    throw new BusinessException(VPureLuxDomainErrorCodes.SalesRevisionIdempotencyConflict);
                }
                return ToDto(existing);
            }
            var cancellation = await _cancellations.GetAsync(cancellationId);
            cancellation.RecordRefund(input.Amount, input.RefundedAt);
            var refund = CreateRefund(salesOrderId, null, cancellationId, input);
            await _refunds.InsertAsync(refund);
            await _cancellations.UpdateAsync(cancellation, autoSave: true);
            return ToDto(refund);
        });
    }

    [Authorize(VPureLuxPermissions.Sales.ManageRefunds)]
    [UnitOfWork(IsDisabled = true)]
    public async Task<SalesOrderRefundDto> RecordRevisionRefundAsync(Guid revisionId, RecordSalesOrderRefundDto input)
    {
        var salesOrderId = await ReadRevisionOrderIdAsync(revisionId);
        return await _coordinator.ExecuteAsync(salesOrderId, async () =>
        {
            var existing = await _refunds.FindByIdempotencyKeyAsync(input.IdempotencyKey);
            if (existing != null)
            {
                if (existing.SalesOrderRevisionId != revisionId)
                {
                    throw new BusinessException(VPureLuxDomainErrorCodes.SalesRevisionIdempotencyConflict);
                }
                return ToDto(existing);
            }
            var revision = await _revisions.GetAsync(revisionId, includeDetails: true);
            if (revision.Status != SalesOrderRevisionStatus.Applied || revision.RefundDue <= 0)
            {
                throw new BusinessException(VPureLuxDomainErrorCodes.SalesRefundExceedsAmountDue);
            }
            var refunded = await _refunds.GetRefundedAmountForRevisionAsync(revisionId);
            if (input.Amount <= 0 || refunded + input.Amount > revision.RefundDue)
            {
                throw new BusinessException(VPureLuxDomainErrorCodes.SalesRefundExceedsAmountDue);
            }
            var refund = CreateRefund(salesOrderId, revisionId, null, input);
            await _refunds.InsertAsync(refund, autoSave: true);
            return ToDto(refund);
        });
    }

    [Authorize(VPureLuxPermissions.Sales.ManageRefunds)]
    public async Task VoidPaymentAsync(Guid paymentId, ReasonDto input)
    {
        var payment = await _payments.GetAsync(paymentId);
        payment.Void(CurrentUser.Id, Clock.Now, input.Reason);
        await _payments.UpdateAsync(payment, autoSave: true);
    }

    private async Task ApplyLineAsync(
        SalesOrder order,
        SalesOrderRevision revision,
        SalesOrderRevisionLine line,
        ApplyContext context)
    {
        var source = line.SourceSalesOrderLineId.HasValue
            ? order.EffectiveLines.Single(x => x.Id == line.SourceSalesOrderLineId.Value)
            : null;
        if (line.IsUnchanged() && source!.LineNo == line.LineNo)
        {
            line.MarkApplied(source!.Id, null, null, source.CostAmountSnapshot);
            return;
        }

        var productChanged = source != null && source.ProductId != line.ProductId;
        var quantityDelta = source == null ? line.Quantity : line.Quantity - source.Quantity;
        Guid? reversalId = null;
        List<SalesOrderRevisionAllocation> reversed = new();
        decimal reversedCost = 0;
        if (source != null && (line.IsRemoved || productChanged || quantityDelta < 0))
        {
            var requirements = line.IsRemoved || productChanged
                ? null
                : source.BomSnapshotItems.ToDictionary(
                    x => context.StockItemByComponentId[x.ComponentId].Id,
                    x => x.QuantityPerProduct * -quantityDelta);
            (reversalId, reversed) = await PostReversalAsync(order, revision, line, source, context, requirements);
            reversedCost = reversed.Sum(x => x.TotalCost);
        }

        if (line.IsRemoved)
        {
            order.RemoveEffectiveRevisionLine(source!.Id, revision.Id);
            line.MarkApplied(null, null, reversalId, 0, reversed);
            return;
        }

        Guid? issueId = null;
        decimal issueCost = 0;
        if (source == null || productChanged || quantityDelta > 0)
        {
            var issueQuantity = source == null || productChanged ? line.Quantity : quantityDelta;
            (issueId, issueCost) = await PostIssueAsync(order, revision, line, issueQuantity, context);
        }

        var product = context.ProductById[line.ProductId];
        var bom = context.BomById[line.BomVersionId];
        var snapshots = BuildBomSnapshots(bom, context.ComponentById, line.Quantity);
        var effectiveCost = source == null || productChanged
            ? issueCost
            : source.CostAmountSnapshot + issueCost - reversedCost;
        var inventoryTransactionId = productChanged || source == null
            ? issueId!.Value
            : source.InventoryTransactionId!.Value;

        if (source == null)
        {
            var added = order.AddEffectiveRevisionLine(
                GuidGenerator.Create(), revision.Id, line.LineNo, product.Id, bom.Id, line.Quantity,
                line.SuggestedPriceVersionId, line.SuggestedPriceSnapshot, line.ActualSellingPrice, line.OverrideReason,
                product.Code, product.Name, SalesConsts.DefaultProductUnit, bom.VersionNo.Value,
                inventoryTransactionId, effectiveCost, snapshots);
            line.MarkApplied(added.Id, issueId, reversalId, effectiveCost, reversed);
        }
        else
        {
            order.ApplyEffectiveRevisionLine(
                source.Id, revision.Id, line.LineNo, product.Id, bom.Id, line.Quantity,
                line.SuggestedPriceVersionId, line.SuggestedPriceSnapshot, line.ActualSellingPrice, line.OverrideReason,
                product.Code, product.Name, SalesConsts.DefaultProductUnit, bom.VersionNo.Value,
                inventoryTransactionId, effectiveCost, snapshots);
            line.MarkApplied(source.Id, issueId, reversalId, effectiveCost, reversed);
        }
    }

    private async Task<(Guid Id, decimal Cost)> PostIssueAsync(
        SalesOrder order,
        SalesOrderRevision revision,
        SalesOrderRevisionLine revisionLine,
        decimal issueQuantity,
        ApplyContext context)
    {
        var bom = context.BomById[revisionLine.BomVersionId];
        var requirements = bom.OrderedItems
            .GroupBy(x => x.ComponentId)
            .Select(x => new { ComponentId = x.Key, Quantity = x.Sum(y => y.Quantity) * issueQuantity })
            .OrderBy(x => x.ComponentId).ToList();
        var key = $"sales-rev-i:{Hash($"{order.Id}|{revision.Id}|{revisionLine.Id}")[..48]}";
        var hash = Hash($"{order.Id}|{revision.Id}|{revisionLine.Id}|{string.Join(";", requirements.Select(x => $"{x.ComponentId}:{x.Quantity}"))}");
        var existing = await _inventoryManager.FindExistingTransactionAsync(key);
        if (existing != null)
        {
            if (existing.RequestHash != hash) throw new BusinessException(VPureLuxDomainErrorCodes.InventoryIdempotencyConflict);
            return (existing.Id, existing.TotalIssueCost);
        }
        var transaction = _inventoryManager.CreateTransaction(
            order.WarehouseId, InventoryTransactionType.SalesIssue, key, hash,
            nameof(SalesOrderRevisionLine), revisionLine.Id, bom.Id);
        foreach (var requirement in requirements)
        {
            var stockItem = context.StockItemByComponentId[requirement.ComponentId];
            var txLine = transaction.AddIssueLine(GuidGenerator.Create(), stockItem.Id, requirement.Quantity);
            var allocations = _inventoryManager.AllocateFifo(transaction, txLine, context.Lots);
            await _balances.ApplyMovementAsync(order.WarehouseId, stockItem.Id, -requirement.Quantity,
                -allocations.Sum(x => x.TotalCost), Clock.Now);
        }
        transaction.Post(Clock.Now);
        await _transactions.InsertAsync(transaction);
        return (transaction.Id, transaction.TotalIssueCost);
    }

    private async Task<(Guid Id, List<SalesOrderRevisionAllocation> Allocations)> PostReversalAsync(
        SalesOrder order,
        SalesOrderRevision revision,
        SalesOrderRevisionLine revisionLine,
        SalesOrderLine source,
        ApplyContext context,
        IReadOnlyDictionary<Guid, decimal>? requiredByStockItem)
    {
        var available = BuildEffectiveAllocationLedger(source, context);
        var selected = new List<AllocationFact>();
        if (requiredByStockItem == null)
        {
            selected.AddRange(available.Where(x => x.Quantity > 0));
        }
        else
        {
            foreach (var requirement in requiredByStockItem.OrderBy(x => x.Key))
            {
                var remaining = requirement.Value;
                foreach (var allocation in available.Where(x => x.StockItemId == requirement.Key && x.Quantity > 0)
                             .OrderByDescending(x => x.InventoryLotId))
                {
                    if (remaining == 0) break;
                    var quantity = Math.Min(remaining, allocation.Quantity);
                    selected.Add(allocation with { Quantity = quantity });
                    remaining -= quantity;
                }
                if (remaining > 0)
                {
                    throw new BusinessException(VPureLuxDomainErrorCodes.SalesOrderCannotBeModified)
                        .WithData("StockItemId", requirement.Key).WithData("MissingReturnQuantity", remaining);
                }
            }
        }

        var key = $"sales-rev-r:{Hash($"{order.Id}|{revision.Id}|{revisionLine.Id}")[..48]}";
        var hash = Hash($"{order.Id}|{revision.Id}|{revisionLine.Id}|{string.Join(";", selected.Select(x => $"{x.StockItemId}:{x.InventoryLotId}:{x.Quantity}:{x.UnitCost}"))}");
        var existing = await _inventoryManager.FindExistingTransactionAsync(key);
        if (existing != null)
        {
            if (existing.RequestHash != hash) throw new BusinessException(VPureLuxDomainErrorCodes.InventoryIdempotencyConflict);
            return (existing.Id, selected.Select(ToRevisionAllocation).ToList());
        }
        var transaction = _inventoryManager.CreateTransaction(
            order.WarehouseId, InventoryTransactionType.AdjustmentIncrease, key, hash,
            nameof(SalesOrderRevisionLine), revisionLine.Id, source.BomVersionId,
            $"Điều chỉnh đơn {order.OrderNo}, revision {revision.RevisionNo}");
        foreach (var allocation in selected)
        {
            var lot = context.LotById[allocation.InventoryLotId];
            lot.Restore(allocation.Quantity);
            transaction.AddReceiptLine(GuidGenerator.Create(), allocation.StockItemId, allocation.Quantity,
                lot.LotNo, Clock.Now, allocation.UnitCost);
            await _balances.ApplyMovementAsync(order.WarehouseId, allocation.StockItemId,
                allocation.Quantity, allocation.Quantity * allocation.UnitCost, Clock.Now);
        }
        transaction.Post(Clock.Now);
        await _transactions.InsertAsync(transaction);
        return (transaction.Id, selected.Select(ToRevisionAllocation).ToList());
    }

    private List<AllocationFact> BuildEffectiveAllocationLedger(SalesOrderLine source, ApplyContext context)
    {
        var facts = context.IssueTransactions
            .Where(x => x.Id == source.InventoryTransactionId || context.IssueTransactionIdsByLineId.GetValueOrDefault(source.Id)?.Contains(x.Id) == true)
            .SelectMany(tx => tx.Lines.SelectMany(line => line.Allocations.Select(a =>
                new AllocationFact(line.StockItemId, a.InventoryLotId, a.Quantity, a.UnitCost))))
            .ToList();
        var reversed = context.PriorReversedAllocationsByLineId.GetValueOrDefault(source.Id) ?? [];
        var ledger = facts.GroupBy(x => new { x.StockItemId, x.InventoryLotId, x.UnitCost })
            .ToDictionary(x => x.Key, x => x.Sum(y => y.Quantity));
        foreach (var item in reversed)
        {
            var key = new { item.StockItemId, item.InventoryLotId, item.UnitCost };
            ledger[key] = ledger.GetValueOrDefault(key) - item.Quantity;
        }
        return ledger.Where(x => x.Value > 0)
            .Select(x => new AllocationFact(x.Key.StockItemId, x.Key.InventoryLotId, x.Value, x.Key.UnitCost)).ToList();
    }

    private async Task<Guid> ReverseEntireOrderAsync(
        SalesOrder order,
        SalesOrderCancellation cancellation,
        ConfirmCancellationReturnedGoodsDto input)
    {
        var context = await LoadCancellationContextAsync(order);
        var allocations = order.EffectiveLines.SelectMany(line => BuildEffectiveAllocationLedger(line, context)).ToList();
        var key = $"sales-cancellation-return:{order.Id}:{cancellation.Id}";
        var hash = Hash($"{order.Id}|{cancellation.Id}|{string.Join(";", allocations.Select(x => $"{x.StockItemId}:{x.InventoryLotId}:{x.Quantity}:{x.UnitCost}"))}");
        var existing = await _inventoryManager.FindExistingTransactionAsync(key);
        if (existing != null)
        {
            if (existing.RequestHash != hash) throw new BusinessException(VPureLuxDomainErrorCodes.InventoryIdempotencyConflict);
            return existing.Id;
        }
        var transaction = _inventoryManager.CreateTransaction(
            order.WarehouseId, InventoryTransactionType.AdjustmentIncrease, key, hash,
            nameof(SalesOrderCancellation), cancellation.Id, reason: input.Reason);
        foreach (var allocation in allocations)
        {
            var lot = context.LotById[allocation.InventoryLotId];
            lot.Restore(allocation.Quantity);
            transaction.AddReceiptLine(GuidGenerator.Create(), allocation.StockItemId, allocation.Quantity,
                lot.LotNo, Clock.Now, allocation.UnitCost);
            await _balances.ApplyMovementAsync(order.WarehouseId, allocation.StockItemId,
                allocation.Quantity, allocation.Quantity * allocation.UnitCost, Clock.Now);
        }
        transaction.Post(Clock.Now);
        await _transactions.InsertAsync(transaction);
        return transaction.Id;
    }

    private async Task<ApplyContext> LoadApplyContextAsync(SalesOrder order, SalesOrderRevision revision)
    {
        var activeLines = revision.Lines.Where(x => !x.IsRemoved).ToList();
        var productIds = activeLines.Select(x => x.ProductId).Distinct().ToArray();
        var productMap = await LoadActiveProductsAsync(productIds);
        var bomIds = activeLines.Select(x => x.BomVersionId).Distinct().ToArray();
        var boms = await AsyncExecuter.ToListAsync((await _boms.WithDetailsAsync()).Where(x => bomIds.Contains(x.Id)));
        if (boms.Count != bomIds.Length) throw new BusinessException(VPureLuxDomainErrorCodes.SalesBomMustBePublished);
        var componentIds = boms.SelectMany(x => x.OrderedItems).Select(x => x.ComponentId)
            .Concat(order.EffectiveLines.SelectMany(x => x.BomSnapshotItems).Select(x => x.ComponentId)).Distinct().ToArray();
        var components = await AsyncExecuter.ToListAsync((await _components.GetQueryableAsync()).Where(x => componentIds.Contains(x.Id)));
        var stockItems = await AsyncExecuter.ToListAsync((await _stockItems.GetQueryableAsync())
            .Where(x => x.ItemType == StockItemType.Component && componentIds.Contains(x.CatalogItemId)));
        var stockItemByComponent = stockItems.GroupBy(x => x.CatalogItemId).ToDictionary(x => x.Key, x => x.Single());
        if (stockItemByComponent.Count != componentIds.Length) throw new BusinessException(VPureLuxDomainErrorCodes.StockItemNotFound);
        var stockItemIds = stockItems.Select(x => x.Id).ToArray();
        var lots = await AsyncExecuter.ToListAsync((await _lots.GetQueryableAsync())
            .Where(x => x.WarehouseId == order.WarehouseId && stockItemIds.Contains(x.StockItemId)));

        var applied = await _revisions.GetAppliedByOrderIdAsync(order.Id);
        var issueIdsByLine = applied.SelectMany(x => x.Lines)
            .Where(x => x.EffectiveSalesOrderLineId.HasValue && x.IssueInventoryTransactionId.HasValue)
            .GroupBy(x => x.EffectiveSalesOrderLineId!.Value)
            .ToDictionary(x => x.Key, x => x.Select(y => y.IssueInventoryTransactionId!.Value).ToHashSet());
        var allIssueIds = order.EffectiveLines.Where(x => x.InventoryTransactionId.HasValue).Select(x => x.InventoryTransactionId!.Value)
            .Concat(issueIdsByLine.Values.SelectMany(x => x)).Distinct().ToArray();
        var issueTransactions = await AsyncExecuter.ToListAsync((await _transactions.WithDetailsAsync())
            .Where(x => allIssueIds.Contains(x.Id)));
        var reversedByLine = applied.SelectMany(x => x.Lines)
            .Where(x => x.EffectiveSalesOrderLineId.HasValue)
            .GroupBy(x => x.EffectiveSalesOrderLineId!.Value)
            .ToDictionary(x => x.Key, x => x.SelectMany(y => y.ReversedAllocations).ToList());
        return new ApplyContext(
            productMap, boms.ToDictionary(x => x.Id), components.ToDictionary(x => x.Id),
            stockItemByComponent, lots, lots.ToDictionary(x => x.Id), issueTransactions,
            issueIdsByLine, reversedByLine);
    }

    private async Task<ApplyContext> LoadCancellationContextAsync(SalesOrder order)
    {
        var revision = new SalesOrderRevision(Guid.NewGuid(), order.Id, 1, "Cancellation context", order.CustomerId,
            order.TotalRevenueAmount, order.EffectiveLines);
        return await LoadApplyContextAsync(order, revision);
    }

    private async Task<Dictionary<Guid, Product>> LoadActiveProductsAsync(IEnumerable<Guid> ids)
    {
        var idSet = ids.Distinct().ToArray();
        var products = await AsyncExecuter.ToListAsync((await _products.GetQueryableAsync())
            .Where(x => idSet.Contains(x.Id) && x.Status == CatalogItemStatus.Active));
        return products.ToDictionary(x => x.Id);
    }

    private async Task<Dictionary<Guid, BomVersion>> LoadPublishedBomMapAsync(IEnumerable<Guid> productIds)
    {
        var ids = productIds.Distinct().ToArray();
        var boms = await AsyncExecuter.ToListAsync((await _boms.WithDetailsAsync())
            .Where(x => ids.Contains(x.ProductId) && x.Status == BomStatus.Published));
        return boms.GroupBy(x => x.ProductId).ToDictionary(x => x.Key, x => x.Single());
    }

    private async Task<Dictionary<Guid, (Guid? SuggestedPriceVersionId, decimal? SuggestedPrice)>> LoadPriceMapAsync(
        IEnumerable<Guid> productIds,
        DateTime at)
    {
        var ids = productIds.Distinct().ToArray();
        var prices = await AsyncExecuter.ToListAsync((await _prices.GetQueryableAsync()).Where(x => ids.Contains(x.ProductId)));
        return ids.ToDictionary(
            id => id,
            id =>
            {
                var price = prices.Where(x => x.ProductId == id && x.EffectivePeriod.Contains(at))
                    .OrderByDescending(x => x.EffectivePeriod.EffectiveFrom).FirstOrDefault();
                return (price?.Id, price?.Price.Amount);
            });
    }

    private async Task EnsureModificationAllowedAsync(SalesOrder order)
    {
        if (order.Status != SalesOrderStatus.Confirmed)
        {
            throw new BusinessException(VPureLuxDomainErrorCodes.SalesRevisionNotAllowed);
        }
        var assetQuery = await _assets.GetQueryableAsync();
        if (await AsyncExecuter.AnyAsync(assetQuery.Where(x => x.SalesOrderId == order.Id && x.InstalledAt.HasValue)))
        {
            throw new BusinessException(VPureLuxDomainErrorCodes.SalesInstallationLocksModification);
        }
    }

    private async Task CancelPendingAssetsAsync(Guid salesOrderId, string reason)
    {
        var query = (await _assets.GetQueryableAsync()).Where(x => x.SalesOrderId == salesOrderId && !x.InstalledAt.HasValue);
        var assets = await AsyncExecuter.ToListAsync(query);
        foreach (var asset in assets)
        {
            asset.CancelBeforeInstallation(reason);
        }
        if (assets.Count > 0) await _assets.UpdateManyAsync(assets);
    }

    private async Task EnsureOverridePermissionAsync(decimal? suggested, decimal actual)
    {
        if (suggested.HasValue && SalesOrderRevision.RoundMoney(suggested.Value) != SalesOrderRevision.RoundMoney(actual))
        {
            await AuthorizationService.CheckAsync(VPureLuxPermissions.Sales.OverridePrice);
        }
    }

    private static List<SalesOrderBomSnapshotData> BuildBomSnapshots(
        BomVersion bom,
        IReadOnlyDictionary<Guid, Component> components,
        decimal quantity) => bom.OrderedItems.Select(item =>
        {
            var component = components[item.ComponentId];
            return new SalesOrderBomSnapshotData(
                component.Id, component.Code, component.Name, component.Unit,
                item.Quantity, item.Quantity * quantity);
        }).ToList();

    private async Task<SalesOrder> GetOrderAsync(Guid id) =>
        await _orders.FindAsync(id, includeDetails: true)
        ?? throw new BusinessException(VPureLuxDomainErrorCodes.SalesOrderNotFound);

    private static void EnsureActiveRevision(SalesOrderRevision revision, SalesOrder order)
    {
        if (revision.SalesOrderId != order.Id || revision.Status != SalesOrderRevisionStatus.Draft)
        {
            throw new BusinessException(VPureLuxDomainErrorCodes.SalesRevisionNotAllowed);
        }
    }

    private async Task<Guid> ReadRevisionOrderIdAsync(Guid revisionId)
    {
        using var uow = _unitOfWorkManager.Begin(requiresNew: true, isTransactional: false);
        var id = (await _revisions.GetAsync(revisionId)).SalesOrderId;
        await uow.CompleteAsync();
        return id;
    }

    private async Task<Guid> ReadCancellationOrderIdAsync(Guid cancellationId)
    {
        using var uow = _unitOfWorkManager.Begin(requiresNew: true, isTransactional: false);
        var id = (await _cancellations.GetAsync(cancellationId)).SalesOrderId;
        await uow.CompleteAsync();
        return id;
    }

    private SalesOrderRefund CreateRefund(Guid orderId, Guid? revisionId, Guid? cancellationId, RecordSalesOrderRefundDto input) =>
        new(GuidGenerator.Create(), orderId, revisionId, cancellationId, input.Amount, input.RefundedAt,
            input.PaymentMethod, input.ReferenceNo, input.Reason, input.IdempotencyKey);

    private static SalesOrderRevisionAllocation ToRevisionAllocation(AllocationFact x) =>
        new(Guid.NewGuid(), x.StockItemId, x.InventoryLotId, x.Quantity, x.UnitCost);

    private static string Hash(string value) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));

    private static SalesOrderRevisionDto ToDto(SalesOrderRevision revision) => new()
    {
        Id = revision.Id,
        SalesOrderId = revision.SalesOrderId,
        RevisionNo = revision.RevisionNo,
        Status = revision.Status,
        Reason = revision.Reason,
        CustomerId = revision.CustomerIdSnapshot,
        BeforeTotal = revision.BeforeTotal,
        AppliedTotal = revision.AppliedTotal,
        RefundDue = revision.RefundDue,
        AppliedAt = revision.AppliedAt,
        Lines = revision.Lines.Where(x => !x.IsRemoved || x.SourceSalesOrderLineId.HasValue)
            .OrderBy(x => x.LineNo).Select(x => new SalesOrderRevisionLineDto
            {
                Id = x.Id,
                SourceSalesOrderLineId = x.SourceSalesOrderLineId,
                EffectiveSalesOrderLineId = x.EffectiveSalesOrderLineId,
                LineNo = x.LineNo,
                ProductId = x.ProductId,
                Quantity = x.Quantity,
                ActualSellingPrice = x.ActualSellingPrice,
                IsRemoved = x.IsRemoved,
                RequiresReturnConfirmation = x.RequiresReturnConfirmation,
                ReturnConfirmedAt = x.ReturnConfirmedAt,
                IssueInventoryTransactionId = x.IssueInventoryTransactionId,
                ReversalInventoryTransactionId = x.ReversalInventoryTransactionId,
                AppliedCostAmount = x.AppliedCostAmount
            }).ToList()
    };

    private static SalesOrderCancellationDto ToDto(SalesOrderCancellation cancellation) => new()
    {
        Id = cancellation.Id,
        SalesOrderId = cancellation.SalesOrderId,
        ReasonGroup = cancellation.ReasonGroup,
        Reason = cancellation.Reason,
        EffectiveAt = cancellation.EffectiveAt,
        StockStatus = cancellation.StockStatus,
        PaymentStatus = cancellation.PaymentStatus,
        RefundDue = cancellation.RefundDue,
        RefundedAmount = cancellation.RefundedAmount,
        StockReversalTransactionId = cancellation.StockReversalTransactionId,
        ClosedAt = cancellation.ClosedAt
    };

    private static SalesOrderRefundDto ToDto(SalesOrderRefund refund) => new()
    {
        Id = refund.Id,
        SalesOrderId = refund.SalesOrderId,
        SalesOrderRevisionId = refund.SalesOrderRevisionId,
        SalesOrderCancellationId = refund.SalesOrderCancellationId,
        Amount = refund.Amount,
        RefundedAt = refund.RefundedAt,
        PaymentMethod = refund.PaymentMethod,
        ReferenceNo = refund.ReferenceNo,
        Reason = refund.Reason
    };

    private sealed record AllocationFact(Guid StockItemId, Guid InventoryLotId, decimal Quantity, decimal UnitCost);

    private sealed record ApplyContext(
        IReadOnlyDictionary<Guid, Product> ProductById,
        IReadOnlyDictionary<Guid, BomVersion> BomById,
        IReadOnlyDictionary<Guid, Component> ComponentById,
        IReadOnlyDictionary<Guid, StockItem> StockItemByComponentId,
        IReadOnlyList<InventoryLot> Lots,
        IReadOnlyDictionary<Guid, InventoryLot> LotById,
        IReadOnlyList<InventoryTransaction> IssueTransactions,
        IReadOnlyDictionary<Guid, HashSet<Guid>> IssueTransactionIdsByLineId,
        IReadOnlyDictionary<Guid, List<SalesOrderRevisionAllocation>> PriorReversedAllocationsByLineId);
}
