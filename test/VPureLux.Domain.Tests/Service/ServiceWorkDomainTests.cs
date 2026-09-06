using System;
using Shouldly;
using Volo.Abp;
using Xunit;

namespace VPureLux.Service;

public class ServiceWorkDomainTests
{
    [Fact]
    public void Should_normalize_work_and_preserve_unknown_and_known_zero_cost()
    {
        var work = new ServiceWork(Guid.NewGuid(), " work-1 ", " Labor ", " Visit ", 10.125m, null, ServiceWorkStatus.Active);
        work.Code.ShouldBe("WORK-1");
        work.Name.ShouldBe("Labor");
        work.Unit.ShouldBe("Visit");
        work.DefaultPrice.ShouldBe(10.13m);
        work.StandardCost.ShouldBeNull();
        work.Update("Labor", "Visit", 12, 0, ServiceWorkStatus.Inactive, " explicit edit ");
        work.StandardCost.ShouldBe(0);
        work.Note.ShouldBe("explicit edit");
        work.Update("Labor", "Visit", 12, null, ServiceWorkStatus.Active, null);
        work.StandardCost.ShouldBeNull();
    }

    [Theory]
    [InlineData("", "Work", "Visit", 0, 0, 1)]
    [InlineData("A", " ", "Visit", 0, 0, 1)]
    [InlineData("A", "Work", " ", 0, 0, 1)]
    [InlineData("A", "Work", "Visit", -1, 0, 1)]
    [InlineData("A", "Work", "Visit", 0, -1, 1)]
    [InlineData("A", "Work", "Visit", 0, 0, 0)]
    public void Should_reject_invalid_work(string code, string name, string unit, int price, int cost, int status) =>
        Should.Throw<Exception>(() => new ServiceWork(Guid.NewGuid(), code, name, unit, price, cost, (ServiceWorkStatus)status));

    [Fact]
    public void Should_preserve_historical_enums_and_default_disabled_runtime()
    {
        ((byte)ServiceOrderStatus.Draft).ShouldBe((byte)1);
        ((byte)ServiceOrderStatus.Confirmed).ShouldBe((byte)2);
        ((byte)ServiceOrderStatus.InProgress).ShouldBe((byte)3);
        ((byte)ServiceOrderStatus.Completed).ShouldBe((byte)4);
        ((byte)ServiceOrderStatus.Cancelled).ShouldBe((byte)5);
        ((byte)ServiceOrderLineType.Material).ShouldBe((byte)1);
        ((byte)ServiceOrderLineType.Labor).ShouldBe((byte)2);
        ((byte)ServiceWorkStatus.Active).ShouldBe((byte)1);
        ((byte)ServiceWorkStatus.Inactive).ShouldBe((byte)2);
        ((byte)ServicePaymentStatus.Posted).ShouldBe((byte)1);
        ((byte)ServicePaymentStatus.Voided).ShouldBe((byte)2);
        new ServiceOptions().IsEnabled.ShouldBeFalse();
        typeof(ServiceOrder).GetMethod("Complete").ShouldBeNull();
        typeof(ServiceOrder).GetMethod("Confirm").ShouldBeNull();
        typeof(ServicePayment).GetMethod("Void").ShouldBeNull();
    }
}
