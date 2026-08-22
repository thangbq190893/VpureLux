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

[Authorize(VPureLuxPermissions.Warranty.ManagePolicies)]
public class PoliciesModel : VPureLuxPageModel
{
    private readonly VPureLux.Warranty.IWarrantyAppService _warrantyAppService;
    private readonly IStringLocalizer<VPureLuxResource> _localizer;

    public PoliciesModel(
        VPureLux.Warranty.IWarrantyAppService warrantyAppService,
        IStringLocalizer<VPureLuxResource> localizer)
    {
        _warrantyAppService = warrantyAppService;
        _localizer = localizer;
    }

    public void OnGet()
    {
    }

    public async Task<JsonResult> OnGetListAsync(VPureLux.Warranty.GetWarrantyPolicyListInput input)
    {
        var result = await _warrantyAppService.GetPolicyListAsync(input);
        return new JsonResult(new PagedResultDto<WarrantyPolicyRow>(
            result.TotalCount,
            result.Items.Select(ToRow).ToList()));
    }

    public async Task<JsonResult> OnPostSaveAsync(Guid componentId, bool isEnabled, int cycleMonths, int warningDaysBeforeDue, string? note)
    {
        await _warrantyAppService.SetPolicyAsync(
            componentId,
            new VPureLux.Warranty.SetComponentReplacementPolicyDto
            {
                IsEnabled = isEnabled,
                CycleMonths = cycleMonths,
                WarningDaysBeforeDue = warningDaysBeforeDue,
                Note = note
            });
        return new JsonResult(new { success = true });
    }

    private WarrantyPolicyRow ToRow(VPureLux.Warranty.WarrantyPolicyListDto policy) =>
        new(
            policy.ComponentId,
            policy.ComponentCode,
            policy.ComponentName,
            policy.ComponentUnit,
            policy.PolicyId,
            policy.IsEnabled,
            policy.CycleMonths,
            policy.WarningDaysBeforeDue,
            policy.CycleMonths.HasValue
                ? policy.CycleMonths.Value.ToString("#,0", CultureInfo.GetCultureInfo("vi-VN"))
                : _localizer["Warranty:NoPolicy"].Value,
            policy.WarningDaysBeforeDue.HasValue
                ? policy.WarningDaysBeforeDue.Value.ToString("#,0", CultureInfo.GetCultureInfo("vi-VN"))
                : _localizer["Warranty:NoPolicy"].Value,
            policy.Note);

    public sealed record WarrantyPolicyRow(
        Guid ComponentId,
        string ComponentCode,
        string ComponentName,
        string ComponentUnit,
        Guid? PolicyId,
        bool IsEnabled,
        int? CycleMonths,
        int? WarningDaysBeforeDue,
        string CycleMonthsText,
        string WarningDaysBeforeDueText,
        string? Note);
}
