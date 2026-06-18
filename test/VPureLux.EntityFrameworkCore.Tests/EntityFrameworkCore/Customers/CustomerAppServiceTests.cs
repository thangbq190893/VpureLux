using System;
using System.Threading.Tasks;
using Shouldly;
using VPureLux.Customers;
using VPureLux.Customers.CustomerGroups;
using Volo.Abp;
using Xunit;

namespace VPureLux.EntityFrameworkCore.Customers;

[Collection(VPureLuxTestConsts.CollectionDefinitionName)]
public class CustomerAppServiceTests : VPureLuxEntityFrameworkCoreTestBase
{
    private readonly ICustomerAppService _customerService;
    private readonly ICustomerGroupAppService _groupService;

    public CustomerAppServiceTests()
    {
        _customerService = GetRequiredService<ICustomerAppService>();
        _groupService = GetRequiredService<ICustomerGroupAppService>();
    }

    [Fact]
    public async Task Should_Create_Update_Deactivate_And_Activate_With_Group_Mapping()
    {
        var group = await CreateGroupAsync();
        var customer = await _customerService.CreateAsync(CreateInput(group.Id));
        customer.CustomerGroupCode.ShouldBe(group.Code);
        customer.CustomerGroupName.ShouldBe(group.Name);

        var updated = await _customerService.UpdateAsync(customer.Id, new UpdateCustomerDto
        {
            Name = "Updated Customer",
            CustomerGroupId = group.Id,
            Email = "updated@example.com"
        });
        updated.Name.ShouldBe("Updated Customer");

        await _customerService.DeactivateAsync(customer.Id);
        (await _customerService.GetAsync(customer.Id)).Status.ShouldBe(CustomerStatus.Inactive);
        await _customerService.ActivateAsync(customer.Id);
        (await _customerService.GetAsync(customer.Id)).Status.ShouldBe(CustomerStatus.Active);
    }

    [Fact]
    public async Task Should_Reject_Duplicate_Customer_And_Group_Codes()
    {
        var group = await CreateGroupAsync();
        var input = CreateInput(group.Id);
        await _customerService.CreateAsync(input);
        (await Should.ThrowAsync<BusinessException>(() => _customerService.CreateAsync(input)))
            .Code.ShouldBe(VPureLuxDomainErrorCodes.CustomerCodeAlreadyExists);

        (await Should.ThrowAsync<BusinessException>(() => _groupService.CreateAsync(new CreateCustomerGroupDto
        {
            Code = group.Code,
            Name = "Duplicate"
        }))).Code.ShouldBe(VPureLuxDomainErrorCodes.CustomerGroupCodeAlreadyExists);
    }

    [Fact]
    public async Task Should_Reject_Missing_And_Inactive_Groups()
    {
        (await Should.ThrowAsync<BusinessException>(() => _customerService.CreateAsync(CreateInput(Guid.NewGuid()))))
            .Code.ShouldBe(VPureLuxDomainErrorCodes.CustomerGroupNotFound);

        var group = await CreateGroupAsync();
        await _groupService.DeactivateAsync(group.Id);
        (await Should.ThrowAsync<BusinessException>(() => _customerService.CreateAsync(CreateInput(group.Id))))
            .Code.ShouldBe(VPureLuxDomainErrorCodes.CustomerGroupInactive);
    }

    [Fact]
    public async Task Should_Not_Invalidate_Customer_When_Group_Is_Deactivated()
    {
        var group = await CreateGroupAsync();
        var customer = await _customerService.CreateAsync(CreateInput(group.Id));

        await _groupService.DeactivateAsync(group.Id);

        var historicalCustomer = await _customerService.GetAsync(customer.Id);
        historicalCustomer.CustomerGroupId.ShouldBe(group.Id);
        historicalCustomer.Status.ShouldBe(CustomerStatus.Active);
        (await Should.ThrowAsync<BusinessException>(() => _customerService.ActivateAsync(customer.Id)))
            .Code.ShouldBe(VPureLuxDomainErrorCodes.CustomerGroupInactive);
    }

    [Fact]
    public async Task Should_Return_Customer_Not_Found()
    {
        (await Should.ThrowAsync<BusinessException>(() => _customerService.GetAsync(Guid.NewGuid())))
            .Code.ShouldBe(VPureLuxDomainErrorCodes.CustomerNotFound);
    }

    private Task<CustomerGroupDto> CreateGroupAsync() =>
        _groupService.CreateAsync(new CreateCustomerGroupDto { Code = Unique("GRP"), Name = "Group", SortOrder = 1 });

    private static CreateCustomerDto CreateInput(Guid groupId) => new()
    {
        Code = Unique("CUS"),
        Name = "Customer",
        CustomerGroupId = groupId,
        Email = "customer@example.com"
    };

    private static string Unique(string prefix) => prefix + Guid.NewGuid().ToString("N")[..8];
}
