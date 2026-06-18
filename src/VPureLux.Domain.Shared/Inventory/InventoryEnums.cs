namespace VPureLux.Inventory;

public enum StockItemType : byte
{
    Component = 1,
    Product = 2
}

public enum InventoryEntityStatus : byte
{
    Active = 1,
    Inactive = 2
}

public enum InventoryLotStatus : byte
{
    Available = 1,
    Depleted = 2
}

public enum InventoryTransactionStatus : byte
{
    Draft = 1,
    Posted = 2
}

public enum InventoryTransactionType : byte
{
    PurchaseReceipt = 1,
    SalesIssue = 2,
    AssemblyIssue = 3,
    AdjustmentIncrease = 4,
    AdjustmentDecrease = 5
}

public enum InventoryMovementDirection : byte
{
    Increase = 1,
    Decrease = 2
}
