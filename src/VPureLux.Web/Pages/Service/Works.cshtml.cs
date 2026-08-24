using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VPureLux.Permissions;
using VPureLux.Service;
using Volo.Abp.Application.Dtos;

namespace VPureLux.Web.Pages.Service;

[Authorize(VPureLuxPermissions.Service.ManageWorks)]
public class WorksModel : VPureLuxPageModel
{
    private static readonly CultureInfo Vi = CultureInfo.GetCultureInfo("vi-VN");
    private readonly IServiceWorkAppService _service;
    public WorksModel(IServiceWorkAppService service) => _service = service;
    public async Task<JsonResult> OnGetListAsync(GetServiceWorkListInput input) { var result = await _service.GetListAsync(input); return new JsonResult(new PagedResultDto<object>(result.TotalCount, result.Items.Select(x => (object)new { x.Id, x.Code, x.Name, DefaultPrice = x.DefaultPrice.ToString("N0", Vi), Status = L[$"Service:Status:{x.Status}"].Value, IsActive = x.Status == ServiceWorkStatus.Active }).ToList())); }
    public async Task<IActionResult> OnPostSetActiveAsync(Guid id, bool isActive) { await _service.SetActiveAsync(id, isActive); return new JsonResult(new { success = true }); }
}
