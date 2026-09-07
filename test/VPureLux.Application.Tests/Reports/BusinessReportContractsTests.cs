using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Shouldly;
using VPureLux.Reports;
using Volo.Abp.Application.Dtos;
using Xunit;

namespace VPureLux.Reports;

public class BusinessReportContractsTests
{
    [Fact]
    public void S005_contracts_preserve_unknown_cost_and_source_identity()
    {
        ((byte)BusinessRevenueSource.Sales).ShouldBe((byte)1); ((byte)BusinessRevenueSource.Service).ShouldBe((byte)2);
        new BusinessRevenueRowDto().Profit.ShouldBeNull(); new BusinessRevenueRowDto().LaborCost.ShouldBeNull();
        new GetBusinessRevenueListInput().ShouldBeAssignableTo<PagedAndSortedResultRequestDto>();
        var errors = new List<ValidationResult>();
        var input = new GetBusinessRevenueListInput { SearchText = new string('x', 129) };
        Validator.TryValidateObject(input, new ValidationContext(input), errors, true).ShouldBeFalse();
    }
}
