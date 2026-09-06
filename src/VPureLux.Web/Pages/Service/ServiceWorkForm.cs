using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using VPureLux.Service;

namespace VPureLux.Web.Pages.Service;

public class ServiceWorkForm
{
    [Required, StringLength(ServiceConsts.MaxCodeLength)] public string Code { get; set; } = string.Empty;
    [Required, StringLength(ServiceConsts.MaxNameLength)] public string Name { get; set; } = string.Empty;
    [Required, StringLength(ServiceConsts.MaxUnitLength)] public string Unit { get; set; } = string.Empty;
    [ModelBinder(BinderType = typeof(ServiceMoneyModelBinder))]
    [Range(typeof(decimal), "0", "9999999999999999.99", ParseLimitsInInvariantCulture = true)]
    public decimal DefaultPrice { get; set; }
    [ModelBinder(BinderType = typeof(ServiceMoneyModelBinder))]
    [Range(typeof(decimal), "0", "9999999999999999.99", ParseLimitsInInvariantCulture = true)]
    public decimal? StandardCost { get; set; }
    [EnumDataType(typeof(ServiceWorkStatus))] public ServiceWorkStatus Status { get; set; } = ServiceWorkStatus.Active;
    [StringLength(ServiceConsts.MaxNoteLength)] public string? Note { get; set; }
    [StringLength(40)] public string? ConcurrencyStamp { get; set; }

    public CreateUpdateServiceWorkDto ToInput() => new()
    {
        Code = Code, Name = Name, Unit = Unit, DefaultPrice = DefaultPrice, StandardCost = StandardCost,
        Status = Status, Note = Note, ConcurrencyStamp = ConcurrencyStamp
    };
}

// HTML number inputs submit invariant decimals, regardless of the operator's display culture.
public class ServiceMoneyModelBinder : IModelBinder
{
    public Task BindModelAsync(ModelBindingContext context)
    {
        var value = context.ValueProvider.GetValue(context.ModelName);
        if (value == ValueProviderResult.None) return Task.CompletedTask;
        context.ModelState.SetModelValue(context.ModelName, value);
        if (string.IsNullOrWhiteSpace(value.FirstValue) && context.ModelMetadata.IsNullableValueType)
            context.Result = ModelBindingResult.Success(null);
        else if (value.Length == 1 && TryParseAmount(value.FirstValue, out var amount))
            context.Result = ModelBindingResult.Success(amount);
        else
            context.ModelState.TryAddModelError(context.ModelName,
                context.ModelMetadata.ModelBindingMessageProvider.AttemptedValueIsInvalidAccessor(value.ToString(), context.ModelMetadata.GetDisplayName()));
        return Task.CompletedTask;
    }

    private static bool TryParseAmount(string? value, out decimal amount) =>
        decimal.TryParse(value,
            NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint,
            CultureInfo.InvariantCulture, out amount) ||
        decimal.TryParse(value,
            NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint,
            CultureInfo.GetCultureInfo("vi-VN"), out amount);
}
