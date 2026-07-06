using System.Linq;
using Volo.Abp.DependencyInjection;

namespace VPureLux.Sales;

public class SalesApplicationMapper : ITransientDependency
{
    public SalesOrderDto ToDto(
        SalesOrder order,
        bool includeCost,
        bool includeProfit,
        SalesOrderPaymentSummary? paymentSummary = null) => new()
    {
        Id = order.Id,
        OrderNo = order.OrderNo,
        CustomerId = order.CustomerId,
        WarehouseId = order.WarehouseId,
        OrderDate = order.OrderDate,
        Status = order.Status,
        Currency = order.Currency,
        CustomerCodeSnapshot = order.CustomerCodeSnapshot,
        CustomerNameSnapshot = order.CustomerNameSnapshot,
        CustomerGroupIdSnapshot = order.CustomerGroupIdSnapshot,
        CustomerGroupCodeSnapshot = order.CustomerGroupCodeSnapshot,
        CustomerGroupNameSnapshot = order.CustomerGroupNameSnapshot,
        ConfirmedAt = order.ConfirmedAt,
        CancelledAt = order.CancelledAt,
        TotalRevenueAmount = order.TotalRevenueAmount,
        TotalCostAmount = includeCost ? order.TotalCostAmount : null,
        TotalProfitAmount = includeProfit ? order.TotalProfitAmount : null,
        PaymentSummary = ToDto(paymentSummary ?? SalesOrderPaymentSummary.From(order.TotalRevenueAmount, 0)),
        Lines = order.Lines.Select(x => ToDto(x, includeCost, includeProfit)).ToList()
    };

    public SalesOrderLineDto ToDto(SalesOrderLine line, bool includeCost, bool includeProfit) => new()
    {
        Id = line.Id,
        LineNo = line.LineNo,
        ProductId = line.ProductId,
        BomVersionId = line.BomVersionId,
        BomVersionNoSnapshot = line.BomVersionNoSnapshot,
        ItemCodeSnapshot = line.ItemCodeSnapshot,
        ItemNameSnapshot = line.ItemNameSnapshot,
        UnitSnapshot = line.UnitSnapshot,
        Quantity = line.Quantity,
        SuggestedPriceVersionId = line.SuggestedPriceVersionId,
        SuggestedPriceSnapshot = line.SuggestedPriceSnapshot,
        ActualSellingPrice = line.ActualSellingPrice,
        OverrideReason = line.OverrideReason,
        InventoryTransactionId = line.InventoryTransactionId,
        RevenueAmount = line.RevenueAmount,
        CostPriceSnapshot = includeCost ? line.CostPriceSnapshot : null,
        CostAmountSnapshot = includeCost ? line.CostAmountSnapshot : null,
        ProfitAmount = includeProfit ? line.ProfitAmount : null,
        MarginPercent = includeProfit ? line.MarginPercent : null,
        BomSnapshotItems = line.BomSnapshotItems.Select(x => new SalesOrderBomSnapshotItemDto
        {
            Id = x.Id,
            ComponentId = x.ComponentId,
            ComponentCode = x.ComponentCode,
            ComponentName = x.ComponentName,
            Unit = x.Unit,
            QuantityPerProduct = x.QuantityPerProduct,
            TotalRequiredQuantity = x.TotalRequiredQuantity
        }).ToList()
    };

    public CustomerPurchaseHistoryDto ToDto(CustomerPurchaseHistoryRecord item) => new()
    {
        CustomerId = item.CustomerId,
        ProductId = item.ProductId,
        LastPurchaseDate = item.LastPurchaseDate,
        LastPurchasePrice = item.LastPurchasePrice,
        AveragePurchasePrice = item.AveragePurchasePrice,
        Revenue = item.Revenue,
        Profit = item.Profit
    };

    public SalesOrderPaymentSummaryDto ToDto(SalesOrderPaymentSummary summary) => new()
    {
        TotalAmount = summary.TotalAmount,
        PaidAmount = summary.PaidAmount,
        RemainingAmount = summary.RemainingAmount,
        PaymentStatus = summary.PaymentStatus
    };

    public SalesOrderPaymentDto ToDto(SalesOrderPayment payment) => new()
    {
        Id = payment.Id,
        SalesOrderId = payment.SalesOrderId,
        CustomerId = payment.CustomerId,
        Amount = payment.Amount,
        PaymentDate = payment.PaymentDate,
        PaymentMethod = payment.PaymentMethod,
        ReferenceNo = payment.ReferenceNo,
        Note = payment.Note,
        Status = payment.Status,
        IdempotencyKey = payment.IdempotencyKey
    };
}
