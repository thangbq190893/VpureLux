using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using global::VPureLux.Audit;
using VPureLux.Permissions;

namespace VPureLux.Web.Pages.Audit;

[Authorize(VPureLuxPermissions.Audit.View)]
public class DetailsModel : VPureLuxPageModel
{
    private readonly IBusinessAuditAppService _service;
    [BindProperty(SupportsGet = true)] public Guid Id { get; set; }
    public BusinessAuditLogDto Log { get; private set; } = new();
    public DetailsModel(IBusinessAuditAppService service) => _service = service;
    public async Task OnGetAsync() => Log = await _service.GetAsync(Id);
}
