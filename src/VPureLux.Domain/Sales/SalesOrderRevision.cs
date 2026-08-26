using System;
using System.Collections.Generic;
using System.Linq;
using VPureLux.Sales.Events;
using Volo.Abp;
using Volo.Abp.Domain.Entities.Auditing;

namespace VPureLux.Sales;

public class SalesOrderRevision : FullAuditedAggregateRoot<Guid>
{
    private readonly List<SalesOrderRevisionLine> _lines = new();

    public Guid SalesOrderId { get; private set; }
    public int RevisionNo { get; private set; }
    public SalesOrderRevisionStatus Status { get; private set; }
    public string Reason { get; private set; } = string.Empty;
    public Guid CustomerIdSnapshot { get; private set; }
    public decimal BeforeTotal { get; private set; }
    public decimal? AppliedTotal { get; private set; }
    public decimal RefundDue { get; private set; }
    public string? ApplyIdempotencyKey { get; private set; }
    public Guid? AppliedBy { get; private set; }
    public DateTime? AppliedAt { get; private set; }
    public Guid? CancelledBy { get; private set; }
    public DateTime? CancelledAt { get; private set; }
    public byte[] RowVersion { get; private set; } = Array.Empty<byte>();
    public IReadOnlyCollection<SalesOrderRevisionLine> Lines => _lines.AsReadOnly();

    protected SalesOrderRevision() { }

    public SalesOrderRevision(
        Guid id,
        Guid salesOrderId,
        int revisionNo,
        string reason,
        Guid customerId,
        decimal beforeTotal,
        IEnumerable<SalesOrderLine> effectiveLines) : base(id)
    {
        if (salesOrderId == Guid.Empty || revisionNo <= 0 || customerId == Guid.Empty)
        {
            throw new BusinessException(VPureLuxDomainErrorCodes.ValidationFailed);
        }

        SalesOrderId = salesOrderId;
        RevisionNo = revisionNo;
        Reason = NormalizeReason(reason);
        CustomerIdSnapshot = customerId;
        BeforeTotal = RoundMoney(beforeTotal);
        Status = SalesOrderRevisionStatus.Draft;
        foreach (var line in effectiveLines.OrderBy(x => x.LineNo))
        {
            _lines.Add(SalesOrderRevisionLine.FromEffective(Guid.NewGuid(), line));
        }
        AddLocalEvent(new SalesOrderRevisionOpenedEvent(Id, SalesOrderId, RevisionNo, Reason));
    }

    public SalesOrderRevisionLine AddLine(
        Guid id,
        Guid productId,
        Guid bomVersionId,
        decimal quantity,
        Guid? suggestedPriceVersionId,
        decimal? suggestedPrice,
        decimal actualSellingPrice,
        string? overrideReason)
    {
        EnsureDraft();
        var lineNo = _lines.Where(x => !x.IsRemoved).Select(x => x.LineNo).DefaultIfEmpty().Max() + 1;
        var line = SalesOrderRevisionLine.CreateAdded(
            id, lineNo, productId, bomVersionId, quantity,
            suggestedPriceVersionId, suggestedPrice, actualSellingPrice, overrideReason);
        _lines.Add(line);
        return line;
    }

    public void UpdateLine(
        Guid lineId,
        Guid productId,
        Guid bomVersionId,
        decimal quantity,
        Guid? suggestedPriceVersionId,
        decimal? suggestedPrice,
        decimal actualSellingPrice,
        string? overrideReason)
    {
        EnsureDraft();
        FindLine(lineId).Update(
            productId, bomVersionId, quantity, suggestedPriceVersionId,
            suggestedPrice, actualSellingPrice, overrideReason);
    }

    public void RemoveLine(Guid lineId)
    {
        EnsureDraft();
        var line = FindLine(lineId);
        if (line.SourceSalesOrderLineId.HasValue)
        {
            line.MarkRemoved();
        }
        else
        {
            _lines.Remove(line);
        }
        RenumberActiveLines();
    }

    public void ConfirmReturnedGoods(Guid lineId, Guid? actorId, DateTime confirmedAt, string reason)
    {
        EnsureDraft();
        FindLine(lineId).ConfirmReturnedGoods(actorId, confirmedAt, reason);
    }

    public void Apply(string idempotencyKey, Guid? actorId, DateTime appliedAt, decimal appliedTotal, decimal netPaid)
    {
        idempotencyKey = Check.NotNullOrWhiteSpace(
            idempotencyKey, nameof(idempotencyKey), SalesConsts.MaxIdempotencyKeyLength);
        if (Status == SalesOrderRevisionStatus.Applied)
        {
            if (ApplyIdempotencyKey == idempotencyKey)
            {
                return;
            }
            throw new BusinessException(VPureLuxDomainErrorCodes.SalesRevisionIdempotencyConflict);
        }
        EnsureDraft();

        ApplyIdempotencyKey = idempotencyKey;
        AppliedBy = actorId;
        AppliedAt = appliedAt;
        AppliedTotal = RoundMoney(appliedTotal);
        RefundDue = RoundMoney(Math.Max(netPaid - appliedTotal, 0));
        Status = SalesOrderRevisionStatus.Applied;
        AddLocalEvent(new SalesOrderRevisionAppliedEvent(
            Id, SalesOrderId, RevisionNo, Reason, AppliedTotal.Value, RefundDue));
    }

    public void Cancel(Guid? actorId, DateTime cancelledAt, string reason)
    {
        EnsureDraft();
        Reason = NormalizeReason(reason);
        CancelledBy = actorId;
        CancelledAt = cancelledAt;
        Status = SalesOrderRevisionStatus.Cancelled;
        AddLocalEvent(new SalesOrderRevisionCancelledEvent(Id, SalesOrderId, RevisionNo, Reason));
    }

    private SalesOrderRevisionLine FindLine(Guid id) =>
        _lines.SingleOrDefault(x => x.Id == id)
        ?? throw new BusinessException(VPureLuxDomainErrorCodes.EntityNotFound);

    private void RenumberActiveLines()
    {
        var lineNo = 1;
        foreach (var line in _lines.Where(x => !x.IsRemoved).OrderBy(x => x.LineNo))
        {
            line.Renumber(lineNo++);
        }
    }

    private void EnsureDraft()
    {
        if (Status != SalesOrderRevisionStatus.Draft)
        {
            throw new BusinessException(VPureLuxDomainErrorCodes.SalesRevisionNotAllowed);
        }
    }

    internal static string NormalizeReason(string reason) =>
        Check.NotNullOrWhiteSpace(reason, nameof(reason), SalesConsts.MaxReasonLength).Trim();

    public static decimal RoundMoney(decimal value) =>
        decimal.Round(value, SalesConsts.MoneyScale, MidpointRounding.AwayFromZero);
}
