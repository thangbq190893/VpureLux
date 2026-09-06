using System;
using System.Linq;
using Shouldly;
using Xunit;

namespace VPureLux.Service;

public class ServiceOrderContractTests
{
    [Fact]
    public void Contract_should_expose_atomic_order_completion_without_individual_line_completion()
    {
        var methods = typeof(IServiceOrderAppService).GetMethods().Select(method => method.Name).ToList();

        methods.ShouldContain(nameof(IServiceOrderAppService.CreateAsync));
        methods.ShouldContain(nameof(IServiceOrderAppService.UpdateAsync));
        methods.ShouldContain(nameof(IServiceOrderAppService.ConfirmAsync));
        methods.ShouldContain(nameof(IServiceOrderAppService.StartAsync));
        methods.ShouldContain(nameof(IServiceOrderAppService.CancelAsync));
        methods.ShouldContain("CompleteAsync");
        methods.ShouldNotContain("CompleteLineAsync");
    }

    [Fact]
    public void Line_contract_should_preserve_identity_and_nullable_standard_cost()
    {
        var lineId = Guid.NewGuid();
        var input = new ServiceOrderLineInput { Id = lineId };
        var output = new ServiceOrderLineDto { Id = lineId, StandardCostSnapshot = null };

        input.Id.ShouldBe(lineId);
        output.Id.ShouldBe(lineId);
        output.StandardCostSnapshot.ShouldBeNull();
        typeof(ServiceOrderLineDto).GetProperty(nameof(ServiceOrderLineDto.StandardCostSnapshot))!
            .PropertyType.ShouldBe(typeof(decimal?));
    }
}
