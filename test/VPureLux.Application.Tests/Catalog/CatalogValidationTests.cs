using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Shouldly;
using VPureLux.Catalog.Components;
using VPureLux.Catalog.Products;
using Xunit;
using System.Linq;

namespace VPureLux.Catalog;

public class CatalogValidationTests
{
    [Fact]
    public void Should_Validate_Create_Component_Dto()
    {
        var input = new CreateComponentDto();

        Validate(input).ShouldNotBeEmpty();
    }

    [Fact]
    public void Should_Validate_Create_Product_Dto()
    {
        var input = new CreateProductDto();

        Validate(input).ShouldNotBeEmpty();
    }

    [Fact]
    public void General_Dtos_Should_Never_Expose_ImageBase64()
    {
        typeof(ProductDto).GetProperties().Select(x => x.Name).ShouldNotContain("ImageBase64");
        typeof(ComponentDto).GetProperties().Select(x => x.Name).ShouldNotContain("ImageBase64");
    }

    private static List<ValidationResult> Validate(object input)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(input, new ValidationContext(input), results, validateAllProperties: true);
        return results;
    }
}
