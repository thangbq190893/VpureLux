using System.Linq;
using Shouldly;
using VPureLux.Pricing;
using Xunit;

namespace VPureLux.EntityFrameworkCore.Pricing;

public class PricingBoundaryTests
{
    [Fact]
    public void Pricing_Should_Not_Reference_Inventory_Or_Sales_Assemblies()
    {
        var references = typeof(PricingManager).Assembly.GetReferencedAssemblies().Select(x => x.Name).ToArray();

        references.ShouldNotContain(x => x != null && x.Contains("Inventory"));
        references.ShouldNotContain(x => x != null && x.Contains("Sales"));
    }

    [Fact]
    public void Pricing_Should_Not_Expose_FIFO_Snapshot_Or_Profit_Concepts()
    {
        var forbiddenNames = new[]
        {
            "InventoryLot",
            "UnitCost",
            "CostPriceSnapshot",
            "ActualSellingPrice",
            "Profit",
            "Margin",
            "CustomerId",
            "CustomerGroupId"
        };
        var publicMembers = typeof(PricingManager).Assembly.GetExportedTypes()
            .Where(x => x.Namespace?.StartsWith("VPureLux.Pricing") == true)
            .SelectMany(x => x.GetMembers())
            .Select(x => x.Name)
            .ToArray();

        foreach (var forbiddenName in forbiddenNames)
        {
            publicMembers.ShouldNotContain(forbiddenName);
        }
    }
}
