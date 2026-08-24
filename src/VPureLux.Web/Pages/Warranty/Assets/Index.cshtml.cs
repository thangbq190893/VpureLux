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

namespace VPureLux.Web.Pages.Warranty.Assets;

[Authorize(VPureLuxPermissions.Warranty.ManageAssets)]
public class IndexModel : VPureLuxPageModel
{
    private static readonly CultureInfo Vi = CultureInfo.GetCultureInfo("vi-VN");
    private readonly IWarrantyAppService _warranty;
    private readonly IStringLocalizer<VPureLuxResource> _localizer;

    public IndexModel(IWarrantyAppService warranty, IStringLocalizer<VPureLuxResource> localizer)
    {
        _warranty = warranty;
        _localizer = localizer;
    }

    public void OnGet() { }

    public async Task<JsonResult> OnGetListAsync(GetCustomerAssetListInput input)
    {
        var result = await _warranty.GetAssetListAsync(input);
        return new JsonResult(new PagedResultDto<CustomerAssetRow>(result.TotalCount, result.Items.Select(item =>
            new CustomerAssetRow(
                item.Id, item.AssetNo, item.CustomerCode, item.CustomerName,
                item.ProductCode, item.ProductName, item.Brand, item.Model, item.SerialNo,
                item.Source.HasValue ? _localizer[$"Warranty:Source:{item.Source}"].Value : string.Empty,
                item.PositionCount, item.NextDueDate?.ToString("dd/MM/yyyy", Vi))).ToList()));
    }

    public sealed record CustomerAssetRow(
        Guid Id, string AssetNo, string CustomerCode, string CustomerName,
        string? ProductCode, string? ProductName, string? Brand, string? Model, string? SerialNo,
        string SourceLabel, int PositionCount, string? NextDueDate);
}
