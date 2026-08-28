using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VPureLux.Permissions;
using VPureLux.Sales;

namespace VPureLux.Web.Pages.Sales;

[Authorize(VPureLuxPermissions.Sales.ConfirmReturnedGoods)]
public class ReturnConfirmationModalModel : VPureLuxPageModel
{
    private readonly ISalesPostConfirmationAppService _postConfirmation;

    [BindProperty(SupportsGet = true)] public SalesReturnTaskType TaskType { get; set; }
    [BindProperty(SupportsGet = true)] public Guid OperationId { get; set; }
    [BindProperty(SupportsGet = true)] public Guid? RevisionLineId { get; set; }
    [BindProperty(SupportsGet = true)] public string OrderNo { get; set; } = string.Empty;
    [BindProperty(SupportsGet = true)] public string ItemName { get; set; } = string.Empty;
    [BindProperty(SupportsGet = true)] public string Quantity { get; set; } = string.Empty;
    [BindProperty(SupportsGet = true)] public bool IsException { get; set; }
    [BindProperty] public ReturnConfirmationInput Input { get; set; } = new();

    public ReturnConfirmationModalModel(ISalesPostConfirmationAppService postConfirmation) =>
        _postConfirmation = postConfirmation;

    public void OnGet()
    {
        Input.IsEligibleForRestock = true;
        Input.IdempotencyKey = Guid.NewGuid().ToString("N");
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid) return Page();
        if (TaskType == SalesReturnTaskType.RevisionLine)
        {
            if (!RevisionLineId.HasValue) return NotFound();
            await _postConfirmation.ConfirmRevisionReturnedGoodsAsync(OperationId, new ConfirmRevisionReturnedGoodsDto
            {
                RevisionLineIds = [RevisionLineId.Value],
                Reason = Input.Reason
            });
        }
        else
        {
            await _postConfirmation.ConfirmReturnedGoodsAsync(OperationId, new ConfirmCancellationReturnedGoodsDto
            {
                IsEligibleForRestock = Input.IsEligibleForRestock,
                Reason = Input.Reason,
                IdempotencyKey = Input.IdempotencyKey
            });
        }
        return NoContent();
    }

    public class ReturnConfirmationInput
    {
        public bool IsEligibleForRestock { get; set; } = true;
        [Required, StringLength(SalesConsts.MaxReasonLength)] public string Reason { get; set; } = string.Empty;
        [Required, StringLength(SalesConsts.MaxIdempotencyKeyLength)] public string IdempotencyKey { get; set; } = string.Empty;
    }
}
