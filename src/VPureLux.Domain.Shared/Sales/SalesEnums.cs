namespace VPureLux.Sales;

public enum SalesOrderStatus : byte
{
    Draft = 1,
    Confirmed = 2,
    Cancelled = 3
}

public enum SalesOrderLineType : byte
{
    Product = 1
}
