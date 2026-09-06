using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using VPureLux.Permissions;
using VPureLux.Service;
using Volo.Abp;

namespace VPureLux.Web.Pages.Service;

[Authorize(VPureLuxPermissions.Service.Default)]
[Authorize(VPureLuxPermissions.Service.Complete)]
public class CompleteModalModel(IServiceOrderAppService service, IOptions<ServiceOptions> options) : VPureLuxPageModel
{
    [BindProperty(SupportsGet = true)] public Guid Id { get; set; }
    [BindProperty] public CompletionForm Input { get; set; } = new();
    public ServiceOrderDto Order { get; private set; } = new();

    public async Task<IActionResult> OnGetAsync()
    {
        if (!options.Value.IsEnabled) return NotFound();
        Order = await service.GetAsync(Id);
        if (Order.Status != ServiceOrderStatus.InProgress) return BadRequest();
        Input.ConcurrencyStamp = Order.ConcurrencyStamp;
        Input.IdempotencyKey = Guid.NewGuid().ToString("N");
        Input.CompletedAt = DateTimeOffset.UtcNow.ToOffset(TimeSpan.FromHours(7)).ToString("yyyy-MM-ddTHH:mm", CultureInfo.InvariantCulture);
        Input.Lines = Order.Lines.Select(x => new CompleteServiceLineDto { LineId = x.Id, ActualQuantity = x.PlannedQuantity }).ToList();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!options.Value.IsEnabled) return NotFound();
        Order = await service.GetAsync(Id);
        if (!DateTime.TryParseExact(Input.CompletedAt, "yyyy-MM-ddTHH:mm", CultureInfo.InvariantCulture,
                DateTimeStyles.None, out var localTime))
            ModelState.AddModelError("Input.CompletedAt", L[ServiceErrorCodes.InvalidCompletion]);
        if (!ModelState.IsValid) return Page();
        try
        {
            await service.CompleteAsync(Id, new CompleteServiceOrderDto
            {
                ConcurrencyStamp = Input.ConcurrencyStamp, IdempotencyKey = Input.IdempotencyKey,
                CompletedAt = new DateTimeOffset(localTime, TimeSpan.FromHours(7)), Lines = Input.Lines
            });
            return NoContent();
        }
        catch (BusinessException exception)
        {
            var message = exception.Code == ServiceErrorCodes.StockShortage
                ? L["Service:ShortageDetail", exception.Data["OrderNo"]!, exception.Data["Line"]!,
                    exception.Data["Component"]!, exception.Data["Warehouse"]!, exception.Data["Requested"]!, exception.Data["Available"]!]
                : L[exception.Code ?? VPureLuxDomainErrorCodes.ValidationFailed];
            ModelState.AddModelError(string.Empty, message);
            return Page();
        }
    }

    public class CompletionForm
    {
        [Required, StringLength(40)] public string ConcurrencyStamp { get; set; } = string.Empty;
        [Required, StringLength(64)] public string IdempotencyKey { get; set; } = string.Empty;
        [Required] public string CompletedAt { get; set; } = string.Empty;
        [Required, MinLength(1)] public List<CompleteServiceLineDto> Lines { get; set; } = [];
    }
}
