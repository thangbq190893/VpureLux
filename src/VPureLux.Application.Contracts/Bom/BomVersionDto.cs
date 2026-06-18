using System;
using System.Collections.Generic;
using Volo.Abp.Application.Dtos;

namespace VPureLux.Bom;

public class BomVersionDto : EntityDto<Guid>
{
    public Guid ProductId { get; set; }
    public int VersionNo { get; set; }
    public BomStatus Status { get; set; }
    public DateTime EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    public List<BomItemDto> Items { get; set; } = new();
}
