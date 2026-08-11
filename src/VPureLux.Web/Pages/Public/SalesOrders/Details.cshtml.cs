using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using VPureLux.Catalog;
using VPureLux.Customers;
using VPureLux.Sales;
using VPureLux.Web.Pages;
using VPureLux.Web.Sales;

namespace VPureLux.Web.Pages.Public.SalesOrders;

[AllowAnonymous]
public class DetailsModel : VPureLuxPageModel
{
    private readonly SalesOrderPublicLinkService _publicLinks;
    private readonly ISalesOrderRepository _salesOrders;
    private readonly ICustomerRepository _customers;
    private readonly IProductRepository _products;
    private readonly ISalesOrderPaymentRepository _payments;

    public bool InvalidLink { get; private set; }
    public bool ShowPrices { get; private set; }
    public PublicSalesOrderViewModel Order { get; private set; } = new();

    public DetailsModel(
        SalesOrderPublicLinkService publicLinks,
        ISalesOrderRepository salesOrders,
        ICustomerRepository customers,
        IProductRepository products,
        ISalesOrderPaymentRepository payments)
    {
        _publicLinks = publicLinks;
        _salesOrders = salesOrders;
        _customers = customers;
        _products = products;
        _payments = payments;
    }

    public async Task OnGetAsync(string token)
    {
        if (!_publicLinks.TryReadToken(token, out var payload))
        {
            MarkInvalidLink();
            return;
        }

        var order = await _salesOrders.FindAsync(payload.SalesOrderId, includeDetails: true);
        if (order == null)
        {
            MarkInvalidLink();
            return;
        }

        ShowPrices = payload.ShowPrices;
        Order = await BuildViewModelAsync(order);
    }

    private async Task<PublicSalesOrderViewModel> BuildViewModelAsync(SalesOrder order)
    {
        var customer = await _customers.FindAsync(order.CustomerId);
        var productLabels = await GetProductLabelsAsync(order);
        var lines = order.Lines
            .OrderBy(x => x.LineNo)
            .Select(line => new PublicSalesOrderLineViewModel
            {
                LineNo = line.LineNo,
                ProductLabel = GetProductLabel(line, productLabels),
                Quantity = line.Quantity,
                Unit = string.IsNullOrWhiteSpace(line.UnitSnapshot)
                    ? SalesConsts.DefaultProductUnit
                    : line.UnitSnapshot,
                ActualSellingPrice = line.ActualSellingPrice,
                RevenueAmount = line.RevenueAmount > 0
                    ? line.RevenueAmount
                    : decimal.Round(line.Quantity * line.ActualSellingPrice, SalesConsts.MoneyScale, MidpointRounding.AwayFromZero)
            })
            .ToList();

        var totalAmount = order.Status == SalesOrderStatus.Confirmed
            ? order.TotalRevenueAmount
            : lines.Sum(x => x.RevenueAmount);
        var paidAmount = 0m;
        if (order.Status == SalesOrderStatus.Confirmed)
        {
            var paidAmounts = await _payments.GetPostedPaidAmountsAsync([order.Id]);
            paidAmounts.TryGetValue(order.Id, out paidAmount);
        }

        var summary = order.Status == SalesOrderStatus.Confirmed
            ? SalesOrderPaymentSummary.From(totalAmount, paidAmount)
            : new SalesOrderPaymentSummary(totalAmount, 0, totalAmount, SalesOrderReceivableStatus.NotApplicable);

        return new PublicSalesOrderViewModel
        {
            OrderNo = order.OrderNo,
            OrderDate = order.OrderDate,
            Status = order.Status,
            CustomerDisplay = GetCustomerDisplay(order, customer),
            CustomerPhone = customer?.PhoneNumber ?? string.Empty,
            CustomerAddress = customer?.Address ?? string.Empty,
            Lines = lines,
            PaymentSummary = summary
        };
    }

    private async Task<Dictionary<Guid, string>> GetProductLabelsAsync(SalesOrder order)
    {
        var labels = new Dictionary<Guid, string>();
        foreach (var productId in order.Lines.Select(x => x.ProductId).Distinct())
        {
            var product = await _products.FindAsync(productId);
            if (product != null)
            {
                labels[productId] = $"{product.Code} - {product.Name}";
            }
        }

        return labels;
    }

    private static string GetProductLabel(SalesOrderLine line, IReadOnlyDictionary<Guid, string> productLabels)
    {
        if (!string.IsNullOrWhiteSpace(line.ItemCodeSnapshot) || !string.IsNullOrWhiteSpace(line.ItemNameSnapshot))
        {
            return $"{line.ItemCodeSnapshot} - {line.ItemNameSnapshot}".Trim(' ', '-');
        }

        return productLabels.TryGetValue(line.ProductId, out var label) ? label : line.ProductId.ToString("N");
    }

    private string GetCustomerDisplay(SalesOrder order, Customer? customer)
    {
        if (!string.IsNullOrWhiteSpace(order.CustomerCodeSnapshot) || !string.IsNullOrWhiteSpace(order.CustomerNameSnapshot))
        {
            return $"{order.CustomerCodeSnapshot} - {order.CustomerNameSnapshot}".Trim(' ', '-');
        }

        return customer == null
            ? L["Sales:CustomerContextUnavailable"]
            : $"{customer.Code} - {customer.Name}";
    }

    private void MarkInvalidLink()
    {
        InvalidLink = true;
        Response.StatusCode = StatusCodes.Status404NotFound;
    }

    public class PublicSalesOrderViewModel
    {
        public string OrderNo { get; set; } = string.Empty;
        public DateTime OrderDate { get; set; }
        public SalesOrderStatus Status { get; set; }
        public string CustomerDisplay { get; set; } = string.Empty;
        public string CustomerPhone { get; set; } = string.Empty;
        public string CustomerAddress { get; set; } = string.Empty;
        public List<PublicSalesOrderLineViewModel> Lines { get; set; } = [];
        public SalesOrderPaymentSummary PaymentSummary { get; set; } =
            new(0, 0, 0, SalesOrderReceivableStatus.NotApplicable);
    }

    public class PublicSalesOrderLineViewModel
    {
        public int LineNo { get; set; }
        public string ProductLabel { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public string Unit { get; set; } = string.Empty;
        public decimal ActualSellingPrice { get; set; }
        public decimal RevenueAmount { get; set; }
    }
}
