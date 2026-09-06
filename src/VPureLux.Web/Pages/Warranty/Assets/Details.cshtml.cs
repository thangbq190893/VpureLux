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

namespace VPureLux.Web.Pages.Warranty.Assets;

[Authorize(VPureLuxPermissions.Warranty.ManageAssets)]
public class DetailsModel : VPureLuxPageModel
{
    private readonly IWarrantyAppService _warranty;
    private readonly IStringLocalizer<VPureLuxResource> _localizer;
    private static readonly CultureInfo Vi = CultureInfo.GetCultureInfo("vi-VN");

    [BindProperty(SupportsGet = true)]
    public Guid Id { get; set; }

    public CustomerAssetDetailDto Asset { get; private set; } = new();

    public DetailsModel(IWarrantyAppService warranty, IStringLocalizer<VPureLuxResource> localizer)
    {
        _warranty = warranty;
        _localizer = localizer;
    }

    public async Task OnGetAsync()
    {
        Asset = await _warranty.GetAssetDetailsAsync(Id);
    }

    public async Task<JsonResult> OnGetHistoryAsync(GetAssetMaintenanceHistoryInput input)
    {
        input.CustomerAssetId = Id;
        var result = await _warranty.GetAssetHistoryAsync(input);
        return new JsonResult(new Volo.Abp.Application.Dtos.PagedResultDto<HistoryRow>(
            result.TotalCount,
            result.Items.Select(item => new HistoryRow(
                item.Id,
                FormatEventTime(item),
                _localizer[$"Warranty:EventType:{item.EventType}"].Value,
                _localizer[$"Warranty:EventSource:{item.SourceType}"].Value,
                item.ComponentCode,
                item.ComponentName,
                item.Note)).ToList()));
    }

    private static string FormatEventTime(AssetMaintenanceEventListDto item)
    {
        // Only S-003 facts are UTC instants; do not reinterpret historical local-wall-time rows.
        var isServiceInstant = item.SourceType == AssetMaintenanceSourceType.ServiceOrder &&
            (item.EventType == AssetMaintenanceEventType.ServiceCompleted || item.ServiceOrderLineId.HasValue);
        return (isServiceInstant ? item.OccurredAt.AddHours(7) : item.OccurredAt).ToString("dd/MM/yyyy HH:mm", Vi);
    }

    public sealed record HistoryRow(
        Guid Id, string OccurredAt, string EventType, string SourceType,
        string? ComponentCode, string? ComponentName, string? Note);
}
