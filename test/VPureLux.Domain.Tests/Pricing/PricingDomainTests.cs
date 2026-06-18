using System;
using System.Linq;
using System.Reflection;
using Shouldly;
using VPureLux.Pricing.Events;
using Volo.Abp;
using Xunit;

namespace VPureLux.Pricing;

public class PricingDomainTests
{
    [Fact]
    public void Should_Create_Component_Suggested_Selling_Price_Version()
    {
        var version = CreateComponentVersion();

        version.Status.ShouldBe(PriceVersionStatus.Active);
        version.VersionNo.Value.ShouldBe(1);
        version.Price.Amount.ShouldBe(30000m);
        version.Price.Currency.ShouldBe(PricingConsts.Currency);
        version.Reason.ShouldBe("Periodic component selling price adjustment");
        version.EffectivePeriod.EffectiveTo.ShouldBeNull();
    }

    [Fact]
    public void Should_Create_Product_Suggested_Price_Version()
    {
        var version = CreateProductVersion();

        version.Status.ShouldBe(PriceVersionStatus.Active);
        version.Price.Amount.ShouldBe(100000m);
        version.Price.Currency.ShouldBe(PricingConsts.Currency);
    }

    [Fact]
    public void Should_Close_Version_Using_Exclusive_End()
    {
        var version = CreateComponentVersion();
        var effectiveTo = version.EffectivePeriod.EffectiveFrom.AddDays(1);

        version.Close(effectiveTo);

        version.Status.ShouldBe(PriceVersionStatus.Closed);
        version.EffectivePeriod.Contains(effectiveTo.AddTicks(-1)).ShouldBeTrue();
        version.EffectivePeriod.Contains(effectiveTo).ShouldBeFalse();
    }

    [Fact]
    public void Should_Reject_Invalid_Effective_Period()
    {
        var start = DateTime.UtcNow;

        Should.Throw<BusinessException>(() => new EffectivePeriod(start, start))
            .Code.ShouldBe(VPureLuxDomainErrorCodes.InvalidPriceEffectivePeriod);
        Should.Throw<BusinessException>(() => new EffectivePeriod(start, start.AddTicks(-1)))
            .Code.ShouldBe(VPureLuxDomainErrorCodes.InvalidPriceEffectivePeriod);
    }

    [Fact]
    public void Should_Reject_Non_Positive_Price()
    {
        Should.Throw<BusinessException>(() => new Money(0))
            .Code.ShouldBe(VPureLuxDomainErrorCodes.PriceMustBeGreaterThanZero);
    }

    [Fact]
    public void Should_Round_Vnd_Price_To_Two_Decimals()
    {
        new Money(123.456m).Amount.ShouldBe(123.46m);
    }

    [Fact]
    public void Should_Validate_Reason()
    {
        Should.Throw<ArgumentException>(() => CreateComponentVersion(reason: " "));
        Should.Throw<ArgumentException>(() => CreateComponentVersion(reason: new string('x', PricingConsts.MaxReasonLength + 1)));
    }

    [Fact]
    public void Should_Reject_Closing_Closed_Version()
    {
        var version = CreateProductVersion();
        version.Close(version.EffectivePeriod.EffectiveFrom.AddDays(1));

        Should.Throw<BusinessException>(
                () => version.Close(version.EffectivePeriod.EffectiveFrom.AddDays(2)))
            .Code.ShouldBe(VPureLuxDomainErrorCodes.PriceVersionAlreadyClosed);
    }

    [Fact]
    public void Should_Expose_No_Public_History_Mutation()
    {
        PublicMutationMethods(typeof(ComponentSuggestedSellingPriceVersion)).ShouldBe(new[] { nameof(ComponentSuggestedSellingPriceVersion.Close) });
        PublicMutationMethods(typeof(ProductSuggestedPriceVersion)).ShouldBe(new[] { nameof(ProductSuggestedPriceVersion.Close) });
    }

    [Fact]
    public void Should_Raise_Lightweight_Component_Price_Events()
    {
        var version = CreateComponentVersion();
        Events(version).OfType<ComponentSuggestedSellingPriceVersionCreatedEvent>().ShouldHaveSingleItem();

        version.Close(version.EffectivePeriod.EffectiveFrom.AddDays(1));
        Events(version).OfType<ComponentSuggestedSellingPriceVersionClosedEvent>().ShouldHaveSingleItem();
    }

    [Fact]
    public void Should_Raise_Lightweight_Product_Price_Events()
    {
        var version = CreateProductVersion();
        Events(version).OfType<ProductSuggestedPriceVersionCreatedEvent>().ShouldHaveSingleItem();

        version.Close(version.EffectivePeriod.EffectiveFrom.AddDays(1));
        Events(version).OfType<ProductSuggestedPriceVersionClosedEvent>().ShouldHaveSingleItem();
    }

    internal static ComponentSuggestedSellingPriceVersion CreateComponentVersion(
        Guid? componentId = null,
        int versionNo = 1,
        string reason = "Periodic component selling price adjustment",
        DateTime? effectiveFrom = null)
    {
        return new ComponentSuggestedSellingPriceVersion(
            Guid.NewGuid(),
            componentId ?? Guid.NewGuid(),
            new PriceVersionNo(versionNo),
            new Money(30000m),
            reason,
            effectiveFrom ?? DateTime.UtcNow.Date);
    }

    internal static ProductSuggestedPriceVersion CreateProductVersion(
        Guid? productId = null,
        int versionNo = 1,
        DateTime? effectiveFrom = null)
    {
        return new ProductSuggestedPriceVersion(
            Guid.NewGuid(),
            productId ?? Guid.NewGuid(),
            new PriceVersionNo(versionNo),
            new Money(100000m),
            "Periodic adjustment",
            effectiveFrom ?? DateTime.UtcNow.Date);
    }

    private static string[] PublicMutationMethods(Type type)
    {
        return type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Where(x => !x.IsSpecialName)
            .Select(x => x.Name)
            .OrderBy(x => x)
            .ToArray();
    }

    private static object[] Events(ComponentSuggestedSellingPriceVersion version)
    {
        return version.GetLocalEvents().Select(x => x.EventData).ToArray();
    }

    private static object[] Events(ProductSuggestedPriceVersion version)
    {
        return version.GetLocalEvents().Select(x => x.EventData).ToArray();
    }
}
