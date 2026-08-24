using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VPureLux.Permissions;
using VPureLux.Service;
using Volo.Abp;

namespace VPureLux.Web.Pages.Service;

[Authorize(VPureLuxPermissions.Service.ManageWorks)]
public class WorkModalModel : VPureLuxPageModel
{
    private readonly IServiceWorkAppService _service;
    [BindProperty(SupportsGet = true)] public Guid? Id { get; set; }
    [BindProperty] public CreateUpdateServiceWorkDto Input { get; set; } = new();
    public WorkModalModel(IServiceWorkAppService service) => _service = service;
    public async Task OnGetAsync() { if (!Id.HasValue) return; var item = await _service.GetAsync(Id.Value); Input = new CreateUpdateServiceWorkDto { Code = item.Code, Name = item.Name, DefaultPrice = item.DefaultPrice, Note = item.Note }; }
    public async Task<IActionResult> OnPostAsync() { if (!ModelState.IsValid) return Page(); try { if (Id.HasValue) await _service.UpdateAsync(Id.Value, Input); else await _service.CreateAsync(Input); return NoContent(); } catch (BusinessException exception) { ModelState.AddModelError(string.Empty, L[exception.Code ?? VPureLuxDomainErrorCodes.ValidationFailed]); return Page(); } }
}
