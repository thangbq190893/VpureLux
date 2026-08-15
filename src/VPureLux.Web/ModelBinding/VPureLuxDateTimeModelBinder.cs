using System;
using System.Globalization;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace VPureLux.Web.ModelBinding;

public class VPureLuxDateTimeModelBinder : IModelBinder
{
    private static readonly string[] SupportedFormats =
    [
        "dd/MM/yyyy",
        "d/M/yyyy",
        "yyyy-MM-dd",
        "yyyy-M-d",
        "MM/dd/yyyy",
        "M/d/yyyy"
    ];

    public Task BindModelAsync(ModelBindingContext bindingContext)
    {
        ArgumentNullException.ThrowIfNull(bindingContext);

        var valueProviderResult = bindingContext.ValueProvider.GetValue(bindingContext.ModelName);
        if (valueProviderResult == ValueProviderResult.None)
        {
            return Task.CompletedTask;
        }

        bindingContext.ModelState.SetModelValue(bindingContext.ModelName, valueProviderResult);
        var value = valueProviderResult.FirstValue;
        if (string.IsNullOrWhiteSpace(value))
        {
            if (IsNullableDateTime(bindingContext.ModelType))
            {
                bindingContext.Result = ModelBindingResult.Success(null);
            }

            return Task.CompletedTask;
        }

        if (TryParseDate(value, valueProviderResult.Culture, out var parsed))
        {
            bindingContext.Result = ModelBindingResult.Success(parsed);
            bindingContext.ModelState.MarkFieldValid(bindingContext.ModelName);
            return Task.CompletedTask;
        }

        bindingContext.ModelState.TryAddModelError(
            bindingContext.ModelName,
            $"The value '{value}' is not valid for {bindingContext.ModelMetadata.GetDisplayName()}.");

        return Task.CompletedTask;
    }

    private static bool TryParseDate(string value, CultureInfo culture, out DateTime parsed)
    {
        return DateTime.TryParseExact(
                   value,
                   SupportedFormats,
                   CultureInfo.InvariantCulture,
                   DateTimeStyles.AllowWhiteSpaces,
                   out parsed) ||
               DateTime.TryParse(
                   value,
                   CultureInfo.GetCultureInfo("vi-VN"),
                   DateTimeStyles.AllowWhiteSpaces,
                   out parsed) ||
               DateTime.TryParse(
                   value,
                   culture,
                   DateTimeStyles.AllowWhiteSpaces,
                   out parsed);
    }

    private static bool IsNullableDateTime(Type modelType) =>
        Nullable.GetUnderlyingType(modelType) == typeof(DateTime);
}
