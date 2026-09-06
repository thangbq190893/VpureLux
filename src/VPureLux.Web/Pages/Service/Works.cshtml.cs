using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using VPureLux.Permissions;
using VPureLux.Service;

namespace VPureLux.Web.Pages.Service;

[Authorize(VPureLuxPermissions.Service.Default)]
[Authorize(VPureLuxPermissions.Service.View)]
public class WorksModel : VPureLuxPageModel
{
    private readonly IServiceWorkAppService _service;
    private readonly IOptions<ServiceOptions> _options;
    public WorksModel(IServiceWorkAppService service, IOptions<ServiceOptions> options)
    {
        _service = service;
        _options = options;
    }
    public IActionResult OnGet() => _options.Value.IsEnabled ? Page() : NotFound();
    public async Task<IActionResult> OnGetListAsync(GetServiceWorkListInput input) =>
        new JsonResult(await _service.GetListAsync(input));
}
