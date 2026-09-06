using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Options;
using VPureLux.Permissions;
using VPureLux.Service;
using Volo.Abp;

namespace VPureLux.Web.Pages.Service;

[Authorize(VPureLuxPermissions.Service.Default)]
[Authorize(VPureLuxPermissions.Service.Create)]
public class CreateModel : VPureLuxPageModel
{
    private readonly IServiceOrderAppService _service;
    private readonly IOptions<ServiceOptions> _options;

    [BindProperty] public ServiceOrderForm Input { get; set; } = new();
    [BindProperty(SupportsGet = true)] public Guid? AssetId { get; set; }
    public ServiceOrderPageViewModel FormView { get; private set; } = new();

    public CreateModel(IServiceOrderAppService service, IOptions<ServiceOptions> options)
    {
        _service = service;
        _options = options;
    }

    public async Task<IActionResult> OnGetAsync()
    {
        if (!_options.Value.IsEnabled) return NotFound();
        Input.CustomerAssetId = AssetId ?? Guid.Empty;
        Input.OrderDate = Clock.Now.Date;
        Input.ScheduledAt = Clock.Now.AddDays(1);
        await LoadFormAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!_options.Value.IsEnabled) return NotFound();
        if (Input.Lines.Count == 0) ModelState.AddModelError(string.Empty, L["Service:AtLeastOneLine"]);
        if (!ModelState.IsValid)
        {
            await LoadFormAsync();
            return Page();
        }

        try
        {
            var order = await _service.CreateAsync(Input.ToCreateInput());
            return RedirectToPage("/Service/Details", new { id = order.Id });
        }
        catch (BusinessException exception)
        {
            ModelState.AddModelError(string.Empty, L[exception.Code ?? VPureLuxDomainErrorCodes.ValidationFailed]);
            await LoadFormAsync();
            return Page();
        }
    }

    public Task<JsonResult> OnGetAssetsAsync(string? searchText, int page = 1) =>
        LookupAsync(searchText, page, async input =>
        {
            var result = await _service.GetAssetOptionsAsync(input);
            return (result.TotalCount, result.Items.Select(item => new
            {
                id = item.Id,
                text = $"{item.CustomerCode} - {item.CustomerName} | {item.AssetNo} - {item.AssetName}",
                address = item.ServiceAddress
            }).Cast<object>());
        });

    public async Task<JsonResult> OnGetPositionsAsync(Guid assetId)
    {
        var items = await _service.GetAssetPositionOptionsAsync(assetId);
        return new JsonResult(items.Select(item => new
        {
            id = item.Id,
            text = $"{item.PositionCode} - {item.PositionName}",
            componentId = item.ComponentId
        }));
    }

    public Task<JsonResult> OnGetMaterialsAsync(string? searchText, int page = 1) =>
        LookupAsync(searchText, page, async input =>
        {
            var result = await _service.GetMaterialOptionsAsync(input);
            return (result.TotalCount, result.Items.Select(item => new
            {
                id = item.Id,
                text = $"{item.Code} - {item.Name} ({item.Unit})",
                price = item.SuggestedPrice
            }).Cast<object>());
        });

    public Task<JsonResult> OnGetWorksAsync(string? searchText, int page = 1) =>
        LookupAsync(searchText, page, async input =>
        {
            var result = await _service.GetWorkOptionsAsync(input);
            return (result.TotalCount, result.Items.Select(item => new
            {
                id = item.Id,
                text = $"{item.Code} - {item.Name} ({item.Unit})",
                price = item.DefaultPrice
            }).Cast<object>());
        });

    public Task<JsonResult> OnGetTechniciansAsync(string? searchText, int page = 1) =>
        LookupAsync(searchText, page, async input =>
        {
            var result = await _service.GetTechnicianOptionsAsync(input);
            return (result.TotalCount, result.Items.Select(item => new { id = item.Id, text = item.Name }).Cast<object>());
        });

    private async Task<JsonResult> LookupAsync(
        string? searchText,
        int page,
        Func<ServiceLookupInput, Task<(long TotalCount, IEnumerable<object> Items)>> query)
    {
        const int pageSize = 20;
        page = Math.Max(page, 1);
        var result = await query(new ServiceLookupInput
        {
            SearchText = searchText,
            SkipCount = (page - 1) * pageSize,
            MaxResultCount = pageSize
        });
        return new JsonResult(new
        {
            results = result.Items,
            pagination = new { more = page * pageSize < result.TotalCount }
        });
    }

    private async Task LoadFormAsync()
    {
        if (Input.CustomerAssetId != Guid.Empty && string.IsNullOrWhiteSpace(Input.AssetLabel))
        {
            var asset = await _service.GetAssetOptionAsync(Input.CustomerAssetId);
            Input.AssetLabel = $"{asset.CustomerCode} - {asset.CustomerName} | {asset.AssetNo} - {asset.AssetName}";
        }

        var warehouses = await _service.GetWarehouseOptionsAsync();
        if (Input.WarehouseId == Guid.Empty)
        {
            Input.WarehouseId = warehouses.FirstOrDefault(item => item.IsDefault)?.Id ?? warehouses.FirstOrDefault()?.Id ?? Guid.Empty;
        }

        FormView = new ServiceOrderPageViewModel
        {
            Input = Input,
            Warehouses = warehouses.Select(item =>
                new SelectListItem($"{item.Code} - {item.Name}", item.Id.ToString(), item.Id == Input.WarehouseId)).ToList()
        };
    }
}
