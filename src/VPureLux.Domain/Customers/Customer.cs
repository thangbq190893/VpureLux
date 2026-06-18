using System;
using VPureLux.Customers.Events;
using Volo.Abp;
using Volo.Abp.Domain.Entities.Auditing;

namespace VPureLux.Customers;

public class Customer : FullAuditedAggregateRoot<Guid>
{
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public Guid CustomerGroupId { get; private set; }
    public CustomerStatus Status { get; private set; }
    public string? PhoneNumber { get; private set; }
    public string? Email { get; private set; }
    public string? Address { get; private set; }
    public string? TaxCode { get; private set; }
    public string? Notes { get; private set; }

    protected Customer()
    {
    }

    internal Customer(
        Guid id,
        string code,
        string name,
        Guid customerGroupId,
        string? phoneNumber,
        string? email,
        string? address,
        string? taxCode,
        string? notes)
        : base(id)
    {
        Code = NormalizeCode(code);
        SetInfo(name, phoneNumber, email, address, taxCode, notes);
        CustomerGroupId = Check.NotDefaultOrNull<Guid>(customerGroupId, nameof(customerGroupId));
        Status = CustomerStatus.Active;
        AddLocalEvent(new CustomerCreatedEvent(Id, Code, CustomerGroupId));
    }

    public void UpdateInfo(
        string name,
        string? phoneNumber,
        string? email,
        string? address,
        string? taxCode,
        string? notes)
    {
        SetInfo(name, phoneNumber, email, address, taxCode, notes);
        AddLocalEvent(new CustomerUpdatedEvent(Id, Code));
    }

    internal void AssignGroup(Guid customerGroupId)
    {
        customerGroupId = Check.NotDefaultOrNull<Guid>(customerGroupId, nameof(customerGroupId));
        if (CustomerGroupId == customerGroupId)
        {
            return;
        }

        var previousCustomerGroupId = CustomerGroupId;
        CustomerGroupId = customerGroupId;
        AddLocalEvent(new CustomerGroupChangedEvent(Id, previousCustomerGroupId, CustomerGroupId));
    }

    public void Activate()
    {
        if (Status == CustomerStatus.Active)
        {
            return;
        }

        Status = CustomerStatus.Active;
        AddLocalEvent(new CustomerActivatedEvent(Id, Code));
    }

    public void Deactivate()
    {
        if (Status == CustomerStatus.Inactive)
        {
            return;
        }

        Status = CustomerStatus.Inactive;
        AddLocalEvent(new CustomerDeactivatedEvent(Id, Code));
    }

    internal static string NormalizeCode(string code)
    {
        return Check.NotNullOrWhiteSpace(code, nameof(code), CustomerConsts.MaxCodeLength)
            .Trim()
            .ToUpperInvariant();
    }

    private void SetInfo(
        string name,
        string? phoneNumber,
        string? email,
        string? address,
        string? taxCode,
        string? notes)
    {
        Name = Check.NotNullOrWhiteSpace(name, nameof(name), CustomerConsts.MaxNameLength);
        PhoneNumber = NormalizeOptional(phoneNumber, nameof(phoneNumber), CustomerConsts.MaxPhoneNumberLength);
        Email = NormalizeOptional(email, nameof(email), CustomerConsts.MaxEmailLength);
        Address = NormalizeOptional(address, nameof(address), CustomerConsts.MaxAddressLength);
        TaxCode = NormalizeOptional(taxCode, nameof(taxCode), CustomerConsts.MaxTaxCodeLength);
        Notes = NormalizeOptional(notes, nameof(notes), CustomerConsts.MaxNotesLength);
    }

    private static string? NormalizeOptional(string? value, string name, int maxLength)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : Check.Length(value.Trim(), name, maxLength);
    }
}
