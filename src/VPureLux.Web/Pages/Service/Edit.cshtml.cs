using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VPureLux.Permissions;
using VPureLux.Service;
using Volo.Abp;

namespace VPureLux.Web.Pages.Service;

[Authorize(VPureLuxPermissions.Service.Edit)]
public class EditModel : VPureLuxPageModel
{
    private readonly IServiceAppService _service;
    [BindProperty(SupportsGet = true)] public Guid Id { get; set; }
    [BindProperty] public UpdateServiceOrderDto Input { get; set; } = new();
    public ServiceOrderDto Order { get; private set; } = new();
    public string WarehouseLabel { get; private set; } = string.Empty;
    public EditModel(IServiceAppService service) => _service = service;
    public async Task<IActionResult> OnGetAsync() { await LoadAsync(true); return Order.Status == ServiceOrderStatus.Draft ? Page() : RedirectToPage("/Service/Details", new { id = Id }); }
    public async Task<IActionResult> OnPostAsync() { if (Input.Lines.Count == 0) ModelState.AddModelError(string.Empty, L["Service:NoLines"]); if (!ModelState.IsValid) { await LoadAsync(false); return Page(); } try { await _service.UpdateAsync(Id, Input); return RedirectToPage("/Service/Details", new { id = Id }); } catch (BusinessException exception) { ModelState.AddModelError(string.Empty, L[exception.Code ?? VPureLuxDomainErrorCodes.ValidationFailed]); await LoadAsync(false); return Page(); } }
    public async Task<JsonResult> OnGetPositionsAsync(Guid assetId) { var items = await _service.GetAssetPositionOptionsAsync(assetId); return new JsonResult(items.Select(x => new { id = x.Id, text = $"{x.PositionCode} - {x.PositionName}", componentId = x.ComponentId })); }
    public async Task<JsonResult> OnGetMaterialsAsync(string? searchText) { var items = await _service.GetMaterialOptionsAsync(new ServiceLookupInput { SearchText = searchText, MaxResultCount = 30 }); return new JsonResult(new { results = items.Select(x => new { id = x.ComponentId, text = $"{x.Code} - {x.Name} ({x.Unit})", price = x.SuggestedPrice ?? 0 }) }); }
    public async Task<JsonResult> OnGetWorksAsync(string? searchText) { var items = await _service.GetWorkOptionsAsync(new ServiceLookupInput { SearchText = searchText, MaxResultCount = 30 }); return new JsonResult(new { results = items.Select(x => new { id = x.Id, text = $"{x.Code} - {x.Name}", price = x.DefaultPrice }) }); }
    private async Task LoadAsync(bool setInput) { Order = await _service.GetAsync(Id); var warehouses = await _service.GetWarehouseOptionsAsync(); WarehouseLabel = warehouses.Where(x => x.Id == Order.WarehouseId).Select(x => $"{x.Code} - {x.Name}").FirstOrDefault() ?? Order.WarehouseId.ToString(); if (setInput) Input = new UpdateServiceOrderDto { ScheduledAt = Order.ScheduledAt, TechnicianUserId = Order.TechnicianUserId, ServiceAddress = Order.ServiceAddress, Note = Order.Note, Lines = Order.Lines.Select(x => new ServiceOrderLineInput { LineType = x.LineType, CatalogItemId = x.ComponentId ?? x.ServiceWorkId ?? Guid.Empty, CustomerAssetComponentId = x.CustomerAssetComponentId, Quantity = x.PlannedQuantity, UnitPrice = x.UnitPrice, Note = x.Note }).ToList() }; }
}
