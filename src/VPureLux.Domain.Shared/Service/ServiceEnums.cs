namespace VPureLux.Service;

public enum ServiceOrderStatus : byte
{
    Draft = 1,
    Confirmed = 2,
    InProgress = 3,
    Completed = 4,
    Cancelled = 5
}

public enum ServiceOrderLineType : byte
{
    Material = 1,
    Labor = 2
}

public enum ServiceWorkStatus : byte
{
    Active = 1,
    Inactive = 2
}

public enum ServicePaymentStatus : byte
{
    Posted = 1,
    Voided = 2
}

public enum ServiceReceivableStatus : byte
{
    Unpaid = 1,
    PartiallyPaid = 2,
    Paid = 3,
    Overpaid = 4
}
