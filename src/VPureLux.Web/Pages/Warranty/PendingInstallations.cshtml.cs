using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VPureLux.Permissions;
using VPureLux.Warranty;
using Volo.Abp.Application.Dtos;

namespace VPureLux.Web.Pages.Warranty;

[Authorize(VPureLuxPermissions.Warranty.ManageInstallations)]
public class PendingInstallationsModel : VPureLuxPageModel
{
    private static readonly CultureInfo Vi = CultureInfo.GetCultureInfo("vi-VN");
    private readonly IWarrantyAppService _warrantyAppService;

    public PendingInstallationsModel(IWarrantyAppService warrantyAppService)
    {
        _warrantyAppService = warrantyAppService;
    }

    public void OnGet()
    {
    }

    public async Task<JsonResult> OnGetListAsync(GetPendingInstallationListInput input)
    {
        var result = await _warrantyAppService.GetPendingInstallationListAsync(input);
        return new JsonResult(new PagedResultDto<PendingInstallationRow>(
            result.TotalCount,
            result.Items.Select(item => new PendingInstallationRow(
                item.Id,
                item.AssetNo,
                item.CustomerCode,
                item.CustomerName,
                item.ProductCode,
                item.ProductName,
                item.OrderNo,
                item.LineNo,
                item.UnitIndex,
                item.SoldDate?.ToString("dd/MM/yyyy", Vi),
                item.PositionCount)).ToList()));
    }

    public sealed record PendingInstallationRow(
        Guid Id,
        string AssetNo,
        string CustomerCode,
        string CustomerName,
        string ProductCode,
        string ProductName,
        string OrderNo,
        int? LineNo,
        int? UnitIndex,
        string? SoldDate,
        int PositionCount);
}
