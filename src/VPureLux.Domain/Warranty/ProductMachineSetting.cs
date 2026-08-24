using System;
using Volo.Abp;
using Volo.Abp.Domain.Entities.Auditing;

namespace VPureLux.Warranty;

public class ProductMachineSetting : FullAuditedAggregateRoot<Guid>
{
    public Guid ProductId { get; private set; }
    public bool IsMachine { get; private set; }
    public string? Note { get; private set; }
    public byte[] RowVersion { get; private set; } = Array.Empty<byte>();

    protected ProductMachineSetting()
    {
    }

    public ProductMachineSetting(Guid id, Guid productId, bool isMachine, string? note = null)
        : base(id)
    {
        ProductId = Check.NotDefaultOrNull<Guid>(productId, nameof(productId));
        Update(isMachine, note);
    }

    public void Update(bool isMachine, string? note)
    {
        IsMachine = isMachine;
        Note = Check.Length(note, nameof(note), WarrantyConsts.MaxNoteLength);
    }
}
