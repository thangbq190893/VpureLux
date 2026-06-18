using System;
using System.Globalization;

namespace VPureLux.Web.Pages.Bom;

public static class BomUi
{
    public const string DateFormat = "dd/MM/yyyy";

    private static readonly CultureInfo VietnameseCulture = CultureInfo.GetCultureInfo("vi-VN");

    public static string FormatDate(DateTime value)
    {
        return value.ToString(DateFormat, VietnameseCulture);
    }

    public static string FormatDate(DateTime? value)
    {
        return value.HasValue ? FormatDate(value.Value) : string.Empty;
    }

    public static bool TryParseDate(string? value, out DateTime date)
    {
        return DateTime.TryParseExact(
            value,
            DateFormat,
            VietnameseCulture,
            DateTimeStyles.None,
            out date);
    }

    public static string FormatQuantity(decimal value)
    {
        return value.ToString("0", CultureInfo.InvariantCulture);
    }
}
