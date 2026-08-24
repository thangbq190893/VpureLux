using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VPureLux.Permissions;
using VPureLux.Warranty;
using Volo.Abp.Application.Dtos;

namespace VPureLux.Web.Pages.Warranty;

[Authorize(VPureLuxPermissions.Warranty.ManageMachines)]
public class MachinesModel : VPureLuxPageModel
{
    private readonly IWarrantyAppService _warrantyAppService;

    public MachinesModel(IWarrantyAppService warrantyAppService)
    {
        _warrantyAppService = warrantyAppService;
    }

    public void OnGet()
    {
    }

    public async Task<JsonResult> OnGetListAsync(GetProductMachineSettingListInput input)
    {
        var result = await _warrantyAppService.GetMachineSettingListAsync(input);
        return new JsonResult(new PagedResultDto<MachineSettingRow>(
            result.TotalCount,
            result.Items.Select(item => new MachineSettingRow(
                item.ProductId,
                item.ProductCode,
                item.ProductName,
                item.ProductStatus.ToString(),
                item.SettingId,
                item.IsMachine,
                item.Note)).ToList()));
    }

    public sealed record MachineSettingRow(
        Guid ProductId,
        string ProductCode,
        string ProductName,
        string ProductStatus,
        Guid? SettingId,
        bool IsMachine,
        string? Note);
}
