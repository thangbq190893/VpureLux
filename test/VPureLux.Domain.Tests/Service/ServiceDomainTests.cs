using System;
using System.Linq;
using Shouldly;
using Volo.Abp;
using Xunit;

namespace VPureLux.Service;

public class ServiceDomainTests
{
    [Fact]
    public void Should_Complete_Performed_Lines_And_Calculate_Totals()
    {
        var order = CreateOrder();
        var material = order.AddLine(Guid.NewGuid(), ServiceOrderLineType.Material, Guid.NewGuid(), Guid.NewGuid(),
            "MAT-001", "Filter", "Piece", 2, 100_000, null);
        var labor = order.AddLine(Guid.NewGuid(), ServiceOrderLineType.Labor, Guid.NewGuid(), null,
            "WORK-001", "Installation", "Time", 1, 150_000, null);
        order.Confirm(DateTime.UtcNow);
        order.Start(DateTime.UtcNow);
        order.CompleteLine(material.Id, 1, 60_000);
        order.CompleteLine(labor.Id, 1, 0);
        var inventoryTransactionId = Guid.NewGuid();

        order.Complete("complete-key", DateTime.UtcNow, inventoryTransactionId);

        order.Status.ShouldBe(ServiceOrderStatus.Completed);
        order.InventoryTransactionId.ShouldBe(inventoryTransactionId);
        order.TotalRevenueAmount.ShouldBe(250_000);
        order.TotalCostAmount.ShouldBe(60_000);
        order.TotalProfitAmount.ShouldBe(190_000);
        material.ActualQuantity.ShouldBe(1);
    }

    [Fact]
    public void Should_Not_Count_Unperformed_Line()
    {
        var order = CreateOrder();
        var material = order.AddLine(Guid.NewGuid(), ServiceOrderLineType.Material, Guid.NewGuid(), null,
            "MAT-001", "Filter", "Piece", 2, 100_000, null);
        var labor = order.AddLine(Guid.NewGuid(), ServiceOrderLineType.Labor, Guid.NewGuid(), null,
            "WORK-001", "Inspection", "Time", 1, 50_000, null);
        order.Confirm(DateTime.UtcNow);
        order.CompleteLine(material.Id, 0, 0);
        order.CompleteLine(labor.Id, 1, 0);

        order.Complete("complete-key", DateTime.UtcNow, null);

        order.TotalRevenueAmount.ShouldBe(50_000);
        material.RevenueAmount.ShouldBe(0);
    }

    [Fact]
    public void Should_Enforce_State_And_Completion_Idempotency()
    {
        var order = CreateOrder();
        var line = order.AddLine(Guid.NewGuid(), ServiceOrderLineType.Labor, Guid.NewGuid(), null,
            "WORK-001", "Inspection", "Time", 1, 50_000, null);
        Should.Throw<BusinessException>(() => order.Start(DateTime.UtcNow))
            .Code.ShouldBe(VPureLuxDomainErrorCodes.ServiceOrderCannotBeModified);
        order.Confirm(DateTime.UtcNow);
        order.CompleteLine(line.Id, 1, 0);
        order.Complete("same-key", DateTime.UtcNow, null);
        order.Complete("same-key", DateTime.UtcNow, null);
        Should.Throw<BusinessException>(() => order.Complete("other-key", DateTime.UtcNow, null))
            .Code.ShouldBe(VPureLuxDomainErrorCodes.ServiceOrderCompletionConflict);
    }

    [Fact]
    public void Material_Quantity_Must_Be_Integer_And_Positive()
    {
        var order = CreateOrder();
        Should.Throw<BusinessException>(() => order.AddLine(Guid.NewGuid(), ServiceOrderLineType.Material,
            Guid.NewGuid(), null, "MAT-001", "Filter", "Piece", 0, 1, null));
        var line = order.AddLine(Guid.NewGuid(), ServiceOrderLineType.Material, Guid.NewGuid(), null,
            "MAT-001", "Filter", "Piece", 2, 1, null);
        line.PlannedQuantity.ShouldBeOfType<int>();
    }

    [Fact]
    public void Payment_void_should_be_idempotent()
    {
        var payment = new ServicePayment(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 100_000,
            DateTime.UtcNow, VPureLux.Sales.SalesPaymentMethod.Cash, "payment-key");

        payment.Void();
        payment.Void();

        payment.Status.ShouldBe(ServicePaymentStatus.Voided);
    }

    private static ServiceOrder CreateOrder() => new(
        Guid.NewGuid(), "SVC-202608240001", Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow,
        "CUS-001", "Customer", "M-001", "Machine", null, null, null, null);
}
