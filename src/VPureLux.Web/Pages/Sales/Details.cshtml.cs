using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Localization;
using VPureLux.Catalog.Products;
using VPureLux.Customers;
using VPureLux.Localization;
using VPureLux.Permissions;
using VPureLux.Pricing;
using VPureLux.Sales;
using Volo.Abp;
using Volo.Abp.Authorization;
using Volo.Abp.Data;
using Volo.Abp.Uow;

namespace VPureLux.Web.Pages.Sales;

[Authorize(VPureLuxPermissions.Sales.View)]
public class DetailsModel : VPureLuxPageModel
{
    private readonly ISalesOrderAppService _service;
    private readonly IAuthorizationService _authorizationService;
    private readonly ICustomerAppService _customers;
    private readonly IProductAppService _products;
    private readonly IProductPricingContextLookupService _productPricingContext;
    private readonly IUnitOfWorkManager _unitOfWorkManager;
    [BindProperty(SupportsGet = true)] public Guid Id { get; set; }
    [BindProperty] public ConfirmSalesOrderDto Confirmation { get; set; } = new() { IdempotencyKey = Guid.NewGuid().ToString("N") };
    [TempData] public string? SuccessMessage { get; set; }
    public SalesOrderDto Order { get; private set; } = new();
    public bool CanEdit { get; private set; }
    public bool CanConfirm { get; private set; }
    public bool CanCancel { get; private set; }
    public bool IsDraft => Order.Status == SalesOrderStatus.Draft;
    public decimal DraftEstimatedRevenueAmount { get; private set; }
    public string CustomerDisplay { get; private set; } = string.Empty;
    [TempData] public string? ConfirmErrorMessage { get; set; }
    public Dictionary<Guid, string> ProductLabels { get; private set; } = new();
    public Dictionary<Guid, SalesProductContextViewModel> ProductContexts { get; private set; } = new();

    public DetailsModel(
        ISalesOrderAppService service,
        IAuthorizationService authorizationService,
        ICustomerAppService customers,
        IProductAppService products,
        IProductPricingContextLookupService productPricingContext,
        IUnitOfWorkManager unitOfWorkManager)
    {
        _service = service;
        _authorizationService = authorizationService;
        _customers = customers;
        _products = products;
        _productPricingContext = productPricingContext;
        _unitOfWorkManager = unitOfWorkManager;
    }

    public async Task OnGetAsync() => await LoadAsync();
    [UnitOfWork(IsDisabled = true)]
    public async Task<IActionResult> OnPostConfirmAsync()
    {
        try
        {
            using var unitOfWork = _unitOfWorkManager.Begin(requiresNew: true, isTransactional: true);
            await _service.ConfirmAsync(Id, Confirmation);
            await unitOfWork.CompleteAsync();
            SuccessMessage = L["Sales:ConfirmedSuccessfully"];
            return RedirectToPage(new { id = Id });
        }
        catch (BusinessException exception) when (IsKnownConfirmException(exception))
        {
            await AddConfirmErrorAsync(exception);
            return Page();
        }
        catch (AbpAuthorizationException)
        {
            await AddConfirmErrorAsync(new BusinessException(VPureLuxDomainErrorCodes.AccessDenied));
            return Page();
        }
        catch (AbpDbConcurrencyException exception)
        {
            await AddConfirmConcurrencyErrorAsync(exception);
            return RedirectToPage(new { id = Id });
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
        catch (BusinessException exception) when (IsKnownCancelException(exception))
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
        DraftEstimatedRevenueAmount = Order.Lines.Sum(x => decimal.Round(
            x.Quantity * x.ActualSellingPrice,
            SalesConsts.MoneyScale,
            MidpointRounding.AwayFromZero));
        CustomerDisplay = await GetCustomerDisplayAsync();
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

    private async Task<string> GetCustomerDisplayAsync()
    {
        if (!string.IsNullOrWhiteSpace(Order.CustomerCodeSnapshot) || !string.IsNullOrWhiteSpace(Order.CustomerNameSnapshot))
        {
            return $"{Order.CustomerCodeSnapshot} - {Order.CustomerNameSnapshot}".Trim(' ', '-');
        }

        try
        {
            var customer = await _customers.GetAsync(Order.CustomerId);
            return $"{customer.Code} - {customer.Name}";
        }
        catch (AbpAuthorizationException)
        {
            return L["Sales:CustomerContextUnavailable"];
        }
        catch (BusinessException exception) when (exception.Code == VPureLuxDomainErrorCodes.CustomerNotFound)
        {
            return L["Sales:CustomerContextUnavailable"];
        }
    }

    private async Task AddConfirmErrorAsync(BusinessException exception)
    {
        if (exception.Code == VPureLuxDomainErrorCodes.SalesConcurrentModification)
        {
            ConfirmErrorMessage = GetConfirmConcurrencyErrorMessage();
            LogConfirmConcurrency(null);
        }
        else
        {
            ConfirmErrorMessage = SalesUiFormatter.GetFriendlyErrorMessage(L, exception);
        }

        ModelState.AddModelError(string.Empty, ConfirmErrorMessage);
        await LoadAsync();
    }

    private async Task AddConfirmConcurrencyErrorAsync(AbpDbConcurrencyException exception)
    {
        await RollbackCurrentUnitOfWorkAsync();
        LogConfirmConcurrency(exception);
        ConfirmErrorMessage = GetConfirmConcurrencyErrorMessage();
    }

    private async Task RollbackCurrentUnitOfWorkAsync()
    {
        var currentUnitOfWork = _unitOfWorkManager.Current;
        if (currentUnitOfWork != null)
        {
            await currentUnitOfWork.RollbackAsync();
        }
    }

    private string GetConfirmConcurrencyErrorMessage()
    {
        var localizer = HttpContext?.RequestServices?.GetService<IStringLocalizer<VPureLuxResource>>();
        return localizer?["Sales:ConfirmConcurrencyError"] ?? L["Sales:ConfirmConcurrencyError"];
    }

    private void LogConfirmConcurrency(Exception? exception)
    {
        var logger = HttpContext?.RequestServices?.GetService<ILogger<DetailsModel>>();
        if (exception == null)
        {
            logger?.LogWarning("Sales confirm concurrency conflict for order {SalesOrderId}.", Id);
            return;
        }

        logger?.LogWarning(exception, "Sales confirm database concurrency conflict for order {SalesOrderId}.", Id);
    }

    private static bool IsKnownConfirmException(BusinessException exception) =>
        exception.Code is
            VPureLuxDomainErrorCodes.SalesInventoryValidationFailed or
            VPureLuxDomainErrorCodes.SalesBomMustBePublished or
            VPureLuxDomainErrorCodes.ComponentNotActive or
            VPureLuxDomainErrorCodes.WarehouseInactive or
            VPureLuxDomainErrorCodes.StockItemNotFound or
            VPureLuxDomainErrorCodes.StockItemInactive or
            VPureLuxDomainErrorCodes.StockItemInventoryDisabled or
            VPureLuxDomainErrorCodes.CustomerNotFound or
            VPureLuxDomainErrorCodes.CustomerInactive or
            VPureLuxDomainErrorCodes.CustomerGroupNotFound or
            VPureLuxDomainErrorCodes.CustomerGroupInactive or
            VPureLuxDomainErrorCodes.ProductNotFound or
            VPureLuxDomainErrorCodes.SalesOrderAlreadyConfirmed or
            VPureLuxDomainErrorCodes.SalesOrderAlreadyCancelled or
            VPureLuxDomainErrorCodes.SalesOrderCannotBeModified or
            VPureLuxDomainErrorCodes.SalesConcurrentModification or
            VPureLuxDomainErrorCodes.DuplicateConfirmationKey or
            VPureLuxDomainErrorCodes.AccessDenied or
            VPureLuxDomainErrorCodes.ValidationFailed;

    private static bool IsKnownCancelException(BusinessException exception) =>
        exception.Code is
            VPureLuxDomainErrorCodes.SalesOrderAlreadyConfirmed or
            VPureLuxDomainErrorCodes.SalesOrderAlreadyCancelled or
            VPureLuxDomainErrorCodes.SalesOrderNotFound or
            VPureLuxDomainErrorCodes.SalesConcurrentModification or
            VPureLuxDomainErrorCodes.AccessDenied or
            VPureLuxDomainErrorCodes.ValidationFailed;
}
