using System;
using Volo.Abp;
using Volo.Abp.Domain.Entities.Auditing;

namespace VPureLux.OperatingCosts;

public class OperatingCostCategory : FullAuditedAggregateRoot<Guid>
{
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public OperatingCostDirection Direction { get; private set; }
    public bool IsActive { get; private set; }

    protected OperatingCostCategory()
    {
    }

    internal OperatingCostCategory(Guid id, string code, string name, OperatingCostDirection direction)
        : base(id)
    {
        Code = NormalizeCode(code);
        SetInfo(name, direction);
        IsActive = true;
    }

    public void UpdateInfo(string name, OperatingCostDirection direction, bool isActive)
    {
        SetInfo(name, direction);
        IsActive = isActive;
    }

    public void ChangeCode(string code)
    {
        Code = NormalizeCode(code);
    }

    public static string NormalizeCode(string code)
    {
        return Check.NotNullOrWhiteSpace(code, nameof(code), OperatingCostConsts.MaxCategoryCodeLength)
            .Trim()
            .ToUpperInvariant();
    }

    private void SetInfo(string name, OperatingCostDirection direction)
    {
        Name = Check.NotNullOrWhiteSpace(name, nameof(name), OperatingCostConsts.MaxCategoryNameLength).Trim();

        if (!Enum.IsDefined(direction))
        {
            throw new BusinessException(VPureLuxDomainErrorCodes.ValidationFailed)
                .WithData(nameof(direction), direction);
        }

        Direction = direction;
    }
}
