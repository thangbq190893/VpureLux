using System;
using System.Globalization;

namespace VPureLux.Web.Pages.Reports;

public static class ReportDateParser
{
    private static readonly string[] IsoFormats = ["yyyy-MM-dd", "yyyy/MM/dd"];

    public static bool TryParseRange(
        string? fromText,
        string? toText,
        out DateTime? fromDate,
        out DateTime? toDate)
    {
        var slashOrder = InferSlashOrder(fromText, toText);
        var fromValid = TryParse(fromText, slashOrder, out fromDate);
        var toValid = TryParse(toText, slashOrder, out toDate);
        return fromValid && toValid;
    }

    public static string ToInputValue(DateTime? value) =>
        value.HasValue ? value.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) : string.Empty;

    private static bool TryParse(string? text, SlashDateOrder slashOrder, out DateTime? date)
    {
        date = null;
        if (string.IsNullOrWhiteSpace(text))
        {
            return true;
        }

        text = text.Trim();
        if (DateTime.TryParseExact(
                text,
                IsoFormats,
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var parsed))
        {
            date = parsed.Date;
            return true;
        }

        var slashFormats = slashOrder == SlashDateOrder.MonthDayYear
            ? new[] { "M/d/yyyy", "MM/dd/yyyy", "d/M/yyyy", "dd/MM/yyyy" }
            : new[] { "d/M/yyyy", "dd/MM/yyyy", "M/d/yyyy", "MM/dd/yyyy" };
        if (DateTime.TryParseExact(
                text,
                slashFormats,
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out parsed))
        {
            date = parsed.Date;
            return true;
        }

        return false;
    }

    private static SlashDateOrder InferSlashOrder(params string?[] values)
    {
        foreach (var value in values)
        {
            var parts = value?.Split('/');
            if (parts is not { Length: 3 } ||
                !int.TryParse(parts[0], NumberStyles.None, CultureInfo.InvariantCulture, out var first) ||
                !int.TryParse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture, out var second))
            {
                continue;
            }

            if (first > 12)
            {
                return SlashDateOrder.DayMonthYear;
            }

            if (second > 12)
            {
                return SlashDateOrder.MonthDayYear;
            }
        }

        return SlashDateOrder.DayMonthYear;
    }

    private enum SlashDateOrder
    {
        DayMonthYear,
        MonthDayYear
    }
}
