using System;
using Volo.Abp;
using Volo.Abp.Domain.Entities.Auditing;

namespace VPureLux.Suppliers;

public class Supplier : FullAuditedAggregateRoot<Guid>
{
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string? ContactName { get; private set; }
    public string? Phone { get; private set; }
    public string? Email { get; private set; }
    public string? TaxCode { get; private set; }
    public string? Address { get; private set; }
    public string? Note { get; private set; }

    protected Supplier()
    {
    }

    internal Supplier(
        Guid id,
        string code,
        string name,
        string? contactName,
        string? phone,
        string? email,
        string? taxCode,
        string? address,
        string? note)
        : base(id)
    {
        Code = NormalizeCode(code);
        SetInfo(name, contactName, phone, email, taxCode, address, note);
    }

    public void UpdateInfo(
        string name,
        string? contactName,
        string? phone,
        string? email,
        string? taxCode,
        string? address,
        string? note)
    {
        SetInfo(name, contactName, phone, email, taxCode, address, note);
    }

    public static string NormalizeCode(string code)
    {
        return Check.NotNullOrWhiteSpace(code, nameof(code), SupplierConsts.MaxCodeLength)
            .Trim()
            .ToUpperInvariant();
    }

    private void SetInfo(
        string name,
        string? contactName,
        string? phone,
        string? email,
        string? taxCode,
        string? address,
        string? note)
    {
        Name = Check.NotNullOrWhiteSpace(name, nameof(name), SupplierConsts.MaxNameLength).Trim();
        ContactName = NormalizeOptional(contactName, nameof(contactName), SupplierConsts.MaxContactNameLength);
        Phone = NormalizeOptional(phone, nameof(phone), SupplierConsts.MaxPhoneLength);
        Email = NormalizeOptional(email, nameof(email), SupplierConsts.MaxEmailLength);
        TaxCode = NormalizeOptional(taxCode, nameof(taxCode), SupplierConsts.MaxTaxCodeLength);
        Address = NormalizeOptional(address, nameof(address), SupplierConsts.MaxAddressLength);
        Note = NormalizeOptional(note, nameof(note), SupplierConsts.MaxNoteLength);
    }

    private static string? NormalizeOptional(string? value, string parameterName, int maxLength)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : Check.Length(value.Trim(), parameterName, maxLength);
    }
}
