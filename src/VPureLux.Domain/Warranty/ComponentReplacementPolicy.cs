using System;
using Volo.Abp;
using Volo.Abp.Domain.Entities.Auditing;

namespace VPureLux.Warranty;

public class ComponentReplacementPolicy : FullAuditedAggregateRoot<Guid>
{
    public Guid ComponentId { get; private set; }
    public bool IsEnabled { get; private set; }
    public int CycleMonths { get; private set; }
    public int WarningDaysBeforeDue { get; private set; }
    public string? Note { get; private set; }

    protected ComponentReplacementPolicy()
    {
    }

    public ComponentReplacementPolicy(
        Guid id,
        Guid componentId,
        int cycleMonths,
        int warningDaysBeforeDue,
        string? note,
        bool isEnabled = true)
        : base(id)
    {
        ComponentId = Check.NotDefaultOrNull<Guid>(componentId, nameof(componentId));
        Update(cycleMonths, warningDaysBeforeDue, note, isEnabled);
    }

    public void Update(int cycleMonths, int warningDaysBeforeDue, string? note, bool isEnabled)
    {
        if (cycleMonths <= 0 || warningDaysBeforeDue < 0)
        {
            throw new BusinessException(VPureLuxDomainErrorCodes.ValidationFailed);
        }

        CycleMonths = cycleMonths;
        WarningDaysBeforeDue = warningDaysBeforeDue;
        Note = Check.Length(note, nameof(note), WarrantyConsts.MaxNoteLength);
        IsEnabled = isEnabled;
    }
}
