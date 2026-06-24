using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VPureLux.Catalog.Products;
using VPureLux.Permissions;
using VPureLux.Pricing;
using VPureLux.Sales;
using Volo.Abp;
using Volo.Abp.Authorization;

namespace VPureLux.Web.Pages.Sales;

[Authorize(VPureLuxPermissions.Sales.View)]
public class DetailsModel : VPureLuxPageModel
{
    private readonly ISalesOrderAppService _service;
    private readonly IAuthorizationService _authorizationService;
    private readonly IProductAppService _products;
    private readonly IProductPricingContextLookupService _productPricingContext;
    [BindProperty(SupportsGet = true)] public Guid Id { get; set; }
    [BindProperty] public ConfirmSalesOrderDto Confirmation { get; set; } = new() { IdempotencyKey = Guid.NewGuid().ToString("N") };
    [TempData] public string? SuccessMessage { get; set; }
    public SalesOrderDto Order { get; private set; } = new();
    public bool CanEdit { get; private set; }
    public bool CanConfirm { get; private set; }
    public bool CanCancel { get; private set; }
    public Dictionary<Guid, string> ProductLabels { get; private set; } = new();
    public Dictionary<Guid, SalesProductContextViewModel> ProductContexts { get; private set; } = new();

    public DetailsModel(
        ISalesOrderAppService service,
        IAuthorizationService authorizationService,
        IProductAppService products,
        IProductPricingContextLookupService productPricingContext)
    {
        _service = service;
        _authorizationService = authorizationService;
        _products = products;
        _productPricingContext = productPricingContext;
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
            ModelState.AddModelError(string.Empty, SalesUiFormatter.GetFriendlyErrorMessage(L, exception));
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
            ModelState.AddModelError(string.Empty, SalesUiFormatter.GetFriendlyErrorMessage(L, exception));
            await LoadAsync();
            return Page();
        }
    }

    public string GetProductLabel(SalesOrderLineDto line) =>
        SalesUiFormatter.GetProductLabel(line, ProductLabels, L);

    public string GetBomBadgeClass(SalesOrderLineDto line)
    {
        if (line.BomVersionNoSnapshot.HasValue)
        {
            return SalesUiFormatter.GetBomBadgeClass(true);
        }

        return ProductContexts.TryGetValue(line.ProductId, out var context)
            ? SalesUiFormatter.GetBomBadgeClass(context.HasPublishedBom)
            : "badge bg-secondary";
    }

    public string GetBomStatusText(SalesOrderLineDto line)
    {
        if (line.BomVersionNoSnapshot.HasValue)
        {
            return L["Sales:PublishedBomVersion", line.BomVersionNoSnapshot.Value];
        }

        return ProductContexts.TryGetValue(line.ProductId, out var context)
            ? context.BomStatusText
            : L["Sales:ProductContextUnavailable"];
    }

    private async Task LoadAsync()
    {
        Order = await _service.GetAsync(Id);
        ProductLabels = (await _products.GetListAsync(new GetProductListInput { MaxResultCount = 1000 })).Items
            .Where(x => Order.Lines.Any(line => line.ProductId == x.Id))
            .ToDictionary(x => x.Id, x => $"{x.Code} - {x.Name}");
        await LoadProductContextsAsync();
        var draft = Order.Status == SalesOrderStatus.Draft;
        CanEdit = draft && (await _authorizationService.AuthorizeAsync(User, VPureLuxPermissions.Sales.Edit)).Succeeded;
        CanConfirm = draft && (await _authorizationService.AuthorizeAsync(User, VPureLuxPermissions.Sales.Confirm)).Succeeded;
        CanCancel = draft && (await _authorizationService.AuthorizeAsync(User, VPureLuxPermissions.Sales.Cancel)).Succeeded;
    }

    private async Task LoadProductContextsAsync()
    {
        if (ProductContexts.Count > 0)
        {
            return;
        }

        try
        {
            var productIds = Order.Lines.Select(x => x.ProductId).ToHashSet();
            ProductContexts = (await _productPricingContext.FindMapAsync(productIds, Clock.Now))
                .Values
                .ToDictionary(
                    x => x.ProductId,
                    x => new SalesProductContextViewModel
                    {
                        ProductId = x.ProductId,
                        ProductLabel = $"{x.ProductCode} - {x.ProductName}",
                        HasPublishedBom = x.HasPublishedBom,
                        BomStatusText = x.HasPublishedBom
                            ? L["Sales:PublishedBomAvailable"]
                            : L["Sales:NoPublishedBom"]
                    });
        }
        catch (AbpAuthorizationException)
        {
            ProductContexts = new Dictionary<Guid, SalesProductContextViewModel>();
        }
    }
}
