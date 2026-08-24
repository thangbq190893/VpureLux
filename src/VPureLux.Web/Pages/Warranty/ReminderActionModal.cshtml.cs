using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VPureLux.Permissions;
using VPureLux.Warranty;

namespace VPureLux.Web.Pages.Warranty;

[Authorize(VPureLuxPermissions.Warranty.ManageReminders)]
public class ReminderActionModalModel : VPureLuxPageModel
{
    private static readonly string[] SupportedActions = ["Complete", "Skip", "Reschedule", "Suspend"];
    private readonly IWarrantyAppService _warranty;

    [BindProperty(SupportsGet = true)] public Guid Id { get; set; }
    [BindProperty(SupportsGet = true)] public Guid AssetId { get; set; }
    [BindProperty(SupportsGet = true)] public string Action { get; set; } = string.Empty;
    [BindProperty(SupportsGet = true)] public DateTime? DueDate { get; set; }
    [BindProperty] public ReminderActionInput Input { get; set; } = new();

    public string TitleKey => $"Warranty:Action:{Action}";

    public ReminderActionModalModel(IWarrantyAppService warranty)
    {
        _warranty = warranty;
    }

    public IActionResult OnGet()
    {
        if (!SupportedActions.Contains(Action, StringComparer.Ordinal))
        {
            return NotFound();
        }
        Input.IdempotencyKey = Guid.NewGuid().ToString("N");
        Input.ActionDate = Action == "Reschedule" ? DueDate : Clock.Now;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!SupportedActions.Contains(Action, StringComparer.Ordinal))
        {
            return NotFound();
        }
        if (!ModelState.IsValid)
        {
            return Page();
        }
        switch (Action)
        {
            case "Complete":
                await _warranty.CompleteReminderAsync(Id, new CompleteReplacementReminderDto
                {
                    CompletedAt = Input.ActionDate,
                    Note = Input.Reason,
                    IdempotencyKey = Input.IdempotencyKey
                });
                break;
            case "Skip":
                await _warranty.SkipReminderAsync(Id, new SkipReplacementReminderDto
                {
                    Note = Input.Reason,
                    IdempotencyKey = Input.IdempotencyKey
                });
                break;
            case "Reschedule":
                if (!Input.ActionDate.HasValue)
                {
                    ModelState.AddModelError(nameof(Input.ActionDate), L["RequiredField"]);
                    return Page();
                }
                await _warranty.RescheduleReminderAsync(Id, new RescheduleReplacementReminderDto
                {
                    DueDate = Input.ActionDate.Value,
                    Note = Input.Reason,
                    IdempotencyKey = Input.IdempotencyKey
                });
                break;
            case "Suspend":
                await _warranty.SuspendAssetAsync(AssetId, new SuspendCustomerAssetDto
                {
                    Reason = Input.Reason,
                    IdempotencyKey = Input.IdempotencyKey
                });
                break;
        }
        return NoContent();
    }

    public class ReminderActionInput
    {
        public DateTime? ActionDate { get; set; }

        [Required]
        [StringLength(WarrantyConsts.MaxNoteLength)]
        public string Reason { get; set; } = string.Empty;

        [Required]
        [StringLength(WarrantyConsts.MaxIdempotencyKeyLength)]
        public string IdempotencyKey { get; set; } = string.Empty;
    }
}
