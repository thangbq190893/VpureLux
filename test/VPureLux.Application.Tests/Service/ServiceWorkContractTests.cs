using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Shouldly;
using Xunit;

namespace VPureLux.Service;

public class ServiceWorkContractTests
{
    [Fact]
    public void Work_input_should_require_identity_and_unit()
    {
        var errors = new List<ValidationResult>();
        var input = new CreateUpdateServiceWorkDto();
        Validator.TryValidateObject(input, new ValidationContext(input), errors, true).ShouldBeFalse();
        errors.Count.ShouldBe(3);
    }

    [Theory]
    [InlineData(null, true)]
    [InlineData(0, true)]
    [InlineData(-1, false)]
    public void Work_cost_validation_should_distinguish_unknown_from_zero(int? cost, bool valid)
    {
        var input = new CreateUpdateServiceWorkDto { Code = "A", Name = "Work", Unit = "Visit", StandardCost = cost };
        Validator.TryValidateObject(input, new ValidationContext(input), new List<ValidationResult>(), true).ShouldBe(valid);
    }
}
