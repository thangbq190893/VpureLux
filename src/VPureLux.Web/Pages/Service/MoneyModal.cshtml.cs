using System;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using VPureLux.Permissions;
using VPureLux.Sales;
using VPureLux.Service;
using Volo.Abp;

namespace VPureLux.Web.Pages.Service;

[Authorize(VPureLuxPermissions.Service.Default)]
[Authorize(VPureLuxPermissions.Service.ManagePayments)]
public class MoneyModalModel(IServicePaymentAppService money, IOptions<ServiceOptions> options) : VPureLuxPageModel
{
    [BindProperty(SupportsGet = true)] public Guid Id { get; set; }
    [BindProperty(SupportsGet = true)] public bool Refund { get; set; }
    [BindProperty] public MoneyForm Input { get; set; } = new();
    public ServiceMoneySummaryDto Summary { get; private set; } = new();
    public string Title => L[Refund ? "Service:RecordRefund" : "Service:RecordPayment"];
    public decimal Available => Refund ? Summary.RefundDue : Summary.Status == ServiceOrderStatus.Completed ? Summary.Receivable : Summary.PlannedRemaining;

    public async Task<IActionResult> OnGetAsync()
    {
        if (!options.Value.IsEnabled) return NotFound();
        Summary = await money.GetSummaryAsync(Id);
        if (Summary.HasInconsistentLedger || Available <= 0 || (!Refund && Summary.Status == ServiceOrderStatus.Cancelled)) return BadRequest();
        Input.IdempotencyKey = Guid.NewGuid().ToString("N");
        Input.OccurredAt = DateTimeOffset.UtcNow.ToOffset(TimeSpan.FromHours(7)).ToString("yyyy-MM-ddTHH:mm", CultureInfo.InvariantCulture);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!options.Value.IsEnabled) return NotFound();
        Summary = await money.GetSummaryAsync(Id);
        if (!TryParseMoney(Input.Amount, out var amount)) ModelState.AddModelError("Input.Amount", L[ServiceErrorCodes.InvalidMoney]);
        if (!DateTime.TryParseExact(Input.OccurredAt, "yyyy-MM-ddTHH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
            ModelState.AddModelError("Input.OccurredAt", L[ServiceErrorCodes.InvalidMoney]);
        if (Refund && string.IsNullOrWhiteSpace(Input.Note)) ModelState.AddModelError("Input.Note", L[ServiceErrorCodes.MoneyReasonRequired]);
        if (!ModelState.IsValid) return Page();
        try
        {
            var at = new DateTimeOffset(date, TimeSpan.FromHours(7));
            if (Refund)
                await money.RefundAsync(Id, new() { Amount = amount, RefundDate = at, Method = Input.Method,
                    IdempotencyKey = Input.IdempotencyKey, ReferenceNo = Input.ReferenceNo, Reason = Input.Note! });
            else
                await money.AddPaymentAsync(Id, new() { Amount = amount, PaymentDate = at, PaymentMethod = Input.Method,
                    IdempotencyKey = Input.IdempotencyKey, ReferenceNo = Input.ReferenceNo, Note = Input.Note });
            return NoContent();
        }
        catch (BusinessException e)
        {
            ModelState.AddModelError(string.Empty, L[e.Code ?? VPureLuxDomainErrorCodes.ValidationFailed]);
            return Page();
        }
    }

    // Financial text input accepts strict vi-VN grouping plus invariant HTML decimal amounts.
    // A single dot followed by three digits is a vi-VN group, not three decimal places.
    public static bool TryParseMoney(string? text, out decimal amount)
    {
        amount = 0;
        var value = text?.Trim();
        if (value == null || value.Length > 32) return false;
        if (Regex.IsMatch(value, @"^\d{1,3}(\.\d{3})+(,\d{1,2})?$", RegexOptions.CultureInvariant))
            value = value.Replace(".", "").Replace(',', '.');
        else if (Regex.IsMatch(value, @"^\d+(,\d{1,2})?$", RegexOptions.CultureInvariant))
            value = value.Replace(',', '.');
        else if (!Regex.IsMatch(value, @"^\d+(\.\d{1,2})?$", RegexOptions.CultureInvariant)) return false;
        return decimal.TryParse(value, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out amount)
            && amount > 0 && amount <= 9999999999999999.99m;
    }

    public class MoneyForm
    {
        [Required, StringLength(32)] public string Amount { get; set; } = string.Empty;
        [Required] public string OccurredAt { get; set; } = string.Empty;
        [EnumDataType(typeof(SalesPaymentMethod))] public SalesPaymentMethod Method { get; set; } = SalesPaymentMethod.Cash;
        [Required, StringLength(ServiceConsts.MaxIdempotencyKeyLength)] public string IdempotencyKey { get; set; } = string.Empty;
        [StringLength(ServiceConsts.MaxReferenceNoLength)] public string? ReferenceNo { get; set; }
        [StringLength(ServiceConsts.MaxNoteLength)] public string? Note { get; set; }
    }
}
