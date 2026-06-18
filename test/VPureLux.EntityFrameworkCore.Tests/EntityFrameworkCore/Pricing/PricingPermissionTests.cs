using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Shouldly;
using VPureLux.Permissions;
using VPureLux.Pricing;
using Volo.Abp.Authorization.Permissions;
using Xunit;

namespace VPureLux.EntityFrameworkCore.Pricing;

[Collection(VPureLuxTestConsts.CollectionDefinitionName)]
public class PricingPermissionTests : VPureLuxEntityFrameworkCoreTestBase
{
    private readonly IPermissionDefinitionManager _permissionDefinitionManager;

    public PricingPermissionTests()
    {
        _permissionDefinitionManager = GetRequiredService<IPermissionDefinitionManager>();
    }

    [Fact]
    public async Task Should_Define_All_Pricing_Permissions()
    {
        (await _permissionDefinitionManager.GetAsync(VPureLuxPermissions.Pricing.View)).ShouldNotBeNull();
        (await _permissionDefinitionManager.GetAsync(VPureLuxPermissions.Pricing.History)).ShouldNotBeNull();
        (await _permissionDefinitionManager.GetAsync(
            VPureLuxPermissions.Pricing.ComponentSuggestedSellingPrices.Create)).ShouldNotBeNull();
        (await _permissionDefinitionManager.GetAsync(
            VPureLuxPermissions.Pricing.ComponentSuggestedSellingPrices.History)).ShouldNotBeNull();
        (await _permissionDefinitionManager.GetAsync(
            VPureLuxPermissions.Pricing.ProductSuggestedPrices.Create)).ShouldNotBeNull();
    }

    [Fact]
    public void Should_Protect_Pricing_Operations()
    {
        GetClassPermission(typeof(ComponentSuggestedSellingPriceAppService)).ShouldBe(VPureLuxPermissions.Pricing.View);
        GetMethodPermission(typeof(ComponentSuggestedSellingPriceAppService), nameof(ComponentSuggestedSellingPriceAppService.GetHistoryAsync))
            .ShouldBe(VPureLuxPermissions.Pricing.ComponentSuggestedSellingPrices.History);
        GetMethodPermission(typeof(ComponentSuggestedSellingPriceAppService), nameof(ComponentSuggestedSellingPriceAppService.CreateAsync))
            .ShouldBe(VPureLuxPermissions.Pricing.ComponentSuggestedSellingPrices.Create);

        GetClassPermission(typeof(ProductSuggestedPriceAppService)).ShouldBe(VPureLuxPermissions.Pricing.View);
        GetMethodPermission(typeof(ProductSuggestedPriceAppService), nameof(ProductSuggestedPriceAppService.GetHistoryAsync))
            .ShouldBe(VPureLuxPermissions.Pricing.History);
        GetMethodPermission(typeof(ProductSuggestedPriceAppService), nameof(ProductSuggestedPriceAppService.CreateAsync))
            .ShouldBe(VPureLuxPermissions.Pricing.ProductSuggestedPrices.Create);
    }

    private static string? GetClassPermission(MemberInfo member) =>
        member.GetCustomAttribute<AuthorizeAttribute>()?.Policy;

    private static string? GetMethodPermission(Type type, string methodName) =>
        type.GetMethods().Single(x => x.Name == methodName)
            .GetCustomAttribute<AuthorizeAttribute>()?.Policy;
}
