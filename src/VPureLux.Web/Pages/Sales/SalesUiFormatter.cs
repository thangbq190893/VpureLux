using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Microsoft.Extensions.Localization;
using VPureLux;
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
                if (exception.Code == VPureLuxDomainErrorCodes.SalesInventoryValidationFailed &&
                    exception.Data.Contains("InventoryErrorCode") &&
                    exception.Data["InventoryErrorCode"] is string inventoryErrorCode)
                {
                    var inventoryMessage = localizer[inventoryErrorCode];
                    return FormatSalesInventoryError(localizer, exception, localized.Value,
                        inventoryMessage.ResourceNotFound ? inventoryErrorCode : inventoryMessage.Value);
                }

                return localized.Value;
            }
        }

        return exception.Message;
    }

    private static string FormatSalesInventoryError(
        IStringLocalizer localizer,
        BusinessException exception,
        string baseMessage,
        string inventoryMessage)
    {
        var builder = new StringBuilder(baseMessage.TrimEnd('.', ' '));
        builder.Append(". ");
        builder.Append(localizer["Sales:InventoryErrorCause"]);
        builder.Append(": ");
        builder.Append(inventoryMessage.TrimEnd('.', ' '));
        builder.Append('.');

        var product = FormatCodeName(GetData(exception, "ProductCode"), GetData(exception, "ProductName"));
        if (!string.IsNullOrWhiteSpace(product))
        {
            builder.Append(' ');
            builder.Append(localizer["Sales:InventoryErrorProduct"]);
            builder.Append(": ");
            builder.Append(product);
            AppendParenthesized(builder, localizer["Sales:InventoryErrorLineNo"], GetData(exception, "SalesLineNo"));
            AppendParenthesized(builder, localizer["Sales:InventoryErrorSalesQuantity"],
                FormatDecimalText(GetData(exception, "SalesLineQuantity")));
            builder.Append('.');
        }

        var component = FormatCodeName(GetData(exception, "ComponentCode"), GetData(exception, "ComponentName"));
        if (!string.IsNullOrWhiteSpace(component))
        {
            builder.Append(' ');
            builder.Append(localizer["Sales:InventoryErrorComponent"]);
            builder.Append(": ");
            builder.Append(component);

            var unit = GetData(exception, "ComponentUnit");
            var requiredQuantity = FormatQuantity(GetData(exception, "RequiredQuantity"), unit);
            if (!string.IsNullOrWhiteSpace(requiredQuantity))
            {
                builder.Append("; ");
                builder.Append(localizer["Sales:InventoryErrorRequiredQuantity"]);
                builder.Append(": ");
                builder.Append(requiredQuantity);
            }

            var availableQuantity = FormatQuantity(GetData(exception, "AvailableQuantity"), unit);
            if (!string.IsNullOrWhiteSpace(availableQuantity))
            {
                builder.Append("; ");
                builder.Append(localizer["Sales:InventoryErrorAvailableQuantity"]);
                builder.Append(": ");
                builder.Append(availableQuantity);
            }

            builder.Append('.');
        }

        var lotNo = GetData(exception, "LotNo");
        var lotAvailable = FormatQuantity(GetData(exception, "LotAvailableQuantity"), GetData(exception, "ComponentUnit"));
        if (!string.IsNullOrWhiteSpace(lotNo) || !string.IsNullOrWhiteSpace(lotAvailable))
        {
            builder.Append(' ');
            builder.Append(localizer["Sales:InventoryErrorFifoLot"]);
            builder.Append(": ");
            builder.Append(string.IsNullOrWhiteSpace(lotNo) ? localizer["Sales:InventoryErrorUnknown"] : lotNo);
            if (!string.IsNullOrWhiteSpace(lotAvailable))
            {
                builder.Append(" (");
                builder.Append(localizer["Sales:InventoryErrorAvailableQuantity"]);
                builder.Append(": ");
                builder.Append(lotAvailable);
                builder.Append(')');
            }

            builder.Append('.');
        }

        var invalidField = GetData(exception, "InvalidField");
        var invalidValue = GetData(exception, "InvalidValue");
        if (!string.IsNullOrWhiteSpace(invalidField) || !string.IsNullOrWhiteSpace(invalidValue))
        {
            builder.Append(' ');
            builder.Append(localizer["Sales:InventoryErrorInvalidValue"]);
            builder.Append(": ");
            builder.Append(string.IsNullOrWhiteSpace(invalidField) ? localizer["Sales:InventoryErrorUnknown"] : invalidField);
            if (!string.IsNullOrWhiteSpace(invalidValue))
            {
                builder.Append('=');
                builder.Append(FormatDecimalText(invalidValue));
            }

            builder.Append('.');
        }

        return builder.ToString();
    }

    private static string? GetData(BusinessException exception, string key) =>
        exception.Data.Contains(key)
            ? Convert.ToString(exception.Data[key], CultureInfo.CurrentCulture)
            : null;

    private static string FormatCodeName(string? code, string? name)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return name ?? string.Empty;
        }

        return string.IsNullOrWhiteSpace(name) ? code : $"{code} - {name}";
    }

    private static string FormatQuantity(string? quantity, string? unit)
    {
        if (string.IsNullOrWhiteSpace(quantity))
        {
            return string.Empty;
        }

        var formatted = FormatDecimalText(quantity);
        return string.IsNullOrWhiteSpace(unit) ? formatted : $"{formatted} {unit}";
    }

    private static string FormatDecimalText(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        if (!TryParseDecimal(value, out var number))
        {
            return value;
        }

        return number == decimal.Truncate(number)
            ? number.ToString("0", CultureInfo.CurrentCulture)
            : number.ToString("0.####", CultureInfo.CurrentCulture);
    }

    private static bool TryParseDecimal(string value, out decimal number) =>
        decimal.TryParse(value, NumberStyles.Number, CultureInfo.CurrentCulture, out number) ||
        decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out number);

    private static void AppendParenthesized(StringBuilder builder, string label, string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        builder.Append(" (");
        builder.Append(label);
        builder.Append(": ");
        builder.Append(value);
        builder.Append(')');
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
