using System;
using System.Globalization;
using Shouldly;
using VPureLux.Sales;
using Volo.Abp;
using Xunit;

namespace VPureLux.Service;

public class ServiceMoneyTests
{
    [Theory]
    [InlineData(null)] [InlineData("0")] [InlineData("-1")] [InlineData("0.001")]
    [InlineData("10000000000000000")]
    public void Invalid_money_rejects_without_rounding_or_fabrication(string? text)
    {
        decimal? amount = text == null ? null : decimal.Parse(text, CultureInfo.InvariantCulture);
        Should.Throw<BusinessException>(() => new ServiceMoneyCommand(Guid.NewGuid(), amount,
            DateTimeOffset.UtcNow, SalesPaymentMethod.Cash, "key", null, null)).Code.ShouldBe(ServiceErrorCodes.InvalidMoney);
        Should.Throw<BusinessException>(() => new ServiceMoneyCommand(Guid.NewGuid(), amount,
            DateTimeOffset.UtcNow, SalesPaymentMethod.Cash, "key", null, "actual refund", true));
    }

    [Theory]
    [InlineData(ServiceOrderStatus.InProgress, 10, 5, 0, 5, 5, 0, 0, 0)]
    [InlineData(ServiceOrderStatus.Completed, 12, 5, 0, 0, 0, 7, 0, 12)]
    [InlineData(ServiceOrderStatus.Completed, 6, 8, 0, 0, 0, 0, 2, 6)]
    [InlineData(ServiceOrderStatus.Completed, 6, 8, 1, 0, 0, 0, 1, 6)]
    [InlineData(ServiceOrderStatus.Completed, 6, 8, 2, 0, 0, 0, 0, 6)]
    [InlineData(ServiceOrderStatus.Cancelled, 10, 5, 0, 0, 0, 0, 5, 0)]
    [InlineData(ServiceOrderStatus.Cancelled, 10, 5, 5, 0, 0, 0, 0, 0)]
    [InlineData(ServiceOrderStatus.Draft, 10, 12, 0, 12, 0, 0, 2, 0)]
    public void Projection_preserves_recognition_and_settlement(ServiceOrderStatus status, decimal actual,
        decimal posted, decimal refunded, decimal advance, decimal plannedRemaining, decimal receivable, decimal credit, decimal revenue)
    {
        var s = ServiceMoneySummary.From(new ServiceMoneyFacts { Status = status, PlannedTotal = 10,
            CompletedTotal = actual, GrossPosted = posted, GrossRefunded = refunded });
        s.AdvancePaid.ShouldBe(advance);
        s.PlannedRemaining.ShouldBe(plannedRemaining);
        s.Receivable.ShouldBe(receivable);
        s.CustomerCredit.ShouldBe(credit);
        s.RefundDue.ShouldBe(credit);
        s.Revenue.ShouldBe(revenue);
        s.NetPaid.ShouldBe(posted - refunded);
        if (status != ServiceOrderStatus.Completed) s.ActualTotal.ShouldBeNull();
    }

    [Fact]
    public void Void_is_immutable_and_retry_does_not_repost()
    {
        var cmd = Command();
        var p = new ServicePayment(Guid.NewGuid(), Guid.NewGuid(), cmd, Guid.NewGuid(), DateTime.UtcNow);
        Should.Throw<BusinessException>(() => new ServiceVoidCommand(p.Id, "v", "  "));
        var v = new ServiceVoidCommand(p.Id, "v", "wrong receipt");
        var at = DateTime.UtcNow;
        var actor = Guid.NewGuid();
        p.Void(v, actor, at);
        p.Void(v, Guid.NewGuid(), at.AddDays(1));
        p.EnsureReplay(cmd);
        p.Status.ShouldBe(ServicePaymentStatus.Voided);
        p.VoidedAt.ShouldBe(at);
        p.VoidedBy.ShouldBe(actor);
        Should.Throw<BusinessException>(() => p.Void(new(p.Id, "v", "different"), actor, at));
        Should.Throw<BusinessException>(() => p.Void(new(p.Id, "other", "wrong receipt"), actor, at));
        p.VoidReason.ShouldBe("wrong receipt");
    }

    [Fact]
    public void Hash_is_invariant_and_text_cannot_collide()
    {
        var before = CultureInfo.CurrentCulture;
        try
        {
            var id = Guid.NewGuid();
            var date = DateTimeOffset.UtcNow;
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("vi-VN");
            var a = new ServiceMoneyCommand(id, 1500000.50m, date, SalesPaymentMethod.Cash, "a", "x|y", "z");
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("en-US");
            new ServiceMoneyCommand(id, 1500000.5m, date.ToOffset(TimeSpan.FromHours(7)), SalesPaymentMethod.Cash,
                "b", "x|y", "z").Hash.ShouldBe(a.Hash);
            new ServiceMoneyCommand(id, 1500000.5m, date, SalesPaymentMethod.Cash, "a", "x", "y|z").Hash.ShouldNotBe(a.Hash);
            new ServiceMoneyCommand(id, 1500000.5m, date, SalesPaymentMethod.Cash, "a", "x|y", "z", true).Hash.ShouldNotBe(a.Hash);
        }
        finally { CultureInfo.CurrentCulture = before; }
    }

    [Fact]
    public void Void_cannot_make_net_cash_negative_and_legacy_errors_are_visible()
    {
        var s = ServiceMoneySummary.From(new() { Status = ServiceOrderStatus.Cancelled, GrossPosted = 5, GrossRefunded = 3 });
        Should.Throw<BusinessException>(() => s.EnsureVoidAllowed(5)).Code.ShouldBe(ServiceErrorCodes.VoidWouldInvalidateRefund);
        s.EnsureVoidAllowed(2);
        Should.Throw<BusinessException>(() => s.EnsurePaymentAllowed(1));
        Should.Throw<BusinessException>(() => s.EnsureRefundAllowed(3));
        var invalid = ServiceMoneySummary.From(new() { GrossPosted = 1, GrossRefunded = 2 });
        invalid.NetPaid.ShouldBe(0);
        invalid.HasInconsistentLedger.ShouldBeTrue();
        Should.Throw<BusinessException>(() => invalid.EnsureRefundAllowed(1));
    }

    private static ServiceMoneyCommand Command() => new(Guid.NewGuid(), 5,
        DateTimeOffset.UtcNow, SalesPaymentMethod.Cash, "key", null, null);
}
