using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.Extensions.Logging;
using VPureLux.Catalog;
using VPureLux.Catalog.Products;
using VPureLux.Permissions;
using VPureLux.Pricing;
using VPureLux.Sales;
using VPureLux.Web.Sales;
using Volo.Abp;
using Volo.Abp.Authorization;

namespace VPureLux.Web.Pages.Sales;

[Authorize(VPureLuxPermissions.Sales.AdjustConfirmedBeforeInstallation)]
public class AdjustModel : VPureLuxPageModel
{
    private readonly ISalesOrderAppService _sales;
    private readonly ISalesPostConfirmationAppService _postConfirmation;
    private readonly IProductAppService _products;
    private readonly IProductPricingContextLookupService _pricingContext;

    [BindProperty(SupportsGet = true)] public Guid Id { get; set; }
    [BindProperty(SupportsGet = true)] public Guid? RevisionId { get; set; }
    [BindProperty, ValidateNever] public OpenSalesOrderRevisionDto StartInput { get; set; } = new();
    [BindProperty] public UpdateSalesOrderRevisionDto UpdateInput { get; set; } = new();
    public SalesOrderDto Order { get; private set; } = new();
    public SalesOrderRevisionDto? Revision { get; private set; }
    public SalesRevisionPreviewDto? Preview { get; private set; }

    public AdjustModel(
        ISalesOrderAppService sales,
        ISalesPostConfirmationAppService postConfirmation,
        IProductAppService products,
        IProductPricingContextLookupService pricingContext)
    {
        _sales = sales;
        _postConfirmation = postConfirmation;
        _products = products;
        _pricingContext = pricingContext;
    }

    public async Task<IActionResult> OnGetAsync()
    {
        Order = await _sales.GetAsync(Id);
        var state = await _postConfirmation.GetOrderStateAsync(Id);
        if (!RevisionId.HasValue && state.ActiveRevisionId.HasValue)
        {
            return RedirectToPage(new { id = Id, revisionId = state.ActiveRevisionId });
        }
        if (RevisionId.HasValue)
        {
            await LoadRevisionAsync();
        }
        return Page();
    }

    public async Task<IActionResult> OnPostStartAsync()
    {
        TryValidateModel(StartInput, nameof(StartInput));
        if (!ModelState.IsValid)
        {
            Order = await _sales.GetAsync(Id);
            return Page();
        }

        var revision = await _postConfirmation.OpenRevisionAsync(Id, StartInput);
        return RedirectToPage(new { id = Id, revisionId = revision.Id });
    }

    public async Task<IActionResult> OnPostSaveAsync()
    {
        if (!RevisionId.HasValue)
        {
            return NotFound();
        }
        NormalizeLines();
        if (!ModelState.IsValid)
        {
            await LoadRevisionAsync(usePostedLines: true);
            return Page();
        }

        try
        {
            await _postConfirmation.UpdateRevisionAsync(RevisionId.Value, UpdateInput);
            TempData["SalesAdjustmentMessage"] = L["Sales:AdjustmentSaved"].Value;
            return RedirectToPage(new { id = Id, revisionId = RevisionId });
        }
        catch (BusinessException exception)
        {
            ModelState.AddModelError(string.Empty, SalesUiFormatter.GetFriendlyErrorMessage(L, exception));
            await LoadRevisionAsync(usePostedLines: true);
            return Page();
        }
    }

    public async Task<IActionResult> OnPostConfirmAsync()
    {
        if (!RevisionId.HasValue)
        {
            return NotFound();
        }

        NormalizeLines();
        if (!ModelState.IsValid)
        {
            Logger.LogWarning(
                "Sales adjustment confirm validation failed for order {OrderId}, revision {RevisionId}: {ValidationErrors}",
                Id,
                RevisionId,
                string.Join("; ", ModelState
                    .Where(entry => entry.Value?.Errors.Count > 0)
                    .Select(entry => $"{entry.Key}: {string.Join(", ", entry.Value!.Errors.Select(error => error.ErrorMessage))}")));
            await LoadRevisionAsync(usePostedLines: true);
            return Page();
        }

        try
        {
            var revision = await _postConfirmation.SubmitRevisionAsync(RevisionId.Value, new SubmitSalesOrderRevisionDto
            {
                CustomerId = UpdateInput.CustomerId,
                Lines = UpdateInput.Lines,
                IdempotencyKey = Guid.NewGuid().ToString("N")
            });
            if (revision.Status == SalesOrderRevisionStatus.Draft)
            {
                TempData["SalesAdjustmentMessage"] = L["Sales:AdjustmentWaitingWarehouse"].Value;
                return RedirectToPage(new { id = Id, revisionId = RevisionId });
            }
            TempData[nameof(DetailsModel.SuccessMessage)] = L["Sales:AdjustmentApplied"].Value;
            return RedirectToPage("/Sales/Details", new { id = Id });
        }
        catch (BusinessException exception)
        {
            ModelState.AddModelError(string.Empty, SalesUiFormatter.GetFriendlyErrorMessage(L, exception));
            await LoadRevisionAsync();
            return Page();
        }
    }

    public async Task<IActionResult> OnPostDiscardAsync()
    {
        if (!RevisionId.HasValue)
        {
            return NotFound();
        }
        await _postConfirmation.CancelRevisionAsync(RevisionId.Value, new ReasonDto
        {
            Reason = L["Sales:AdjustmentDiscarded"].Value
        });
        return RedirectToPage("/Sales/Details", new { id = Id });
    }

    public async Task<JsonResult> OnGetProductLookupAsync(string? term, int page = 1)
    {
        const int pageSize = 20;
        var result = await _products.GetListAsync(new GetProductListInput
        {
            Keyword = term,
            Status = CatalogItemStatus.Active,
            SkipCount = Math.Max(page - 1, 0) * pageSize,
            MaxResultCount = pageSize,
            Sorting = "Code"
        });
        return new JsonResult(new
        {
            results = result.Items.Select(x => new { id = x.Id, text = $"{x.Code} - {x.Name}" }),
            pagination = new { more = page * pageSize < result.TotalCount }
        });
    }

    public async Task<JsonResult> OnGetProductContextAsync(Guid productId)
    {
        Order = await _sales.GetAsync(Id);
        try
        {
            var contexts = await _pricingContext.FindMapAsync([productId], Order.OrderDate);
            if (contexts.TryGetValue(productId, out var context))
            {
                return new JsonResult(new { context.HasPublishedBom, SuggestedPrice = context.CurrentProductSuggestedPrice });
            }
        }
        catch (AbpAuthorizationException)
        {
        }
        return new JsonResult(new { hasPublishedBom = false, suggestedPrice = (decimal?)null });
    }

    public string FormatMoney(decimal value) =>
        decimal.Round(value, 0, MidpointRounding.AwayFromZero)
            .ToString("#,0", CultureInfo.GetCultureInfo("vi-VN")) + " VND";

    private async Task LoadRevisionAsync(bool usePostedLines = false)
    {
        Order = await _sales.GetAsync(Id);
        Revision = await _postConfirmation.GetRevisionAsync(RevisionId!.Value);
        if (Revision.SalesOrderId != Id)
        {
            throw new BusinessException(VPureLuxDomainErrorCodes.SalesRevisionNotAllowed);
        }
        Preview = await _postConfirmation.GetRevisionPreviewAsync(Revision.Id);
        if (!usePostedLines)
        {
            UpdateInput = new UpdateSalesOrderRevisionDto
            {
                CustomerId = Revision.CustomerId,
                Lines = Revision.Lines.OrderBy(x => x.LineNo).Select(x => new UpdateSalesOrderRevisionLineDto
                {
                    RevisionLineId = x.Id,
                    IsRemoved = x.IsRemoved,
                    ProductId = x.ProductId,
                    Quantity = x.Quantity,
                    ActualSellingPrice = x.ActualSellingPrice
                }).ToList()
            };
        }
    }

    private void NormalizeLines()
    {
        UpdateInput.Lines ??= new List<UpdateSalesOrderRevisionLineDto>();
        UpdateInput.Lines = UpdateInput.Lines
            .Where(x => x.RevisionLineId.HasValue || !x.IsRemoved)
            .ToList();
        if (UpdateInput.Lines.All(x => x.IsRemoved))
        {
            ModelState.AddModelError(string.Empty, L["Sales:AdjustmentRequiresLine"]);
        }
    }
}
