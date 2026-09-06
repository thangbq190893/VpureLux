using System;
using System.Collections.Generic;
using System.Linq;
using Volo.Abp;
using Volo.Abp.Domain.Entities.Auditing;

namespace VPureLux.Service;

public class ServiceOrder : FullAuditedAggregateRoot<Guid>
{
    private readonly List<ServiceOrderLine> _lines = [];

    public string OrderNo { get; private set; } = string.Empty;
    public Guid CustomerId { get; private set; }
    public Guid CustomerAssetId { get; private set; }
    public Guid WarehouseId { get; private set; }
    public DateTime OrderDate { get; private set; }
    public DateTime? ScheduledAt { get; private set; }
    public Guid? TechnicianUserId { get; private set; }
    public ServiceOrderStatus Status { get; private set; }
    public string Currency { get; private set; } = ServiceConsts.Currency;
    public string CustomerCodeSnapshot { get; private set; } = string.Empty;
    public string CustomerNameSnapshot { get; private set; } = string.Empty;
    public string AssetNoSnapshot { get; private set; } = string.Empty;
    public string AssetNameSnapshot { get; private set; } = string.Empty;
    public string? ServiceAddress { get; private set; }
    public string? Note { get; private set; }
    public DateTime? ConfirmedAt { get; private set; }
    public DateTime? StartedAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }
    public DateTime? CancelledAt { get; private set; }
    public string? CancellationReason { get; private set; }
    public string? CompletionIdempotencyKey { get; private set; }
    public string? CompletionCommandHash { get; private set; }
    public decimal? ActualCostAmount { get; private set; }
    public decimal? ActualProfitAmount { get; private set; }
    public Guid? InventoryTransactionId { get; private set; }
    public decimal TotalRevenueAmount { get; private set; }
    public decimal TotalCostAmount { get; private set; }
    public decimal TotalProfitAmount { get; private set; }
    public byte[] RowVersion { get; private set; } = [];
    public IReadOnlyCollection<ServiceOrderLine> Lines => _lines.AsReadOnly();

    protected ServiceOrder()
    {
    }

    public ServiceOrder(
        Guid id,
        string orderNo,
        Guid customerId,
        Guid customerAssetId,
        Guid warehouseId,
        DateTime orderDate,
        string customerCode,
        string customerName,
        string assetNo,
        string assetName,
        DateTime? scheduledAt,
        Guid? technicianUserId,
        string? serviceAddress,
        string? note) : base(id)
    {
        OrderNo = Check.NotNullOrWhiteSpace(orderNo, nameof(orderNo), ServiceConsts.MaxOrderNoLength).Trim();
        CustomerId = Check.NotDefaultOrNull<Guid>(customerId, nameof(customerId));
        CustomerAssetId = Check.NotDefaultOrNull<Guid>(customerAssetId, nameof(customerAssetId));
        WarehouseId = Check.NotDefaultOrNull<Guid>(warehouseId, nameof(warehouseId));
        CustomerCodeSnapshot = Check.NotNullOrWhiteSpace(customerCode, nameof(customerCode), ServiceConsts.MaxCodeLength).Trim();
        CustomerNameSnapshot = Check.NotNullOrWhiteSpace(customerName, nameof(customerName), ServiceConsts.MaxNameLength).Trim();
        AssetNoSnapshot = Check.NotNullOrWhiteSpace(assetNo, nameof(assetNo), ServiceConsts.MaxAssetNoLength).Trim();
        AssetNameSnapshot = Check.NotNullOrWhiteSpace(assetName, nameof(assetName), ServiceConsts.MaxNameLength).Trim();
        Status = ServiceOrderStatus.Draft;
        UpdateDraft(orderDate, scheduledAt, technicianUserId, serviceAddress, note);
    }

    public void UpdateDraft(DateTime orderDate, DateTime? scheduledAt, Guid? technicianUserId, string? serviceAddress, string? note)
    {
        EnsureDraft();
        OrderDate = orderDate;
        ScheduledAt = scheduledAt;
        TechnicianUserId = technicianUserId;
        ServiceAddress = NormalizeOptional(serviceAddress, nameof(serviceAddress), ServiceConsts.MaxAddressLength);
        Note = NormalizeOptional(note, nameof(note), ServiceConsts.MaxNoteLength);
    }

    public ServiceOrderLine AddLine(
        Guid lineId, ServiceOrderLineType lineType, Guid catalogItemId, Guid? customerAssetComponentId,
        string itemCode, string itemName, string unit, int quantity, decimal unitPrice,
        decimal? standardCostSnapshot, string? note)
    {
        EnsureDraft();
        var line = new ServiceOrderLine(
            lineId, _lines.Count + 1, lineType, catalogItemId, customerAssetComponentId,
            itemCode, itemName, unit, quantity, unitPrice, standardCostSnapshot, note);
        _lines.Add(line);
        return line;
    }

    public void UpdateLine(Guid lineId, Guid? customerAssetComponentId, int quantity, decimal unitPrice, string? note)
    {
        EnsureDraft();
        FindLine(lineId).UpdateDraft(customerAssetComponentId, quantity, unitPrice, note);
    }

    public void ReplaceLineSelection(
        Guid lineId, ServiceOrderLineType lineType, Guid catalogItemId, Guid? customerAssetComponentId,
        string itemCode, string itemName, string unit, int quantity, decimal unitPrice,
        decimal? standardCostSnapshot, string? note)
    {
        EnsureDraft();
        FindLine(lineId).ReplaceSelection(
            lineType, catalogItemId, customerAssetComponentId, itemCode, itemName,
            unit, quantity, unitPrice, standardCostSnapshot, note);
    }

    public void RemoveLine(Guid lineId)
    {
        EnsureDraft();
        _lines.Remove(FindLine(lineId));
        for (var index = 0; index < _lines.Count; index++)
        {
            _lines[index].Renumber(index + 1);
        }
    }

    public void Confirm(DateTime confirmedAt)
    {
        EnsureDraft();
        if (_lines.Count == 0)
        {
            throw InvalidState("Confirm", "AtLeastOneLineRequired");
        }

        Status = ServiceOrderStatus.Confirmed;
        ConfirmedAt = confirmedAt;
    }

    public void Start(DateTime startedAt)
    {
        if (Status != ServiceOrderStatus.Confirmed)
        {
            throw InvalidState("Start");
        }

        Status = ServiceOrderStatus.InProgress;
        StartedAt = startedAt;
    }

    public void Cancel(DateTime cancelledAt, string reason)
    {
        if (Status is ServiceOrderStatus.Completed or ServiceOrderStatus.Cancelled)
        {
            throw InvalidState("Cancel");
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new BusinessException(ServiceErrorCodes.CancellationReasonRequired)
                .WithData("OrderNo", OrderNo)
                .WithData("Status", Status);
        }

        CancellationReason = Check.Length(reason.Trim(), nameof(reason), ServiceConsts.MaxNoteLength);
        CancelledAt = cancelledAt;
        Status = ServiceOrderStatus.Cancelled;
    }

    public bool IsCompletionReplay(ServiceCompletionCommand command)
    {
        if (command.OrderId != Id)
            throw new BusinessException(ServiceErrorCodes.InvalidCompletion);
        if (CompletionIdempotencyKey != command.Key) return false;
        if (Status != ServiceOrderStatus.Completed || CompletionCommandHash != command.Hash)
            throw new BusinessException(ServiceErrorCodes.CompletionConflict).WithData("OrderNo", OrderNo);
        return true;
    }

    public void ValidateCompletion(ServiceCompletionCommand command)
    {
        if (Status != ServiceOrderStatus.InProgress) throw InvalidState("Complete");
        if (command.OrderId != Id || command.Lines.Count != _lines.Count ||
            command.Lines.Any(actual => !_lines.Any(line => line.Id == actual.LineId &&
                actual.ActualQuantity <= line.PlannedQuantity)))
            throw new BusinessException(ServiceErrorCodes.InvalidCompletion).WithData("OrderNo", OrderNo);
    }

    public void Complete(ServiceCompletionCommand command, Guid? inventoryTransactionId,
        IReadOnlyDictionary<Guid, (decimal Cost, Guid InventoryLineId)> materialCosts)
    {
        ValidateCompletion(command);
        var quantities = command.Lines.ToDictionary(x => x.LineId, x => x.ActualQuantity);
        var performed = _lines.Where(x => x.LineType == ServiceOrderLineType.Material && quantities[x.Id] > 0).ToList();
        if (performed.Count != materialCosts.Count || (performed.Count > 0) != inventoryTransactionId.HasValue ||
            performed.Any(x => !materialCosts.TryGetValue(x.Id, out var cost) || cost.Cost < 0 || cost.InventoryLineId == Guid.Empty))
            throw new BusinessException(ServiceErrorCodes.InvalidCompletion);
        foreach (var line in _lines)
        {
            var hasCost = materialCosts.TryGetValue(line.Id, out var cost);
            line.RecordCompletion(quantities[line.Id], hasCost ? cost.Cost : null, hasCost ? cost.InventoryLineId : null);
        }
        TotalRevenueAmount = _lines.Sum(x => x.RevenueAmount);
        TotalCostAmount = _lines.Sum(x => x.ActualCostAmount ?? 0);
        TotalProfitAmount = TotalRevenueAmount - TotalCostAmount;
        ActualCostAmount = _lines.Any(x => x.ActualQuantity > 0 && !x.ActualCostAmount.HasValue)
            ? null : TotalCostAmount;
        ActualProfitAmount = ActualCostAmount.HasValue ? TotalRevenueAmount - ActualCostAmount : null;
        InventoryTransactionId = inventoryTransactionId;
        CompletionIdempotencyKey = command.Key;
        CompletionCommandHash = command.Hash;
        CompletedAt = command.CompletedAt;
        Status = ServiceOrderStatus.Completed;
    }

    private ServiceOrderLine FindLine(Guid lineId) =>
        _lines.SingleOrDefault(line => line.Id == lineId)
        ?? throw new BusinessException(ServiceErrorCodes.InvalidLine)
            .WithData("OrderNo", OrderNo)
            .WithData("LineId", lineId);

    private void EnsureDraft()
    {
        if (Status != ServiceOrderStatus.Draft)
        {
            throw InvalidState("Edit");
        }
    }

    private BusinessException InvalidState(string action, string? reason = null) =>
        new BusinessException(ServiceErrorCodes.OrderCannotBeModified)
            .WithData("OrderNo", OrderNo)
            .WithData("Status", Status)
            .WithData("Action", action)
            .WithData("Reason", reason ?? "InvalidState");

    private static string? NormalizeOptional(string? value, string name, int maxLength) =>
        string.IsNullOrWhiteSpace(value) ? null : Check.Length(value.Trim(), name, maxLength);
}
