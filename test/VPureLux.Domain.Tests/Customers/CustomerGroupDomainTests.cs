using System;
using System.Linq;
using System.Reflection;
using Shouldly;
using VPureLux.Customers.Events;
using Volo.Abp;
using Xunit;

namespace VPureLux.Customers;

public class CustomerGroupDomainTests
{
    [Fact]
    public void Should_Create_Normalized_Active_Group_And_Update_Status()
    {
        var group = CreateGroup(" dealer ");
        group.UpdateInfo("Dealer Updated", "Description", 20);
        group.Deactivate();
        group.Activate();

        group.Code.ShouldBe("DEALER");
        group.Name.ShouldBe("Dealer Updated");
        group.Status.ShouldBe(CustomerGroupStatus.Active);
        var events = group.GetLocalEvents().Select(x => x.EventData).ToList();
        events.OfType<CustomerGroupCreatedEvent>().ShouldHaveSingleItem();
        events.OfType<CustomerGroupUpdatedEvent>().ShouldHaveSingleItem();
        events.OfType<CustomerGroupDeactivatedEvent>().ShouldHaveSingleItem();
        events.OfType<CustomerGroupActivatedEvent>().ShouldHaveSingleItem();
    }

    [Fact]
    public void CustomerGroup_Code_Should_Be_Immutable()
    {
        typeof(CustomerGroup).GetProperty(nameof(CustomerGroup.Code))!.SetMethod!.IsPrivate.ShouldBeTrue();
        typeof(CustomerGroup).GetMethods(BindingFlags.Instance | BindingFlags.Public)
            .ShouldNotContain(x => x.Name.StartsWith("SetCode", StringComparison.OrdinalIgnoreCase) ||
                                   x.Name.StartsWith("ChangeCode", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Should_Reject_Invalid_Group_Invariants()
    {
        Should.Throw<ArgumentException>(() => CreateGroup(" "));
        Should.Throw<ArgumentException>(() => new CustomerGroup(Guid.NewGuid(), "VALID", " ", null, 0));
        Should.Throw<BusinessException>(() => new CustomerGroup(Guid.NewGuid(), "VALID", "Valid", null, -1))
            .Code.ShouldBe(VPureLuxDomainErrorCodes.ValidationFailed);
    }

    [Fact]
    public void Customer_Error_Codes_Should_Match_Specification()
    {
        VPureLuxDomainErrorCodes.CustomerCodeAlreadyExists.ShouldBe("CUSTOMER_001");
        VPureLuxDomainErrorCodes.CustomerNotFound.ShouldBe("CUSTOMER_002");
        VPureLuxDomainErrorCodes.CustomerInactive.ShouldBe("CUSTOMER_003");
        VPureLuxDomainErrorCodes.CustomerGroupNotFound.ShouldBe("CUSTOMER_004");
        VPureLuxDomainErrorCodes.CustomerGroupInactive.ShouldBe("CUSTOMER_005");
        VPureLuxDomainErrorCodes.CustomerGroupCodeAlreadyExists.ShouldBe("CUSTOMER_006");
        VPureLuxDomainErrorCodes.CustomerGroupIsInUse.ShouldBe("CUSTOMER_007");
    }

    internal static CustomerGroup CreateGroup(string code = "RETAIL")
    {
        return new CustomerGroup(Guid.NewGuid(), code, "Retail", null, 10);
    }
}
