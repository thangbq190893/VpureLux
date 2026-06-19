using System;
using System.Collections.Generic;
using Microsoft.Extensions.Localization;
using VPureLux.Sales;
using Volo.Abp;

namespace VPureLux.Web.Pages.Sales;

public static class SalesUiFormatter
{
    public static string GetFriendlyErrorMessage(IStringLocalizer localizer, BusinessException exception)
    {
        if (!string.IsNullOrWhiteSpace(exception.Code))
        {
            var localized = localizer[exception.Code];
            if (!localized.ResourceNotFound)
            {
                return localized.Value;
            }
        }

        return exception.Message;
    }

    public static string GetProductLabel(
        SalesOrderLineDto line,
        IReadOnlyDictionary<Guid, string> productLabels,
        IStringLocalizer localizer)
    {
        if (!string.IsNullOrWhiteSpace(line.ItemCodeSnapshot) || !string.IsNullOrWhiteSpace(line.ItemNameSnapshot))
        {
            return $"{line.ItemCodeSnapshot} - {line.ItemNameSnapshot}".Trim(' ', '-');
        }

        return productLabels.TryGetValue(line.ProductId, out var product)
            ? product
            : localizer["Sales:ProductContextUnavailable"];
    }

    public static string GetProductLabel(
        CustomerPurchaseHistoryDto item,
        IReadOnlyDictionary<Guid, string> productLabels,
        IStringLocalizer localizer)
    {
        return productLabels.TryGetValue(item.ProductId, out var product)
            ? product
            : localizer["Sales:ProductContextUnavailable"];
    }

    public static string GetBomBadgeClass(bool hasPublishedBom) =>
        hasPublishedBom ? "badge bg-success" : "badge bg-warning text-dark";
}

public class SalesProductContextViewModel
{
    public Guid ProductId { get; set; }
    public string ProductLabel { get; set; } = string.Empty;
    public bool HasPublishedBom { get; set; }
    public bool HasImage { get; set; }
    public decimal? SuggestedPrice { get; set; }
    public string BomStatusText { get; set; } = string.Empty;
}
