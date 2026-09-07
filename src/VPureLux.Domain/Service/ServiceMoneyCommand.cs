using System;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using VPureLux.Sales;
using Volo.Abp;

namespace VPureLux.Service;

public sealed class ServiceMoneyCommand
{
    public Guid OrderId { get; }
    public decimal Amount { get; }
    public DateTime OccurredAt { get; }
    public SalesPaymentMethod Method { get; }
    public string Key { get; }
    public string Reference { get; }
    public string? Note { get; }
    public string Hash { get; }
    public bool IsRefund { get; }

    public ServiceMoneyCommand(Guid orderId, decimal? amount, DateTimeOffset occurredAt,
        SalesPaymentMethod method, string key, string? reference, string? note, bool refund = false)
    {
        if (orderId == Guid.Empty || !amount.HasValue || amount <= 0 || amount > 9999999999999999.99m ||
            decimal.Round(amount.Value, 2) != amount.Value || occurredAt == default || !Enum.IsDefined(method))
            throw new BusinessException(ServiceErrorCodes.InvalidMoney);
        OrderId = orderId;
        IsRefund = refund;
        Amount = amount.Value;
        OccurredAt = occurredAt.UtcDateTime;
        Method = method;
        Key = Required(key, ServiceConsts.MaxIdempotencyKeyLength);
        Reference = Optional(reference, ServiceConsts.MaxReferenceNoLength) ?? string.Empty;
        Note = Optional(note, ServiceConsts.MaxNoteLength);
        if (refund && Note == null) throw new BusinessException(ServiceErrorCodes.MoneyReasonRequired);
        // Length-delimited JSON avoids delimiter collisions in free-text business facts.
        Hash = Fingerprint(refund ? "service-refund-v1" : "service-payment-v1", orderId.ToString("N"),
            Amount.ToString("0.00", CultureInfo.InvariantCulture), OccurredAt.Ticks.ToString(CultureInfo.InvariantCulture),
            ((byte)method).ToString(CultureInfo.InvariantCulture), Reference, Note ?? string.Empty);
    }

    internal static string Fingerprint(params string[] facts) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(facts))));

    internal static string Required(string? value, int max) => Optional(value, max)
        ?? throw new BusinessException(ServiceErrorCodes.MoneyReasonRequired);

    private static string? Optional(string? value, int max)
    {
        var normalized = value?.Trim();
        if (normalized?.Length > max) throw new BusinessException(ServiceErrorCodes.InvalidMoney);
        return string.IsNullOrEmpty(normalized) ? null : normalized;
    }
}

public sealed class ServiceVoidCommand
{
    public Guid PaymentId { get; }
    public string Key { get; }
    public string Reason { get; }
    public string Hash { get; }

    public ServiceVoidCommand(Guid paymentId, string key, string reason)
    {
        if (paymentId == Guid.Empty) throw new BusinessException(ServiceErrorCodes.InvalidMoney);
        PaymentId = paymentId;
        Key = ServiceMoneyCommand.Required(key, ServiceConsts.MaxIdempotencyKeyLength);
        Reason = ServiceMoneyCommand.Required(reason, ServiceConsts.MaxNoteLength);
        Hash = ServiceMoneyCommand.Fingerprint("service-void-v1", paymentId.ToString("N"), Reason);
    }
}
