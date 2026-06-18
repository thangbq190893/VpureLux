using System;
using System.Linq;
using System.Reflection;
using Shouldly;
using VPureLux.Customers.Events;
using Volo.Abp;
using Xunit;

namespace VPureLux.Customers;

public class CustomerDomainTests
{
    [Fact]
    public void Should_Create_Normalized_Active_Customer_With_Lightweight_Event()
    {
        var groupId = Guid.NewGuid();
        var customer = CreateCustomer(" retail-01 ", groupId);

        customer.Code.ShouldBe("RETAIL-01");
        customer.Status.ShouldBe(CustomerStatus.Active);
        var created = customer.GetLocalEvents().Select(x => x.EventData).OfType<CustomerCreatedEvent>().Single();
        created.CustomerId.ShouldBe(customer.Id);
        created.CustomerGroupId.ShouldBe(groupId);
    }

    [Fact]
    public void Customer_Code_Should_Be_Immutable()
    {
        typeof(Customer).GetProperty(nameof(Customer.Code))!.SetMethod!.IsPrivate.ShouldBeTrue();
        typeof(Customer).GetMethods(BindingFlags.Instance | BindingFlags.Public)
            .ShouldNotContain(x => x.Name.StartsWith("SetCode", StringComparison.OrdinalIgnoreCase) ||
                                   x.Name.StartsWith("ChangeCode", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Should_Update_Assign_Group_And_Transition_Status_With_Events()
    {
        var customer = CreateCustomer();
        var newGroupId = Guid.NewGuid();

        customer.UpdateInfo("Updated", "123", "a@example.com", "Address", "TAX", "Notes");
        customer.AssignGroup(newGroupId);
        customer.Deactivate();
        customer.Activate();

        customer.Name.ShouldBe("Updated");
        customer.CustomerGroupId.ShouldBe(newGroupId);
        customer.Status.ShouldBe(CustomerStatus.Active);
        var events = customer.GetLocalEvents().Select(x => x.EventData).ToList();
        events.OfType<CustomerUpdatedEvent>().ShouldHaveSingleItem();
        events.OfType<CustomerGroupChangedEvent>().ShouldHaveSingleItem();
        events.OfType<CustomerDeactivatedEvent>().ShouldHaveSingleItem();
        events.OfType<CustomerActivatedEvent>().ShouldHaveSingleItem();
    }

    [Fact]
    public void Should_Reject_Invalid_Customer_Invariants()
    {
        Should.Throw<ArgumentException>(() => CreateCustomer(code: " "));
        Should.Throw<ArgumentException>(() => CreateCustomer(name: " "));
        Should.Throw<ArgumentException>(() => CreateCustomer(groupId: Guid.Empty));
    }

    internal static Customer CreateCustomer(
        string code = "CUST-01",
        Guid? groupId = null,
        string name = "Customer")
    {
        return new Customer(Guid.NewGuid(), code, name, groupId ?? Guid.NewGuid(), null, null, null, null, null);
    }
}
