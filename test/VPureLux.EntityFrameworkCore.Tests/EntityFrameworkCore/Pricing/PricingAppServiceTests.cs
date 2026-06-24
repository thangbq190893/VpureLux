using System;
using System.Linq;
using System.Threading.Tasks;
using VPureLux.Bom;
using Shouldly;
using VPureLux.Catalog;
using VPureLux.Catalog.Components;
using VPureLux.Catalog.Products;
using VPureLux.Pricing;
using Volo.Abp;
using Xunit;

namespace VPureLux.EntityFrameworkCore.Pricing;

[Collection(VPureLuxTestConsts.CollectionDefinitionName)]
public class PricingAppServiceTests : VPureLuxEntityFrameworkCoreTestBase
{
    private readonly IComponentSuggestedSellingPriceAppService _componentPriceAppService;
    private readonly IProductSuggestedPriceAppService _productPriceAppService;
    private readonly IProductPricingContextAppService _productPricingContextAppService;
    private readonly IProductPricingContextLookupService _productPricingContextLookupService;
    private readonly IBomAppService _bomAppService;
    private readonly IComponentAppService _componentAppService;
    private readonly IProductAppService _productAppService;

    public PricingAppServiceTests()
    {
        _componentPriceAppService = GetRequiredService<IComponentSuggestedSellingPriceAppService>();
        _productPriceAppService = GetRequiredService<IProductSuggestedPriceAppService>();
        _productPricingContextAppService = GetRequiredService<IProductPricingContextAppService>();
        _productPricingContextLookupService = GetRequiredService<IProductPricingContextLookupService>();
        _bomAppService = GetRequiredService<IBomAppService>();
        _componentAppService = GetRequiredService<IComponentAppService>();
        _productAppService = GetRequiredService<IProductAppService>();
    }

    [Fact]
    public async Task Should_Reject_Missing_Catalog_Items()
    {
        (await Should.ThrowAsync<BusinessException>(() =>
                _componentPriceAppService.CreateAsync(Guid.NewGuid(), ComponentInput())))
            .Code.ShouldBe(VPureLuxDomainErrorCodes.ComponentNotFound);

        (await Should.ThrowAsync<BusinessException>(() =>
                _productPriceAppService.CreateAsync(Guid.NewGuid(), ProductInput())))
            .Code.ShouldBe(VPureLuxDomainErrorCodes.ProductNotFound);
    }

    [Fact]
    public async Task Should_Reject_Inactive_Catalog_Items()
    {
        var component = await CreateComponentAsync();
        var product = await CreateProductAsync();
        await _componentAppService.DeactivateAsync(component.Id);
        await _productAppService.DeactivateAsync(product.Id);

        (await Should.ThrowAsync<BusinessException>(() =>
                _componentPriceAppService.CreateAsync(component.Id, ComponentInput())))
            .Code.ShouldBe(VPureLuxDomainErrorCodes.ComponentNotActive);
        (await Should.ThrowAsync<BusinessException>(() =>
                _productPriceAppService.CreateAsync(product.Id, ProductInput())))
            .Code.ShouldBe(VPureLuxDomainErrorCodes.ValidationFailed);
    }

    [Fact]
    public async Task Should_Reject_Backdated_Creation()
    {
        var component = await CreateComponentAsync();
        var product = await CreateProductAsync();
        var yesterday = DateTime.Now.Date.AddDays(-1);

        (await Should.ThrowAsync<BusinessException>(() =>
                _componentPriceAppService.CreateAsync(component.Id, ComponentInput(yesterday))))
            .Code.ShouldBe(VPureLuxDomainErrorCodes.BackdatedPriceVersionNotAllowed);

        (await Should.ThrowAsync<BusinessException>(() =>
                _productPriceAppService.CreateAsync(product.Id, ProductInput(yesterday))))
            .Code.ShouldBe(VPureLuxDomainErrorCodes.BackdatedPriceVersionNotAllowed);
    }

    [Fact]
    public async Task Should_Allow_Today_For_Component_And_Product_Suggested_Prices()
    {
        var component = await CreateComponentAsync();
        var product = await CreateProductAsync();
        var todayFromDateInput = DateTime.Now.Date;

        var componentVersion = await _componentPriceAppService.CreateAsync(
            component.Id,
            ComponentInput(todayFromDateInput));
        var productVersion = await _productPriceAppService.CreateAsync(
            product.Id,
            ProductInput(todayFromDateInput));

        componentVersion.EffectiveFrom.ShouldBe(todayFromDateInput);
        productVersion.EffectiveFrom.ShouldBe(todayFromDateInput);
    }

    [Fact]
    public async Task Should_Create_Close_And_Query_Component_Price_History()
    {
        var component = await CreateComponentAsync();
        var today = DateTime.Now.Date;
        var first = await _componentPriceAppService.CreateAsync(component.Id, ComponentInput(today, 30000m));
        var second = await _componentPriceAppService.CreateAsync(component.Id, ComponentInput(today.AddDays(1), 25000m));

        (await _componentPriceAppService.GetCurrentAsync(component.Id)).Id.ShouldBe(first.Id);
        (await _componentPriceAppService.GetAtDateAsync(component.Id, today.AddDays(1))).Id.ShouldBe(second.Id);

        var history = await _componentPriceAppService.GetHistoryAsync(component.Id);
        history.Count.ShouldBe(2);
        history[0].VersionNo.ShouldBe(2);
        history[1].Status.ShouldBe(PriceVersionStatus.Closed);
        history[1].EffectiveTo.ShouldBe(today.AddDays(1));
    }

    [Fact]
    public async Task Should_Create_Close_And_Query_Product_Price_History()
    {
        var product = await CreateProductAsync();
        var today = DateTime.Now.Date;
        var first = await _productPriceAppService.CreateAsync(product.Id, ProductInput(today, 100000m));
        var second = await _productPriceAppService.CreateAsync(product.Id, ProductInput(today.AddDays(1), 120000m));

        (await _productPriceAppService.GetCurrentAsync(product.Id)).Id.ShouldBe(first.Id);
        (await _productPriceAppService.GetAtDateAsync(product.Id, today.AddDays(1))).Id.ShouldBe(second.Id);
        (await _productPriceAppService.GetHistoryAsync(product.Id)).Count.ShouldBe(2);
    }

    [Fact]
    public async Task Should_Return_PRICE_003_When_No_Price_Exists()
    {
        var component = await CreateComponentAsync();

        (await Should.ThrowAsync<BusinessException>(() =>
                _componentPriceAppService.GetCurrentAsync(component.Id)))
            .Code.ShouldBe(VPureLuxDomainErrorCodes.PriceVersionNotFound);
    }

    [Fact]
    public async Task Component_Current_Price_Map_Should_Return_Current_Prices_And_Omit_Missing()
    {
        var pricedComponent = await CreateComponentAsync();
        var componentWithoutPrice = await CreateComponentAsync();
        var current = await _componentPriceAppService.CreateAsync(
            pricedComponent.Id,
            ComponentInput(price: 33000m));

        var map = await GetRequiredService<IComponentSuggestedSellingPriceLookupService>()
            .FindCurrentMapAsync(
                new[] { pricedComponent.Id, componentWithoutPrice.Id, Guid.NewGuid() },
                DateTime.Now);

        map.Count.ShouldBe(1);
        map[pricedComponent.Id].Id.ShouldBe(current.Id);
        map[pricedComponent.Id].Price.ShouldBe(33000m);
        map.ContainsKey(componentWithoutPrice.Id).ShouldBeFalse();
    }

    [Fact]
    public async Task Product_Pricing_Context_Should_Show_No_Published_Bom()
    {
        var product = await CreateProductAsync();

        var context = await GetProductContextAsync(product.Id);

        context.HasPublishedBom.ShouldBeFalse();
        context.ComponentBuildPrice.ShouldBeNull();
        context.HasMissingComponentSuggestedPrices.ShouldBeFalse();
    }

    [Fact]
    public async Task Product_Pricing_Context_Should_Show_Missing_Component_Prices()
    {
        var product = await CreateProductAsync();
        var component = await CreateComponentAsync();
        await CreatePublishedBomAsync(product.Id, (component.Id, 1m));

        var context = await GetProductContextAsync(product.Id);

        context.HasPublishedBom.ShouldBeTrue();
        context.HasMissingComponentSuggestedPrices.ShouldBeTrue();
        context.ComponentBuildPrice.ShouldBeNull();
    }

    [Fact]
    public async Task Product_Pricing_Context_Should_Calculate_Component_Build_Price_And_Difference()
    {
        var product = await CreateProductAsync();
        var component1 = await CreateComponentAsync();
        var component2 = await CreateComponentAsync();
        await CreatePublishedBomAsync(product.Id, (component1.Id, 2m), (component2.Id, 3m));
        await _componentPriceAppService.CreateAsync(component1.Id, ComponentInput(price: 10000m));
        await _componentPriceAppService.CreateAsync(component2.Id, ComponentInput(price: 20000m));
        await _productPriceAppService.CreateAsync(product.Id, ProductInput(price: 100000m));

        var context = await GetProductContextAsync(product.Id);

        context.HasPublishedBom.ShouldBeTrue();
        context.HasMissingComponentSuggestedPrices.ShouldBeFalse();
        context.ComponentBuildPrice.ShouldBe(80000m);
        context.CurrentProductSuggestedPrice.ShouldBe(100000m);
        context.Difference.ShouldBe(20000m);
    }

    [Fact]
    public async Task Product_Pricing_Context_Map_Should_Return_Only_Requested_Product_Context()
    {
        var requestedProduct = await CreateProductAsync();
        var otherProduct = await CreateProductAsync();
        var component = await CreateComponentAsync();
        await CreatePublishedBomAsync(requestedProduct.Id, (component.Id, 2m));
        await CreatePublishedBomAsync(otherProduct.Id, (component.Id, 3m));
        await _componentPriceAppService.CreateAsync(component.Id, ComponentInput(price: 10000m));
        await _productPriceAppService.CreateAsync(requestedProduct.Id, ProductInput(price: 50000m));
        await _productPriceAppService.CreateAsync(otherProduct.Id, ProductInput(price: 90000m));

        var map = await _productPricingContextLookupService.FindMapAsync([requestedProduct.Id], DateTime.Now);

        map.Count.ShouldBe(1);
        map.ContainsKey(requestedProduct.Id).ShouldBeTrue();
        map.ContainsKey(otherProduct.Id).ShouldBeFalse();
        map[requestedProduct.Id].CurrentProductSuggestedPrice.ShouldBe(50000m);
        map[requestedProduct.Id].ComponentBuildPrice.ShouldBe(20000m);
        map[requestedProduct.Id].Difference.ShouldBe(30000m);
    }

    private Task<ComponentDto> CreateComponentAsync() =>
        _componentAppService.CreateAsync(new CreateComponentDto
        {
            Code = Unique("PRICE-C"),
            Name = "Pricing Component",
            Unit = "Piece"
        });

    private Task<ProductDto> CreateProductAsync() =>
        _productAppService.CreateAsync(new CreateProductDto
        {
            Code = Unique("PRICE-P"),
            Name = "Pricing Product"
        });

    private async Task<ProductPricingContextDto> GetProductContextAsync(Guid productId)
    {
        var contexts = await _productPricingContextAppService.GetListAsync();
        return contexts.Single(x => x.ProductId == productId);
    }

    private async Task CreatePublishedBomAsync(Guid productId, params (Guid ComponentId, decimal Quantity)[] items)
    {
        var bom = await _bomAppService.CreateAsync(productId, new CreateBomVersionDto
        {
            EffectiveFrom = DateTime.Now.Date,
            Items = items.Select(x => new CreateBomItemDto
            {
                ComponentId = x.ComponentId,
                Quantity = x.Quantity
            }).ToList()
        });
        await _bomAppService.PublishAsync(bom.Id);
    }

    private static CreateComponentSuggestedSellingPriceVersionDto ComponentInput(
        DateTime? effectiveFrom = null,
        decimal price = 30000m) =>
        new()
        {
            Price = price,
            Reason = "Periodic component selling price adjustment",
            EffectiveFrom = effectiveFrom ?? DateTime.Now.Date
        };

    private static CreateProductSuggestedPriceVersionDto ProductInput(
        DateTime? effectiveFrom = null,
        decimal price = 100000m) =>
        new()
        {
            Price = price,
            Reason = "Periodic adjustment",
            EffectiveFrom = effectiveFrom ?? DateTime.Now.Date
        };

    private static string Unique(string prefix) => prefix + Guid.NewGuid().ToString("N")[..8];
}
