using System;
using Volo.Abp;
using Volo.Abp.Domain.Entities.Auditing;

namespace VPureLux.OperatingCosts;

public class OperatingCostEntry : FullAuditedAggregateRoot<Guid>
{
    public DateTime EntryDate { get; private set; }
    public OperatingCostDirection Direction { get; private set; }
    public Guid CategoryId { get; private set; }
    public string CategoryNameSnapshot { get; private set; } = string.Empty;
    public decimal Amount { get; private set; }
    public OperatingCostPaymentStatus PaymentStatus { get; private set; }
    public DateTime? DueDate { get; private set; }
    public DateTime? PaymentDate { get; private set; }
    public string? Counterparty { get; private set; }
    public string? ReferenceNo { get; private set; }
    public string Description { get; private set; } = string.Empty;
    public string? Note { get; private set; }

    protected OperatingCostEntry()
    {
    }

    internal OperatingCostEntry(
        Guid id,
        DateTime entryDate,
        OperatingCostDirection direction,
        Guid categoryId,
        string categoryNameSnapshot,
        decimal amount,
        OperatingCostPaymentStatus paymentStatus,
        DateTime? dueDate,
        DateTime? paymentDate,
        string? counterparty,
        string? referenceNo,
        string description,
        string? note)
        : base(id)
    {
        SetInfo(
            entryDate,
            direction,
            categoryId,
            categoryNameSnapshot,
            amount,
            paymentStatus,
            dueDate,
            paymentDate,
            counterparty,
            referenceNo,
            description,
            note);
    }

    public void UpdateInfo(
        DateTime entryDate,
        OperatingCostDirection direction,
        Guid categoryId,
        string categoryNameSnapshot,
        decimal amount,
        OperatingCostPaymentStatus paymentStatus,
        DateTime? dueDate,
        DateTime? paymentDate,
        string? counterparty,
        string? referenceNo,
        string description,
        string? note)
    {
        SetInfo(
            entryDate,
            direction,
            categoryId,
            categoryNameSnapshot,
            amount,
            paymentStatus,
            dueDate,
            paymentDate,
            counterparty,
            referenceNo,
            description,
            note);
    }

    private void SetInfo(
        DateTime entryDate,
        OperatingCostDirection direction,
        Guid categoryId,
        string categoryNameSnapshot,
        decimal amount,
        OperatingCostPaymentStatus paymentStatus,
        DateTime? dueDate,
        DateTime? paymentDate,
        string? counterparty,
        string? referenceNo,
        string description,
        string? note)
    {
        if (!Enum.IsDefined(direction))
        {
            throw new BusinessException(VPureLuxDomainErrorCodes.ValidationFailed)
                .WithData(nameof(direction), direction);
        }

        if (!Enum.IsDefined(paymentStatus))
        {
            throw new BusinessException(VPureLuxDomainErrorCodes.ValidationFailed)
                .WithData(nameof(paymentStatus), paymentStatus);
        }

        if (amount <= 0)
        {
            throw new BusinessException(VPureLuxDomainErrorCodes.OperatingCostAmountMustBeGreaterThanZero)
                .WithData(nameof(amount), amount);
        }

        if (paymentStatus == OperatingCostPaymentStatus.Paid && !paymentDate.HasValue)
        {
            paymentDate = entryDate;
        }

        if (paymentStatus == OperatingCostPaymentStatus.Unpaid)
        {
            paymentDate = null;
        }

        EntryDate = entryDate.Date;
        Direction = direction;
        CategoryId = categoryId;
        CategoryNameSnapshot = Check.NotNullOrWhiteSpace(
            categoryNameSnapshot,
            nameof(categoryNameSnapshot),
            OperatingCostConsts.MaxCategoryNameLength).Trim();
        Amount = amount;
        PaymentStatus = paymentStatus;
        DueDate = dueDate?.Date;
        PaymentDate = paymentDate?.Date;
        Counterparty = NormalizeOptional(counterparty, nameof(counterparty), OperatingCostConsts.MaxCounterpartyLength);
        ReferenceNo = NormalizeOptional(referenceNo, nameof(referenceNo), OperatingCostConsts.MaxReferenceNoLength);
        Description = Check.NotNullOrWhiteSpace(
            description,
            nameof(description),
            OperatingCostConsts.MaxDescriptionLength).Trim();
        Note = NormalizeOptional(note, nameof(note), OperatingCostConsts.MaxNoteLength);
    }

    private static string? NormalizeOptional(string? value, string parameterName, int maxLength)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : Check.Length(value.Trim(), parameterName, maxLength);
    }
}
