using System;
using System.Globalization;

namespace VPureLux.Web.Pages.Pricing;

internal static class PricingDateUi
{
    public const string DateFormat = "dd/MM/yyyy";
    private static readonly CultureInfo VietnameseCulture = CultureInfo.GetCultureInfo("vi-VN");

    public static string Format(DateTime date)
    {
        return date.ToString(DateFormat, VietnameseCulture);
    }

    public static bool TryParse(string? value, out DateTime date)
    {
        return DateTime.TryParseExact(
            value,
            DateFormat,
            VietnameseCulture,
            DateTimeStyles.None,
            out date);
    }
}
