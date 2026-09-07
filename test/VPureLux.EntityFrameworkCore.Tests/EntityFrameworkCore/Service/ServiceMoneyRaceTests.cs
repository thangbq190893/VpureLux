using System;
using System.Threading;
using System.Threading.Tasks;
using Shouldly;
using VPureLux.Service;
using Volo.Abp;
using Xunit;

namespace VPureLux.EntityFrameworkCore.Service;

public partial class ServiceOrderWorkflowTests
{
    private async Task<(Exception? First, Exception? Second)> MoneyRace(Guid id, Func<Task> first, Func<Task> second)
    {
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var waiting = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var testLock = GetRequiredService<ServiceTestLock>();
        var key = $"VPureLux:ServiceOrder:{id:N}";
        var count = 0;
        testLock.Acquired = async name =>
        {
            if (name == key && Interlocked.Increment(ref count) == 1)
            { entered.TrySetResult(); await release.Task.WaitAsync(TimeSpan.FromSeconds(20)); }
        };
        testLock.Waiting = name => { if (name == key) waiting.TrySetResult(); };
        static async Task<Exception?> Capture(Func<Task> action)
        {
            try { await action(); return null; } catch (Exception e) { return e; }
        }
        var a = Task.Run(() => Capture(first));
        await entered.Task.WaitAsync(TimeSpan.FromSeconds(20));
        var b = Task.Run(() => Capture(second));
        try { await waiting.Task.WaitAsync(TimeSpan.FromSeconds(20)); b.IsCompleted.ShouldBeFalse(); }
        finally { release.TrySetResult(); }
        try { return (await a, await b); }
        finally { testLock.Acquired = null; testLock.Waiting = null; }
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task Money_two_payments_serialize_cap_and_exact_retry(bool sameKey)
    {
        var f = await CreateFixtureAsync("S004-PAY-RACE");
        var o = await StartOrderAsync(f, Labor(f.Work.Id, 1, 5));
        var a = Pay(4); var b = Pay(4);
        if (sameKey) b.IdempotencyKey = a.IdempotencyKey;
        var result = await MoneyRace(o.Id, async () => await Money.AddPaymentAsync(o.Id, a), async () => await Money.AddPaymentAsync(o.Id, b));
        result.First.ShouldBeNull();
        if (sameKey) result.Second.ShouldBeNull();
        else result.Second.ShouldBeOfType<BusinessException>().Code.ShouldBe(ServiceErrorCodes.PaymentExceedsObligation);
        (await Money.GetSummaryAsync(o.Id)).NetPaid.ShouldBe(4);
        (await Money.GetListAsync(new() { ServiceOrderId = o.Id })).TotalCount.ShouldBe(1);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task Money_payment_vs_cancel_respects_authoritative_ordering(bool cancelFirst)
    {
        var f = await CreateFixtureAsync("S004-CANCEL-RACE");
        var o = await StartOrderAsync(f, Labor(f.Work.Id, 1, 5));
        Func<Task> pay = async () => await Money.AddPaymentAsync(o.Id, Pay(4));
        Func<Task> cancel = async () => await Orders.CancelAsync(o.Id, new() { ConcurrencyStamp = o.ConcurrencyStamp, Reason = "cancel" });
        var result = await MoneyRace(o.Id, cancelFirst ? cancel : pay, cancelFirst ? pay : cancel);
        result.First.ShouldBeNull();
        if (cancelFirst) result.Second.ShouldBeOfType<BusinessException>().Code.ShouldBe(ServiceErrorCodes.PaymentExceedsObligation);
        else result.Second.ShouldBeNull();
        var s = await Money.GetSummaryAsync(o.Id);
        s.Status.ShouldBe(ServiceOrderStatus.Cancelled); s.Revenue.ShouldBe(0);
        s.RefundDue.ShouldBe(cancelFirst ? 0 : 4);
        s.GrossRefunded.ShouldBe(0);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task Money_payment_vs_completion_uses_actual_obligation(bool completeFirst)
    {
        var f = await CreateFixtureAsync("S004-COMPLETE-RACE");
        var o = await StartOrderAsync(f, Labor(f.Work.Id, 5, 1));
        var completion = Completion(o); completion.Lines[0].ActualQuantity = 3;
        Func<Task> pay = async () => await Money.AddPaymentAsync(o.Id, Pay(4));
        Func<Task> complete = async () => await Orders.CompleteAsync(o.Id, completion);
        var result = await MoneyRace(o.Id, completeFirst ? complete : pay, completeFirst ? pay : complete);
        result.First.ShouldBeNull();
        if (completeFirst) result.Second.ShouldBeOfType<BusinessException>().Code.ShouldBe(ServiceErrorCodes.PaymentExceedsObligation);
        else result.Second.ShouldBeNull();
        var s = await Money.GetSummaryAsync(o.Id);
        s.Revenue.ShouldBe(3); s.NetPaid.ShouldBe(completeFirst ? 0 : 4);
        s.Receivable.ShouldBe(completeFirst ? 3 : 0); s.RefundDue.ShouldBe(completeFirst ? 0 : 1);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task Money_two_refunds_serialize_remaining_credit(bool sameKey)
    {
        var f = await CreateFixtureAsync("S004-REFUND-RACE");
        var o = await StartOrderAsync(f, Labor(f.Work.Id, 1, 5));
        await Money.AddPaymentAsync(o.Id, Pay(5));
        await Orders.CancelAsync(o.Id, new() { ConcurrencyStamp = o.ConcurrencyStamp, Reason = "cancel" });
        var a = RefundInput(4); var b = RefundInput(4); if (sameKey) b.IdempotencyKey = a.IdempotencyKey;
        var result = await MoneyRace(o.Id, async () => await Money.RefundAsync(o.Id, a), async () => await Money.RefundAsync(o.Id, b));
        result.First.ShouldBeNull();
        if (sameKey) result.Second.ShouldBeNull();
        else result.Second.ShouldBeOfType<BusinessException>().Code.ShouldBe(ServiceErrorCodes.RefundExceedsCredit);
        var s = await Money.GetSummaryAsync(o.Id); s.GrossRefunded.ShouldBe(4); s.RefundDue.ShouldBe(1);
        (await Money.GetRefundListAsync(new() { ServiceOrderId = o.Id })).TotalCount.ShouldBe(1);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task Money_void_vs_refund_cannot_make_net_cash_negative(bool voidFirst)
    {
        var f = await CreateFixtureAsync("S004-VOID-RACE");
        var o = await StartOrderAsync(f, Labor(f.Work.Id, 1, 5));
        var p = await Money.AddPaymentAsync(o.Id, Pay(5));
        await Orders.CancelAsync(o.Id, new() { ConcurrencyStamp = o.ConcurrencyStamp, Reason = "cancel" });
        Func<Task> refund = async () => await Money.RefundAsync(o.Id, RefundInput(3));
        Func<Task> voidPayment = async () => await Money.VoidPaymentAsync(o.Id, new() { PaymentId = p.Id,
            ConcurrencyStamp = p.ConcurrencyStamp, IdempotencyKey = "void", Reason = "wrong" });
        var result = await MoneyRace(o.Id, voidFirst ? voidPayment : refund, voidFirst ? refund : voidPayment);
        result.First.ShouldBeNull();
        result.Second.ShouldBeOfType<BusinessException>().Code.ShouldBe(voidFirst ? ServiceErrorCodes.RefundExceedsCredit : ServiceErrorCodes.VoidWouldInvalidateRefund);
        var s = await Money.GetSummaryAsync(o.Id); s.NetPaid.ShouldBe(voidFirst ? 0 : 2); s.HasInconsistentLedger.ShouldBeFalse();
    }

    [Fact]
    public async Task Money_refund_vs_new_payment_does_not_reopen_collection_room()
    {
        var f = await CreateFixtureAsync("S004-REFUND-PAY");
        var o = await StartOrderAsync(f, Labor(f.Work.Id, 5, 1));
        await Money.AddPaymentAsync(o.Id, Pay(5));
        var completion = Completion(o); completion.Lines[0].ActualQuantity = 3;
        await Orders.CompleteAsync(o.Id, completion);
        var result = await MoneyRace(o.Id, async () => await Money.RefundAsync(o.Id, RefundInput(2)), async () => await Money.AddPaymentAsync(o.Id, Pay(1)));
        result.First.ShouldBeNull();
        result.Second.ShouldBeOfType<BusinessException>().Code.ShouldBe(ServiceErrorCodes.PaymentExceedsObligation);
        var s = await Money.GetSummaryAsync(o.Id); s.NetPaid.ShouldBe(3); s.Receivable.ShouldBe(0); s.RefundDue.ShouldBe(0);
    }
}
