using System;
using Volo.Abp;
using Volo.Abp.Domain.Entities.Auditing;

namespace VPureLux.Service;

public class ServiceWork : FullAuditedAggregateRoot<Guid>
{
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    // Legacy work rows have no known unit; new or explicitly edited work must supply it.
    public string? Unit { get; private set; }
    public decimal DefaultPrice { get; private set; }
    public decimal? StandardCost { get; private set; }
    public ServiceWorkStatus Status { get; private set; }
    public string? Note { get; private set; }

    protected ServiceWork() { }

    public ServiceWork(Guid id, string code, string name, string unit, decimal defaultPrice,
        decimal? standardCost, ServiceWorkStatus status, string? note = null) : base(id)
    {
        Code = Check.NotNullOrWhiteSpace(code, nameof(code), ServiceConsts.MaxCodeLength).Trim().ToUpperInvariant();
        Update(name, unit, defaultPrice, standardCost, status, note);
    }

    public void Update(string name, string unit, decimal defaultPrice, decimal? standardCost,
        ServiceWorkStatus status, string? note)
    {
        if (!Enum.IsDefined(status))
            throw new BusinessException(ServiceErrorCodes.InvalidWork).WithData("Field", nameof(status));

        var checkedName = Check.NotNullOrWhiteSpace(name, nameof(name), ServiceConsts.MaxNameLength).Trim();
        var checkedUnit = Check.NotNullOrWhiteSpace(unit, nameof(unit), ServiceConsts.MaxUnitLength).Trim();
        var price = ValidateMoney(defaultPrice, nameof(defaultPrice));
        var cost = standardCost.HasValue ? ValidateMoney(standardCost.Value, nameof(standardCost)) : (decimal?)null;
        var checkedNote = string.IsNullOrWhiteSpace(note) ? null : Check.Length(note.Trim(), nameof(note), ServiceConsts.MaxNoteLength);
        Name = checkedName;
        Unit = checkedUnit;
        DefaultPrice = price;
        StandardCost = cost;
        Status = status;
        Note = checkedNote;
    }

    private static decimal ValidateMoney(decimal value, string field)
    {
        if (value < 0 || value > 9999999999999999.99m)
            throw new BusinessException(ServiceErrorCodes.InvalidWork).WithData("Field", field).WithData("Value", value);
        return decimal.Round(value, ServiceConsts.MoneyScale, MidpointRounding.AwayFromZero);
    }
}
