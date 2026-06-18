using System;
using System.Linq;
using Shouldly;
using Xunit;

namespace VPureLux.Catalog;

public class CatalogHardeningDomainTests
{
    [Fact]
    public void Aggregate_Constructors_Should_Not_Be_Public()
    {
        typeof(Component).GetConstructors().ShouldBeEmpty();
        typeof(Product).GetConstructors().ShouldBeEmpty();
    }

    [Fact]
    public void Catalog_Error_Codes_Should_Match_Documentation()
    {
        VPureLuxDomainErrorCodes.ComponentCodeAlreadyExists.ShouldBe("CATALOG_001");
        VPureLuxDomainErrorCodes.ProductCodeAlreadyExists.ShouldBe("CATALOG_002");
        VPureLuxDomainErrorCodes.ComponentNotFound.ShouldBe("CATALOG_003");
        VPureLuxDomainErrorCodes.ProductNotFound.ShouldBe("CATALOG_004");
    }
}
