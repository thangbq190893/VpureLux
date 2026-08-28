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
    NotApplicable = 0,
    Unpaid = 1,
    PartiallyPaid = 2,
    Paid = 3,
    Overpaid = 4
}

public enum SalesOrderRevisionStatus : byte
{
    Draft = 1,
    Applied = 2,
    Cancelled = 3
}

public enum SalesOrderCancellationStockStatus : byte
{
    NotRequired = 1,
    PendingReturn = 2,
    Completed = 3,
    Exception = 4
}

public enum SalesOrderCancellationPaymentStatus : byte
{
    NotRequired = 1,
    PendingRefund = 2,
    Completed = 3
}

public enum SalesReturnTaskType : byte
{
    RevisionLine = 1,
    Cancellation = 2
}

public enum SalesRefundTaskType : byte
{
    Revision = 1,
    Cancellation = 2
}

public enum SalesRevisionImpactType : byte
{
    Unchanged = 1,
    Return = 2,
    Issue = 3
}
