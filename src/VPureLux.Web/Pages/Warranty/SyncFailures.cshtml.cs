using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using VPureLux.Localization;
using VPureLux.Permissions;
using VPureLux.Warranty;
using Volo.Abp.Application.Dtos;

namespace VPureLux.Web.Pages.Warranty;

[Authorize(VPureLuxPermissions.Warranty.ManageSyncFailures)]
public class SyncFailuresModel : VPureLuxPageModel
{
    private static readonly CultureInfo Vi = CultureInfo.GetCultureInfo("vi-VN");
    private readonly IWarrantyAppService _warrantyAppService;
    private readonly IStringLocalizer<VPureLuxResource> _localizer;

    public SyncFailuresModel(
        IWarrantyAppService warrantyAppService,
        IStringLocalizer<VPureLuxResource> localizer)
    {
        _warrantyAppService = warrantyAppService;
        _localizer = localizer;
    }

    public void OnGet()
    {
    }

    public async Task<JsonResult> OnGetListAsync(GetCustomerCareSyncFailureListInput input)
    {
        var result = await _warrantyAppService.GetSyncFailureListAsync(input);
        return new JsonResult(new PagedResultDto<SyncFailureRow>(
            result.TotalCount,
            result.Items.Select(item => new SyncFailureRow(
                item.Id,
                item.OrderNo,
                item.LineNo,
                item.ProductCode,
                item.ProductName,
                item.Quantity,
                item.ErrorCode,
                item.ErrorMessage,
                item.ErrorContext,
                item.AttemptCount,
                item.LastOccurredAt.ToString("dd/MM/yyyy HH:mm", Vi),
                item.NextRetryAt?.ToString("dd/MM/yyyy HH:mm", Vi),
                _localizer[$"Warranty:SyncStatus:{item.Status}"].Value,
                item.Status)).ToList()));
    }

    public async Task<JsonResult> OnPostRetryAsync(Guid id)
    {
        await _warrantyAppService.RetrySyncFailureAsync(id);
        return new JsonResult(new { success = true });
    }

    public sealed record SyncFailureRow(
        Guid Id,
        string OrderNo,
        int? LineNo,
        string ProductCode,
        string ProductName,
        decimal? Quantity,
        string? ErrorCode,
        string ErrorMessage,
        string? ErrorContext,
        int AttemptCount,
        string LastOccurredAt,
        string? NextRetryAt,
        string StatusLabel,
        CustomerCareSyncFailureStatus Status);
}
