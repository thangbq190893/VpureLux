using System;
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
[Authorize(VPureLuxPermissions.Service.Edit)]
public class EditModel : VPureLuxPageModel
{
    private readonly IServiceOrderAppService _service;
    private readonly IOptions<ServiceOptions> _options;

    [BindProperty(SupportsGet = true)] public Guid Id { get; set; }
    [BindProperty] public ServiceOrderForm Input { get; set; } = new();
    public ServiceOrderPageViewModel FormView { get; private set; } = new();

    public EditModel(IServiceOrderAppService service, IOptions<ServiceOptions> options)
    {
        _service = service;
        _options = options;
    }

    public async Task<IActionResult> OnGetAsync()
    {
        if (!_options.Value.IsEnabled) return NotFound();
        var order = await _service.GetAsync(Id);
        if (order.Status != ServiceOrderStatus.Draft)
        {
            return RedirectToPage("/Service/Details", new { id = Id });
        }

        Input = ToForm(order);
        var positions = (await _service.GetAssetPositionOptionsAsync(order.CustomerAssetId))
            .ToDictionary(item => item.Id, item => $"{item.PositionCode} - {item.PositionName}");
        foreach (var line in Input.Lines.Where(line => line.CustomerAssetComponentId.HasValue))
        {
            if (positions.TryGetValue(line.CustomerAssetComponentId!.Value, out var label))
            {
                line.PositionLabel = label;
            }
        }
        await LoadFormAsync(order.OrderNo);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!_options.Value.IsEnabled) return NotFound();
        if (Input.Lines.Count == 0) ModelState.AddModelError(string.Empty, L["Service:AtLeastOneLine"]);
        if (!ModelState.IsValid)
        {
            await LoadFormAsync(null);
            return Page();
        }

        try
        {
            await _service.UpdateAsync(Id, Input.ToUpdateInput());
            return RedirectToPage("/Service/Details", new { id = Id });
        }
        catch (BusinessException exception)
        {
            ModelState.AddModelError(string.Empty, L[exception.Code ?? VPureLuxDomainErrorCodes.ValidationFailed]);
            await LoadFormAsync(null);
            return Page();
        }
    }

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
                id = item.Id, text = $"{item.Code} - {item.Name} ({item.Unit})", price = item.SuggestedPrice
            }).Cast<object>());
        });

    public Task<JsonResult> OnGetWorksAsync(string? searchText, int page = 1) =>
        LookupAsync(searchText, page, async input =>
        {
            var result = await _service.GetWorkOptionsAsync(input);
            return (result.TotalCount, result.Items.Select(item => new
            {
                id = item.Id, text = $"{item.Code} - {item.Name} ({item.Unit})", price = item.DefaultPrice
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
        Func<ServiceLookupInput, Task<(long TotalCount, System.Collections.Generic.IEnumerable<object> Items)>> query)
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

    private async Task LoadFormAsync(string? knownOrderNo)
    {
        var warehouses = await _service.GetWarehouseOptionsAsync();
        FormView = new ServiceOrderPageViewModel
        {
            Input = Input,
            IsEdit = true,
            OrderId = Id,
            OrderNo = knownOrderNo,
            Warehouses = warehouses.Select(item =>
                new SelectListItem($"{item.Code} - {item.Name}", item.Id.ToString(), item.Id == Input.WarehouseId)).ToList()
        };
    }

    private static ServiceOrderForm ToForm(ServiceOrderDto order) => new()
    {
        CustomerAssetId = order.CustomerAssetId,
        AssetLabel = $"{order.CustomerCode} - {order.CustomerName} | {order.AssetNo} - {order.AssetName}",
        WarehouseId = order.WarehouseId,
        OrderDate = order.OrderDate,
        ScheduledAt = order.ScheduledAt,
        TechnicianUserId = order.TechnicianUserId,
        TechnicianLabel = order.TechnicianName,
        ServiceAddress = order.ServiceAddress,
        Note = order.Note,
        ConcurrencyStamp = order.ConcurrencyStamp,
        Lines = order.Lines.Select(line => new ServiceOrderLineForm
        {
            Id = line.Id,
            LineType = line.LineType,
            CatalogItemId = line.ComponentId ?? line.ServiceWorkId ?? Guid.Empty,
            CustomerAssetComponentId = line.CustomerAssetComponentId,
            Quantity = line.PlannedQuantity,
            UnitPrice = line.UnitPrice,
            Note = line.Note,
            ItemLabel = $"{line.ItemCode} - {line.ItemName} ({line.Unit})"
        }).ToList()
    };
}
