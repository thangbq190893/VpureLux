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

public enum SalesOrderPaymentStatus : byte
{
    Posted = 1,
    Voided = 2
}

public enum SalesPaymentMethod : byte
{
    Cash = 1,
    BankTransfer = 2,
    Card = 3,
    Other = 99
}

public enum SalesOrderReceivableStatus : byte
{
    Unpaid = 1,
    PartiallyPaid = 2,
    Paid = 3,
    Overpaid = 4
}
