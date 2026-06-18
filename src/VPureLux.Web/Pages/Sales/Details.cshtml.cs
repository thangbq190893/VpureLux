using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VPureLux.Catalog.Products;
using VPureLux.Permissions;
using VPureLux.Sales;
using Volo.Abp;

namespace VPureLux.Web.Pages.Sales;

[Authorize(VPureLuxPermissions.Sales.View)]
public class DetailsModel : VPureLuxPageModel
{
    private readonly ISalesOrderAppService _service;
    private readonly IAuthorizationService _authorizationService;
    private readonly IProductAppService _products;
    [BindProperty(SupportsGet = true)] public Guid Id { get; set; }
    [BindProperty] public ConfirmSalesOrderDto Confirmation { get; set; } = new() { IdempotencyKey = Guid.NewGuid().ToString("N") };
    [TempData] public string? SuccessMessage { get; set; }
    public SalesOrderDto Order { get; private set; } = new();
    public bool CanEdit { get; private set; }
    public bool CanConfirm { get; private set; }
    public bool CanCancel { get; private set; }
    public Dictionary<Guid, string> ProductLabels { get; private set; } = new();

    public DetailsModel(
        ISalesOrderAppService service,
        IAuthorizationService authorizationService,
        IProductAppService products)
    {
        _service = service;
        _authorizationService = authorizationService;
        _products = products;
    }

    public async Task OnGetAsync() => await LoadAsync();
    public async Task<IActionResult> OnPostConfirmAsync()
    {
        try
        {
            await _service.ConfirmAsync(Id, Confirmation);
            SuccessMessage = L["Sales:ConfirmedSuccessfully"];
            return RedirectToPage(new { id = Id });
        }
        catch (BusinessException exception)
        {
            ModelState.AddModelError(string.Empty, GetFriendlyErrorMessage(exception));
            await LoadAsync();
            return Page();
        }
    }

    public async Task<IActionResult> OnPostCancelAsync()
    {
        try
        {
            await _service.CancelAsync(Id);
            SuccessMessage = L["Sales:CancelledSuccessfully"];
            return RedirectToPage(new { id = Id });
        }
        catch (BusinessException exception)
        {
            ModelState.AddModelError(string.Empty, GetFriendlyErrorMessage(exception));
            await LoadAsync();
            return Page();
        }
    }

    public string GetProductLabel(SalesOrderLineDto line)
    {
        if (!string.IsNullOrWhiteSpace(line.ItemCodeSnapshot) || !string.IsNullOrWhiteSpace(line.ItemNameSnapshot))
        {
            return $"{line.ItemCodeSnapshot} - {line.ItemNameSnapshot}".Trim(' ', '-');
        }

        return ProductLabels.TryGetValue(line.ProductId, out var product)
            ? product
            : L["Sales:ProductContextUnavailable"];
    }

    private async Task LoadAsync()
    {
        Order = await _service.GetAsync(Id);
        ProductLabels = (await _products.GetListAsync(new GetProductListInput { MaxResultCount = 1000 })).Items
            .Where(x => Order.Lines.Any(line => line.ProductId == x.Id))
            .ToDictionary(x => x.Id, x => $"{x.Code} - {x.Name}");
        var draft = Order.Status == SalesOrderStatus.Draft;
        CanEdit = draft && (await _authorizationService.AuthorizeAsync(User, VPureLuxPermissions.Sales.Edit)).Succeeded;
        CanConfirm = draft && (await _authorizationService.AuthorizeAsync(User, VPureLuxPermissions.Sales.Confirm)).Succeeded;
        CanCancel = draft && (await _authorizationService.AuthorizeAsync(User, VPureLuxPermissions.Sales.Cancel)).Succeeded;
    }

    private string GetFriendlyErrorMessage(BusinessException exception)
    {
        return string.IsNullOrWhiteSpace(exception.Code)
            ? exception.Message
            : L[exception.Code].Value;
    }
}
