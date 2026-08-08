using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VPureLux.Bom;
using VPureLux.Catalog.Components;
using VPureLux.Catalog.Products;
using VPureLux.Permissions;

namespace VPureLux.Web.Pages.Bom;

[Authorize(VPureLuxPermissions.Bom.View)]
public class DetailsModel : VPureLuxPageModel
{
    private readonly IBomAppService _bomAppService;
    private readonly IProductAppService _productAppService;
    private readonly IComponentAppService _componentAppService;
    private readonly IBomStandardCostLookupService _standardCostLookupService;
    private readonly IAuthorizationService _authorizationService;

    [BindProperty(SupportsGet = true)]
    public Guid Id { get; set; }

    public BomVersionDto BomVersion { get; private set; } = new();
    public string ProductLabel { get; private set; } = string.Empty;
    public Dictionary<Guid, string> ComponentLabels { get; private set; } = new();
    public BomStandardCostRangeDto StandardCost { get; private set; } = new();
    public bool CanEdit { get; private set; }

    public DetailsModel(
        IBomAppService bomAppService,
        IProductAppService productAppService,
        IComponentAppService componentAppService,
        IBomStandardCostLookupService standardCostLookupService,
        IAuthorizationService authorizationService)
    {
        _bomAppService = bomAppService;
        _productAppService = productAppService;
        _componentAppService = componentAppService;
        _standardCostLookupService = standardCostLookupService;
        _authorizationService = authorizationService;
    }

    public async Task OnGetAsync()
    {
        BomVersion = await _bomAppService.GetAsync(Id);
        StandardCost = await _standardCostLookupService.GetAsync(Id);
        await LoadCatalogLabelsAsync();
        CanEdit = BomVersion.Status == BomStatus.Draft &&
                  (await _authorizationService.AuthorizeAsync(User, VPureLuxPermissions.Bom.Create)).Succeeded;
    }

    public string GetComponentLabel(Guid componentId)
    {
        return ComponentLabels.TryGetValue(componentId, out var label)
            ? label
            : L["Bom:UnknownComponent"];
    }

    public BomStandardCostItemDto? GetStandardCostItem(Guid bomItemId)
    {
        return StandardCost.Items.FirstOrDefault(x => x.BomItemId == bomItemId);
    }

    private async Task LoadCatalogLabelsAsync()
    {
        var product = await _productAppService.GetAsync(BomVersion.ProductId);
        ProductLabel = $"{product.Code} - {product.Name}";

        var components = await _componentAppService.GetListAsync(new GetComponentListInput
        {
            MaxResultCount = Volo.Abp.Application.Dtos.LimitedResultRequestDto.MaxMaxResultCount
        });

        var componentIds = BomVersion.Items.Select(x => x.ComponentId).ToHashSet();
        ComponentLabels = components.Items
            .Where(x => componentIds.Contains(x.Id))
            .ToDictionary(x => x.Id, x => $"{x.Code} - {x.Name}");
    }
}
