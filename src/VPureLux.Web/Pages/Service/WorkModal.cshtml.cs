using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using VPureLux.Permissions;
using VPureLux.Service;

namespace VPureLux.Web.Pages.Service;

[Authorize(VPureLuxPermissions.Service.Default)]
[Authorize(VPureLuxPermissions.Service.View)]
[Authorize(VPureLuxPermissions.Service.ManageWorks)]
public class WorkModalModel : VPureLuxPageModel
{
    private readonly IServiceWorkAppService _service;
    private readonly IOptions<ServiceOptions> _options;
    [BindProperty(SupportsGet = true)] public Guid? Id { get; set; }
    [BindProperty] public ServiceWorkForm Input { get; set; } = new();

    public WorkModalModel(IServiceWorkAppService service, IOptions<ServiceOptions> options)
    {
        _service = service;
        _options = options;
    }
    public async Task<IActionResult> OnGetAsync()
    {
        if (!_options.Value.IsEnabled) return NotFound();
        if (Id.HasValue)
        {
            var work = await _service.GetAsync(Id.Value);
            Input = new ServiceWorkForm
            {
                Code = work.Code, Name = work.Name, Unit = work.Unit ?? string.Empty,
                DefaultPrice = work.DefaultPrice, StandardCost = work.StandardCost,
                Status = work.Status, Note = work.Note, ConcurrencyStamp = work.ConcurrencyStamp
            };
        }
        return Page();
    }
    public async Task<IActionResult> OnPostAsync()
    {
        if (!_options.Value.IsEnabled) return NotFound();
        if (!ModelState.IsValid) return Page();
        if (Id.HasValue) await _service.UpdateAsync(Id.Value, Input.ToInput());
        else await _service.CreateAsync(Input.ToInput());
        return NoContent();
    }
}
