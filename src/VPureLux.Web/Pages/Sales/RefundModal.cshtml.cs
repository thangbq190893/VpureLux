using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VPureLux.Permissions;
using VPureLux.Sales;

namespace VPureLux.Web.Pages.Sales;

[Authorize(VPureLuxPermissions.Sales.ManageRefunds)]
public class RefundModalModel : VPureLuxPageModel
{
    private readonly ISalesPostConfirmationAppService _postConfirmation;

    [BindProperty(SupportsGet = true)] public SalesRefundTaskType TaskType { get; set; }
    [BindProperty(SupportsGet = true)] public Guid OperationId { get; set; }
    [BindProperty(SupportsGet = true)] public string OrderNo { get; set; } = string.Empty;
    [BindProperty(SupportsGet = true)] public decimal RemainingAmount { get; set; }
    [BindProperty] public RefundInput Input { get; set; } = new();

    public RefundModalModel(ISalesPostConfirmationAppService postConfirmation) =>
        _postConfirmation = postConfirmation;

    public void OnGet()
    {
        Input.Amount = RemainingAmount;
        Input.RefundedAt = Clock.Now;
        Input.IdempotencyKey = Guid.NewGuid().ToString("N");
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid) return Page();
        var dto = new RecordSalesOrderRefundDto
        {
            Amount = Input.Amount,
            RefundedAt = Input.RefundedAt,
            PaymentMethod = Input.PaymentMethod,
            ReferenceNo = Input.ReferenceNo,
            Reason = Input.Reason,
            IdempotencyKey = Input.IdempotencyKey
        };
        if (TaskType == SalesRefundTaskType.Revision)
        {
            await _postConfirmation.RecordRevisionRefundAsync(OperationId, dto);
        }
        else
        {
            await _postConfirmation.RecordCancellationRefundAsync(OperationId, dto);
        }
        return NoContent();
    }

    public class RefundInput
    {
        [Range(typeof(decimal), "0.01", "9999999999999999.99")] public decimal Amount { get; set; }
        public DateTime RefundedAt { get; set; }
        public SalesPaymentMethod PaymentMethod { get; set; } = SalesPaymentMethod.BankTransfer;
        [StringLength(SalesConsts.MaxPaymentReferenceNoLength)] public string? ReferenceNo { get; set; }
        [Required, StringLength(SalesConsts.MaxReasonLength)] public string Reason { get; set; } = string.Empty;
        [Required, StringLength(SalesConsts.MaxIdempotencyKeyLength)] public string IdempotencyKey { get; set; } = string.Empty;
    }
}
