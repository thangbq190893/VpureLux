using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using VPureLux.Permissions;
using VPureLux.Service;
using Volo.Abp;

namespace VPureLux.Web.Pages.Service;

[Authorize(VPureLuxPermissions.Service.Default)]
[Authorize(VPureLuxPermissions.Service.Cancel)]
public class CancelModalModel : VPureLuxPageModel
{
    private readonly IServiceOrderAppService _service;
    private readonly IOptions<ServiceOptions> _options;

    [BindProperty(SupportsGet = true)] public Guid Id { get; set; }
    [BindProperty] public CancelForm Input { get; set; } = new();

    public CancelModalModel(IServiceOrderAppService service, IOptions<ServiceOptions> options)
    {
        _service = service;
        _options = options;
    }

    public IActionResult OnGet(string concurrencyStamp)
    {
        if (!_options.Value.IsEnabled) return NotFound();
        Input.ConcurrencyStamp = concurrencyStamp;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!_options.Value.IsEnabled) return NotFound();
        if (!ModelState.IsValid) return Page();
        try
        {
            await _service.CancelAsync(Id, new CancelServiceOrderDto
            {
                Reason = Input.Reason,
                ConcurrencyStamp = Input.ConcurrencyStamp
            });
            return NoContent();
        }
        catch (BusinessException exception)
        {
            ModelState.AddModelError(string.Empty, L[exception.Code ?? VPureLuxDomainErrorCodes.ValidationFailed]);
            return Page();
        }
    }

    public class CancelForm
    {
        [Required, StringLength(ServiceConsts.MaxNoteLength)] public string Reason { get; set; } = string.Empty;
        [Required, StringLength(40)] public string ConcurrencyStamp { get; set; } = string.Empty;
    }
}
