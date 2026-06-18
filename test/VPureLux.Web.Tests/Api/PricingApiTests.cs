using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http.Json;
using System.Reflection;
using System.Threading.Tasks;
using Shouldly;
using VPureLux.Catalog.Components;
using VPureLux.Catalog.Products;
using VPureLux.Pricing;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace VPureLux.Api;

[Collection(VPureLuxTestConsts.CollectionDefinitionName)]
public class PricingApiTests : VPureLuxWebTestBase
{
    [Fact]
    public async Task Should_Create_And_Query_Component_Suggested_Selling_Prices()
    {
        var component = await CreateComponentAsync();
        var today = DateTime.Now.Date;
        var created = await CreateComponentPriceAsync(component.Id, today, 30000m);

        (await GetResponseAsObjectAsync<ComponentSuggestedSellingPriceVersionDto>(
            $"/api/pricing/components/{component.Id}/suggested-selling-prices/current")).Id.ShouldBe(created.Id);
        (await GetResponseAsObjectAsync<ComponentSuggestedSellingPriceVersionDto>(
            $"/api/pricing/components/{component.Id}/suggested-selling-prices/at-date?date={today:yyyy-MM-dd}")).Id.ShouldBe(created.Id);
        (await GetResponseAsObjectAsync<List<ComponentSuggestedSellingPriceVersionDto>>(
            $"/api/pricing/components/{component.Id}/suggested-selling-prices")).ShouldHaveSingleItem();
    }

    [Fact]
    public async Task Should_Create_And_Query_Product_Suggested_Prices()
    {
        var product = await CreateProductAsync();
        var today = DateTime.Now.Date;
        var response = await Client.PostAsJsonAsync(
            $"/api/pricing/products/{product.Id}/suggested-prices",
            new CreateProductSuggestedPriceVersionDto
            {
                Price = 100000m,
                Reason = "Periodic adjustment",
                EffectiveFrom = today
            });
        response.StatusCode.ShouldBe(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        var created = (await response.Content.ReadFromJsonAsync<ProductSuggestedPriceVersionDto>())!;

        (await GetResponseAsObjectAsync<ProductSuggestedPriceVersionDto>(
            $"/api/pricing/products/{product.Id}/suggested-prices/current")).Id.ShouldBe(created.Id);
        (await GetResponseAsObjectAsync<ProductSuggestedPriceVersionDto>(
            $"/api/pricing/products/{product.Id}/suggested-prices/at-date?date={today:yyyy-MM-dd}")).Id.ShouldBe(created.Id);
        (await GetResponseAsObjectAsync<List<ProductSuggestedPriceVersionDto>>(
            $"/api/pricing/products/{product.Id}/suggested-prices")).ShouldHaveSingleItem();
    }

    [Fact]
    public async Task Should_Reject_Invalid_Create_Request()
    {
        var component = await CreateComponentAsync();
        var response = await Client.PostAsJsonAsync(
            $"/api/pricing/components/{component.Id}/suggested-selling-prices",
            new CreateComponentSuggestedSellingPriceVersionDto
            {
                Price = 0,
                Reason = string.Empty,
                EffectiveFrom = DateTime.Now.Date
            });

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public void Should_Not_Expose_Update_Or_Delete_Routes()
    {
        var controllerMethods = new[]
            {
                typeof(ComponentSuggestedSellingPriceController),
                typeof(ProductSuggestedPriceController)
            }
            .SelectMany(x => x.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            .ToArray();

        controllerMethods.ShouldNotContain(x => x.GetCustomAttribute<HttpPutAttribute>() != null);
        controllerMethods.ShouldNotContain(x => x.GetCustomAttribute<HttpDeleteAttribute>() != null);
        typeof(IComponentSuggestedSellingPriceAppService).GetMethods().ShouldNotContain(x => x.Name.Contains("Update") || x.Name.Contains("Delete"));
        typeof(IProductSuggestedPriceAppService).GetMethods().ShouldNotContain(x => x.Name.Contains("Update") || x.Name.Contains("Delete"));
    }

    private async Task<ComponentDto> CreateComponentAsync()
    {
        return await GetRequiredService<IComponentAppService>().CreateAsync(new CreateComponentDto
        {
            Code = Unique("API-PRICE-C"),
            Name = "API Pricing Component",
            Unit = "Piece"
        });
    }

    private async Task<ProductDto> CreateProductAsync()
    {
        return await GetRequiredService<IProductAppService>().CreateAsync(new CreateProductDto
        {
            Code = Unique("API-PRICE-P"),
            Name = "API Pricing Product"
        });
    }

    private async Task<ComponentSuggestedSellingPriceVersionDto> CreateComponentPriceAsync(
        Guid componentId,
        DateTime effectiveFrom,
        decimal price)
    {
        var response = await Client.PostAsJsonAsync(
            $"/api/pricing/components/{componentId}/suggested-selling-prices",
            new CreateComponentSuggestedSellingPriceVersionDto
            {
                Price = price,
                Reason = "Periodic component selling price adjustment",
                EffectiveFrom = effectiveFrom
            });
        response.StatusCode.ShouldBe(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        return (await response.Content.ReadFromJsonAsync<ComponentSuggestedSellingPriceVersionDto>())!;
    }

    private static string Unique(string prefix) => prefix + Guid.NewGuid().ToString("N")[..8];
}
