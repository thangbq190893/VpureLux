using System;
using System.Linq;
using Shouldly;
using Volo.Abp;
using Xunit;

namespace VPureLux.Inventory;

public class InventoryDomainTests
{
    [Fact]
    public void StockItem_Should_Enforce_Component_And_Product_Policies()
    {
        var component = new StockItem(Guid.NewGuid(), StockItemType.Component, Guid.NewGuid(), "C1", "Component", "Piece", true);
        var product = new StockItem(Guid.NewGuid(), StockItemType.Product, Guid.NewGuid(), "P1", "Product", "Unit", false);
        component.IsInventoryEnabled.ShouldBeTrue();
        product.IsInventoryEnabled.ShouldBeFalse();
        component.Deactivate();
        component.Status.ShouldBe(InventoryEntityStatus.Inactive);
    }

    [Fact]
    public void Warehouse_Code_Should_Be_Normalized_And_Immutable()
    {
        var warehouse = new Warehouse(Guid.NewGuid(), " wh-01 ", "Main", null, true);
        warehouse.Code.ShouldBe("WH-01");
        warehouse.UpdateInfo("Renamed", "Address", false);
        warehouse.Code.ShouldBe("WH-01");
    }

    [Fact]
    public void Lot_Should_Reject_Invalid_And_Excess_Allocation()
    {
        Should.Throw<BusinessException>(() => Lot(0));
        var lot = Lot(10);
        Should.Throw<BusinessException>(() => lot.Allocate(11)).Code.ShouldBe(VPureLuxDomainErrorCodes.InsufficientInventory);
        lot.Allocate(10);
        lot.AvailableQuantity.ShouldBe(0);
        lot.Status.ShouldBe(InventoryLotStatus.Depleted);
    }

    [Fact]
    public void Lot_No_Should_Have_No_Public_Mutation()
    {
        typeof(InventoryLot).GetMethods()
            .Where(x => x.DeclaringType == typeof(InventoryLot) && !x.IsSpecialName)
            .Select(x => x.Name)
            .ShouldBe(new[] { nameof(InventoryLot.Allocate) });
    }

    [Fact]
    public void Adjustment_Should_Require_Reason()
    {
        Should.Throw<BusinessException>(() => Transaction(InventoryTransactionType.AdjustmentIncrease, null))
            .Code.ShouldBe(VPureLuxDomainErrorCodes.AdjustmentReasonRequired);
    }

    [Fact]
    public void Posted_Transaction_Should_Be_Immutable()
    {
        var transaction = Transaction(InventoryTransactionType.PurchaseReceipt);
        transaction.AddReceiptLine(Guid.NewGuid(), Guid.NewGuid(), 1, "LOT-1", DateTime.UtcNow, 100);
        transaction.Post(DateTime.UtcNow);
        Should.Throw<BusinessException>(() => transaction.AddReceiptLine(Guid.NewGuid(), Guid.NewGuid(), 1, "LOT-2", DateTime.UtcNow, 100))
            .Code.ShouldBe(VPureLuxDomainErrorCodes.InventoryTransactionAlreadyPosted);
    }

    [Fact]
    public void Issue_Should_Require_Full_Allocation_Before_Posting()
    {
        var transaction = Transaction(InventoryTransactionType.SalesIssue);
        transaction.AddIssueLine(Guid.NewGuid(), Guid.NewGuid(), 2);
        Should.Throw<BusinessException>(() => transaction.Post(DateTime.UtcNow))
            .Code.ShouldBe(VPureLuxDomainErrorCodes.ValidationFailed);
    }

    private static InventoryLot Lot(decimal quantity) =>
        new(Guid.NewGuid(), "LOT-1", Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow, quantity, 30000);

    private static InventoryTransaction Transaction(InventoryTransactionType type, string? reason = null) =>
        new(Guid.NewGuid(), Guid.NewGuid(), type, Guid.NewGuid().ToString("N"), new string('A', 64), reason: reason);
}
