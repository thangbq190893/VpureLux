using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using VPureLux.Customers;
using Volo.Abp;
using Volo.Abp.Data;
using Xunit;

namespace VPureLux.EntityFrameworkCore.Customers;

[Collection(VPureLuxTestConsts.CollectionDefinitionName)]
public class CustomerRepositoryAndSeedTests : VPureLuxEntityFrameworkCoreTestBase
{
    private readonly ICustomerRepository _customers;
    private readonly ICustomerGroupRepository _groups;
    private readonly CustomerManager _customerManager;
    private readonly CustomerGroupManager _groupManager;
    private readonly CustomerGroupDataSeedContributor _seedContributor;

    public CustomerRepositoryAndSeedTests()
    {
        _customers = GetRequiredService<ICustomerRepository>();
        _groups = GetRequiredService<ICustomerGroupRepository>();
        _customerManager = GetRequiredService<CustomerManager>();
        _groupManager = GetRequiredService<CustomerGroupManager>();
        _seedContributor = GetRequiredService<CustomerGroupDataSeedContributor>();
    }

    [Fact]
    public async Task Should_Persist_Filter_And_Soft_Delete_Customer()
    {
        await WithUnitOfWorkAsync(async () =>
        {
            var group = await CreateGroupAsync();
            var customer = await _customerManager.CreateAsync(Unique("CUS"), "Searchable Customer", group.Id, email: "search@example.com");
            await _customers.InsertAsync(customer, autoSave: true);

            (await _customers.GetListAsync("Searchable", group.Id, CustomerStatus.Active)).ShouldHaveSingleItem();
            (await _customers.GetCountAsync("search@example.com")).ShouldBe(1);

            await _customers.DeleteAsync(customer, autoSave: true);
            (await _customers.FindAsync(customer.Id)).ShouldBeNull();
        });
    }

    [Fact]
    public async Task Should_Enforce_Unique_Indexes_And_CustomerGroup_FK()
    {
        await Should.ThrowAsync<DbUpdateException>(() => WithUnitOfWorkAsync(async () =>
        {
            var group = await CreateGroupAsync();
            var duplicate = await _groupManager.CreateAsync(Unique("GRP"), "Duplicate");
            SetPrivateProperty(duplicate, nameof(CustomerGroup.Code), group.Code);
            await _groups.InsertAsync(duplicate, autoSave: true);
        }));

        await Should.ThrowAsync<DbUpdateException>(() => WithUnitOfWorkAsync(async () =>
        {
            var group = await CreateGroupAsync();
            var customer = await _customerManager.CreateAsync(Unique("CUS"), "Invalid FK", group.Id);
            SetPrivateProperty(customer, nameof(Customer.CustomerGroupId), Guid.NewGuid());
            await _customers.InsertAsync(customer, autoSave: true);
        }));
    }

    [Fact]
    public async Task Should_Enforce_Customer_007_Through_Repository_Contract()
    {
        await WithUnitOfWorkAsync(async () =>
        {
            var group = await CreateGroupAsync();
            var customer = await _customerManager.CreateAsync(Unique("CUS"), "Customer", group.Id);
            await _customers.InsertAsync(customer, autoSave: true);

            (await _customers.HasCustomersInGroupAsync(group.Id)).ShouldBeTrue();
            (await Should.ThrowAsync<BusinessException>(() => _groupManager.EnsureCanDeleteAsync(group)))
                .Code.ShouldBe(VPureLuxDomainErrorCodes.CustomerGroupIsInUse);
        });
    }

    [Fact]
    public async Task Seed_Should_Be_Idempotent_And_Preserve_Customized_Name()
    {
        await WithUnitOfWorkAsync(async () =>
        {
            await _seedContributor.SeedAsync(new DataSeedContext());
            var before = await _groups.GetCountAsync();
            var retail = (await _groups.FindByCodeAsync("RETAIL"))!;
            retail.UpdateInfo("Customized Retail", retail.Description, retail.SortOrder);
            await _groups.UpdateAsync(retail, autoSave: true);

            await _seedContributor.SeedAsync(new DataSeedContext());

            (await _groups.GetCountAsync()).ShouldBe(before);
            (await _groups.FindByCodeAsync("RETAIL"))!.Name.ShouldBe("Customized Retail");
            (await _groups.GetListAsync()).Select(x => x.Code).Distinct().Count().ShouldBe((int)before);
        });
    }

    private async Task<CustomerGroup> CreateGroupAsync()
    {
        var group = await _groupManager.CreateAsync(Unique("GRP"), "Group");
        return await _groups.InsertAsync(group, autoSave: true);
    }

    private static string Unique(string prefix) => prefix + Guid.NewGuid().ToString("N")[..8];

    private static void SetPrivateProperty<T>(T instance, string propertyName, object value)
    {
        typeof(T).GetProperty(propertyName)!.SetValue(instance, value);
    }
}
