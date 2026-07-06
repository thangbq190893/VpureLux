namespace VPureLux.Sales;

public sealed record SalesOrderPaymentSummary(
    decimal TotalAmount,
    decimal PaidAmount,
    decimal RemainingAmount,
    SalesOrderReceivableStatus PaymentStatus)
{
    public static SalesOrderPaymentSummary From(decimal totalAmount, decimal paidAmount)
    {
        var remaining = totalAmount - paidAmount;
        var status = paidAmount <= 0
            ? SalesOrderReceivableStatus.Unpaid
            : paidAmount < totalAmount
                ? SalesOrderReceivableStatus.PartiallyPaid
                : paidAmount == totalAmount
                    ? SalesOrderReceivableStatus.Paid
                    : SalesOrderReceivableStatus.Overpaid;

        return new SalesOrderPaymentSummary(totalAmount, paidAmount, remaining, status);
    }
}
