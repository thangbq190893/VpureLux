using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using global::VPureLux.Audit;
using VPureLux.Permissions;
namespace VPureLux.Web.Pages.Audit;
[Authorize(VPureLuxPermissions.Audit.Export)]
public class ExportModel : VPureLuxPageModel
{
    private readonly IBusinessAuditAppService _service;
    [BindProperty] public AuditSearchInput Input { get; set; } = new();
    public ExportModel(IBusinessAuditAppService service) => _service = service;
    public void OnGet() { }
    public async Task<IActionResult> OnPostAsync()
    {
        var result = await _service.ExportAsync(Input);
        return File(result.Content, result.ContentType, result.FileName);
    }
}
