using System;
using VPureLux.Customers.Events;
using Volo.Abp;
using Volo.Abp.Domain.Entities.Auditing;

namespace VPureLux.Customers;

public class CustomerGroup : FullAuditedAggregateRoot<Guid>
{
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public CustomerGroupStatus Status { get; private set; }
    public int SortOrder { get; private set; }

    protected CustomerGroup()
    {
    }

    internal CustomerGroup(Guid id, string code, string name, string? description, int sortOrder)
        : base(id)
    {
        Code = NormalizeCode(code);
        SetInfo(name, description, sortOrder);
        Status = CustomerGroupStatus.Active;
        AddLocalEvent(new CustomerGroupCreatedEvent(Id, Code, Name));
    }

    public void UpdateInfo(string name, string? description, int sortOrder)
    {
        SetInfo(name, description, sortOrder);
        AddLocalEvent(new CustomerGroupUpdatedEvent(Id, Code, Name));
    }

    public void Activate()
    {
        if (Status == CustomerGroupStatus.Active)
        {
            return;
        }

        Status = CustomerGroupStatus.Active;
        AddLocalEvent(new CustomerGroupActivatedEvent(Id, Code));
    }

    public void Deactivate()
    {
        if (Status == CustomerGroupStatus.Inactive)
        {
            return;
        }

        Status = CustomerGroupStatus.Inactive;
        AddLocalEvent(new CustomerGroupDeactivatedEvent(Id, Code));
    }

    internal static string NormalizeCode(string code)
    {
        return Check.NotNullOrWhiteSpace(code, nameof(code), CustomerGroupConsts.MaxCodeLength)
            .Trim()
            .ToUpperInvariant();
    }

    private void SetInfo(string name, string? description, int sortOrder)
    {
        Name = Check.NotNullOrWhiteSpace(name, nameof(name), CustomerGroupConsts.MaxNameLength);
        Description = string.IsNullOrWhiteSpace(description)
            ? null
            : Check.Length(description.Trim(), nameof(description), CustomerGroupConsts.MaxDescriptionLength);

        if (sortOrder < 0)
        {
            throw new BusinessException(VPureLuxDomainErrorCodes.ValidationFailed)
                .WithData(nameof(sortOrder), sortOrder);
        }

        SortOrder = sortOrder;
    }
}
