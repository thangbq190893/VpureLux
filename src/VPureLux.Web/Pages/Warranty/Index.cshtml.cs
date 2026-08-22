using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using VPureLux.Localization;
using VPureLux.Permissions;
using Volo.Abp.Application.Dtos;

namespace VPureLux.Web.Pages.Warranty;

[Authorize(VPureLuxPermissions.Warranty.View)]
public class IndexModel : VPureLuxPageModel
{
    private static readonly CultureInfo Vi = CultureInfo.GetCultureInfo("vi-VN");

    private readonly VPureLux.Warranty.IWarrantyAppService _warrantyAppService;
    private readonly IAuthorizationService _authorizationService;
    private readonly IStringLocalizer<VPureLuxResource> _localizer;

    public bool CanManageReminders { get; private set; }

    public IndexModel(
        VPureLux.Warranty.IWarrantyAppService warrantyAppService,
        IAuthorizationService authorizationService,
        IStringLocalizer<VPureLuxResource> localizer)
    {
        _warrantyAppService = warrantyAppService;
        _authorizationService = authorizationService;
        _localizer = localizer;
    }

    public async Task OnGetAsync()
    {
        CanManageReminders = (await _authorizationService.AuthorizeAsync(User, VPureLuxPermissions.Warranty.ManageReminders)).Succeeded;
    }

    public async Task<JsonResult> OnGetListAsync(VPureLux.Warranty.GetWarrantyReminderListInput input)
    {
        var result = await _warrantyAppService.GetReminderListAsync(input);
        return new JsonResult(new PagedResultDto<WarrantyReminderRow>(
            result.TotalCount,
            result.Items.Select(ToRow).ToList()));
    }

    public async Task<JsonResult> OnPostCompleteAsync(Guid id)
    {
        await _warrantyAppService.CompleteReminderAsync(id, new VPureLux.Warranty.CompleteReplacementReminderDto());
        return new JsonResult(new { success = true });
    }

    public async Task<JsonResult> OnPostSkipAsync(Guid id, string? note)
    {
        await _warrantyAppService.SkipReminderAsync(id, new VPureLux.Warranty.SkipReplacementReminderDto { Note = note });
        return new JsonResult(new { success = true });
    }

    public async Task<JsonResult> OnPostRescheduleAsync(Guid id, DateTime dueDate, string? note)
    {
        await _warrantyAppService.RescheduleReminderAsync(
            id,
            new VPureLux.Warranty.RescheduleReplacementReminderDto
            {
                DueDate = dueDate,
                Note = note
            });
        return new JsonResult(new { success = true });
    }

    private WarrantyReminderRow ToRow(VPureLux.Warranty.WarrantyReminderListDto reminder) =>
        new(
            reminder.Id,
            reminder.AssetNo,
            $"{reminder.CustomerCode} - {reminder.CustomerName}",
            $"{reminder.ProductCode} - {reminder.ProductName}",
            $"{reminder.ComponentCode} - {reminder.ComponentName}",
            FormatQuantity(reminder.QuantityPerProduct) + " " + reminder.ComponentUnit,
            reminder.DueDate.ToString("dd/MM/yyyy", Vi),
            reminder.DueDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            reminder.CycleMonths,
            reminder.WarningDaysBeforeDue,
            _localizer[$"Warranty:Status:{reminder.Status}"].Value,
            GetStatusBadgeClass(reminder.Status),
            $"{reminder.OrderNo} / {_localizer["Warranty:LineNo"].Value} {reminder.LineNo}",
            reminder.Note,
            reminder.Status == VPureLux.Warranty.AssetReplacementReminderStatus.Pending);

    private static string FormatQuantity(decimal value)
    {
        var rounded = decimal.Round(value, 0, MidpointRounding.AwayFromZero);
        return rounded == value
            ? rounded.ToString("#,0", Vi)
            : value.ToString("#,0.####", Vi);
    }

    private static string GetStatusBadgeClass(VPureLux.Warranty.AssetReplacementReminderStatus status) => status switch
    {
        VPureLux.Warranty.AssetReplacementReminderStatus.Pending => "text-bg-warning text-dark",
        VPureLux.Warranty.AssetReplacementReminderStatus.Completed => "text-bg-success",
        VPureLux.Warranty.AssetReplacementReminderStatus.Skipped => "text-bg-secondary",
        VPureLux.Warranty.AssetReplacementReminderStatus.Cancelled => "text-bg-dark",
        _ => "text-bg-light text-dark"
    };

    public sealed record WarrantyReminderRow(
        Guid Id,
        string AssetNo,
        string Customer,
        string Product,
        string Component,
        string Quantity,
        string DueDate,
        string DueDateIso,
        int CycleMonths,
        int WarningDaysBeforeDue,
        string StatusLabel,
        string StatusBadgeClass,
        string OrderContext,
        string? Note,
        bool IsPending);
}
