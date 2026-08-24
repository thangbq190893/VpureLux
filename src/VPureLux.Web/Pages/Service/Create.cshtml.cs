using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using VPureLux.Permissions;
using VPureLux.Service;
using Volo.Abp;

namespace VPureLux.Web.Pages.Service;

[Authorize(VPureLuxPermissions.Service.Create)]
public class CreateModel : VPureLuxPageModel
{
    private readonly IServiceAppService _service;
    [BindProperty] public CreateServiceOrderDto Input { get; set; } = new();
    [BindProperty(SupportsGet = true)] public Guid? AssetId { get; set; }
    public List<SelectListItem> Warehouses { get; private set; } = [];
    public ServiceAssetOptionDto? SelectedAsset { get; private set; }

    public CreateModel(IServiceAppService service) => _service = service;

    public async Task OnGetAsync()
    {
        Input.CustomerAssetId = AssetId ?? Guid.Empty;
        Input.ScheduledAt = Clock.Now.AddDays(1);
        await LoadSelectionsAsync();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (Input.Lines.Count == 0) ModelState.AddModelError(string.Empty, L["Service:NoLines"]);
        if (!ModelState.IsValid)
        {
            await LoadSelectionsAsync();
            return Page();
        }
        try
        {
            var order = await _service.CreateAsync(Input);
            return RedirectToPage("/Service/Details", new { id = order.Id });
        }
        catch (BusinessException exception)
        {
            ModelState.AddModelError(string.Empty, L[exception.Code ?? VPureLuxDomainErrorCodes.ValidationFailed]);
            await LoadSelectionsAsync();
            return Page();
        }
    }

    public async Task<JsonResult> OnGetAssetsAsync(string? searchText)
    {
        var items = await _service.GetAssetOptionsAsync(new ServiceLookupInput { SearchText = searchText, MaxResultCount = 30 });
        return new JsonResult(new { results = items.Select(x => new { id = x.Id, text = $"{x.CustomerCode} - {x.CustomerName} | {x.AssetNo} - {x.AssetName}", address = x.ServiceAddress }) });
    }

    public async Task<JsonResult> OnGetPositionsAsync(Guid assetId)
    {
        var items = await _service.GetAssetPositionOptionsAsync(assetId);
        return new JsonResult(items.Select(x => new { id = x.Id, text = $"{x.PositionCode} - {x.PositionName}", componentId = x.ComponentId }));
    }

    public async Task<JsonResult> OnGetMaterialsAsync(string? searchText)
    {
        var items = await _service.GetMaterialOptionsAsync(new ServiceLookupInput { SearchText = searchText, MaxResultCount = 30 });
        return new JsonResult(new { results = items.Select(x => new { id = x.ComponentId, text = $"{x.Code} - {x.Name} ({x.Unit})", price = x.SuggestedPrice ?? 0 }) });
    }

    public async Task<JsonResult> OnGetWorksAsync(string? searchText)
    {
        var items = await _service.GetWorkOptionsAsync(new ServiceLookupInput { SearchText = searchText, MaxResultCount = 30 });
        return new JsonResult(new { results = items.Select(x => new { id = x.Id, text = $"{x.Code} - {x.Name}", price = x.DefaultPrice }) });
    }

    private async Task LoadSelectionsAsync()
    {
        var warehouses = await _service.GetWarehouseOptionsAsync();
        Warehouses = warehouses.Select(x => new SelectListItem($"{x.Code} - {x.Name}", x.Id.ToString(), x.Id == Input.WarehouseId)).ToList();
        if (Input.WarehouseId == Guid.Empty) Input.WarehouseId = warehouses.FirstOrDefault(x => x.IsDefault)?.Id ?? warehouses.FirstOrDefault()?.Id ?? Guid.Empty;
        if (Input.CustomerAssetId != Guid.Empty)
        {
            SelectedAsset = await _service.GetAssetOptionAsync(Input.CustomerAssetId);
        }
    }
}
