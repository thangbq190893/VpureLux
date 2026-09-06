using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using VPureLux.Customers;
using VPureLux.Customers.CustomerGroups;
using Volo.Abp.EntityFrameworkCore;
using Xunit;
using Xunit.Abstractions;

namespace VPureLux.EntityFrameworkCore.Warranty;

[Collection(VPureLuxTestConsts.CollectionDefinitionName)]
public class CustomerCareUatSafetyTests : VPureLuxEntityFrameworkCoreTestBase
{
    private readonly ITestOutputHelper _output;

    public CustomerCareUatSafetyTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public async Task Creating_Uat_Customer_Should_Preserve_Group_Business_Fields_And_Expose_Metadata_Touches()
    {
        var group = await GetRequiredService<ICustomerGroupAppService>().CreateAsync(new CreateCustomerGroupDto
        {
            Code = "W008-" + Guid.NewGuid().ToString("N")[..12],
            Name = "Isolated UAT group",
            Description = "Never reuse a legacy group for a zero-row-change rehearsal",
            SortOrder = 999
        });
        var before = await ReadGroupAsync(group.Id);
        await GetRequiredService<ICustomerAppService>().CreateAsync(new CreateCustomerDto
        {
            Code = "W008-" + Guid.NewGuid().ToString("N")[..12],
            Name = "Isolated synthetic customer",
            CustomerGroupId = group.Id
        });
        var after = await ReadGroupAsync(group.Id);

        after.Code.ShouldBe(before.Code);
        after.Name.ShouldBe(before.Name);
        after.Description.ShouldBe(before.Description);
        after.Status.ShouldBe(before.Status);
        after.SortOrder.ShouldBe(before.SortOrder);
        after.IsDeleted.ShouldBe(before.IsDeleted);
        after.CreationTime.ShouldBe(before.CreationTime);
        after.CreatorId.ShouldBe(before.CreatorId);
        _output.WriteLine("Group concurrency stamp changed: " + (after.ConcurrencyStamp != before.ConcurrencyStamp));
        _output.WriteLine("Group modification time changed: " + (after.LastModificationTime != before.LastModificationTime));
        _output.WriteLine("Group business fields unchanged; this in-memory result does not replace the VPL legacy fingerprint gate.");
    }

    private Task<CustomerGroup> ReadGroupAsync(Guid id) => WithUnitOfWorkAsync(async () =>
    {
        var db = await GetRequiredService<IDbContextProvider<VPureLuxDbContext>>().GetDbContextAsync();
        return await db.CustomerGroups.AsNoTracking().SingleAsync(group => group.Id == id);
    });
}
