using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VPureLux.Permissions;
using VPureLux.Service;
using Volo.Abp;

namespace VPureLux.Web.Pages.Service;

[Authorize(VPureLuxPermissions.Service.Complete)]
public class CompleteModalModel : VPureLuxPageModel
{
    private readonly IServiceAppService _service;
    [BindProperty(SupportsGet = true)] public Guid Id { get; set; }
    [BindProperty] public CompleteServiceOrderDto Input { get; set; } = new();
    public List<ServiceOrderLineDto> Lines { get; private set; } = [];
    public CompleteModalModel(IServiceAppService service) => _service = service;
    public async Task OnGetAsync() { var order = await _service.GetAsync(Id); Lines = order.Lines; Input.CompletedAt = Clock.Now; Input.IdempotencyKey = Guid.NewGuid().ToString("N"); Input.Lines = Lines.Select(x => new CompleteServiceOrderLineDto { LineId = x.Id, ActualQuantity = x.PlannedQuantity }).ToList(); }
    public async Task<IActionResult> OnPostAsync()
    {
        var order = await _service.GetAsync(Id); Lines = order.Lines;
        if (!ModelState.IsValid) return Page();
        try { await _service.CompleteAsync(Id, Input); return NoContent(); }
        catch (BusinessException exception) { ModelState.AddModelError(string.Empty, L[exception.Code ?? VPureLuxDomainErrorCodes.ValidationFailed]); return Page(); }
    }
}
