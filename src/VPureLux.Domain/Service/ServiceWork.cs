using System;
using Volo.Abp;
using Volo.Abp.Domain.Entities.Auditing;

namespace VPureLux.Service;

public class ServiceWork : FullAuditedAggregateRoot<Guid>
{
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public decimal DefaultPrice { get; private set; }
    public ServiceWorkStatus Status { get; private set; }
    public string? Note { get; private set; }

    protected ServiceWork()
    {
    }

    public ServiceWork(Guid id, string code, string name, decimal defaultPrice, string? note = null) : base(id)
    {
        Code = Check.NotNullOrWhiteSpace(code, nameof(code), ServiceConsts.MaxCodeLength).Trim().ToUpperInvariant();
        Update(name, defaultPrice, note);
        Status = ServiceWorkStatus.Active;
    }

    public void Update(string name, decimal defaultPrice, string? note)
    {
        if (defaultPrice < 0)
        {
            throw new BusinessException(VPureLuxDomainErrorCodes.ValidationFailed);
        }

        Name = Check.NotNullOrWhiteSpace(name, nameof(name), ServiceConsts.MaxNameLength).Trim();
        DefaultPrice = decimal.Round(defaultPrice, ServiceConsts.MoneyScale, MidpointRounding.AwayFromZero);
        Note = string.IsNullOrWhiteSpace(note)
            ? null
            : Check.Length(note.Trim(), nameof(note), ServiceConsts.MaxNoteLength);
    }

    public void Activate() => Status = ServiceWorkStatus.Active;

    public void Deactivate() => Status = ServiceWorkStatus.Inactive;
}
