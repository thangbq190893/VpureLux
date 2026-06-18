using System;
using Volo.Abp.Application.Dtos;

namespace VPureLux.Bom;

public class BomItemDto : EntityDto<Guid>
{
    public Guid ComponentId { get; set; }
    public decimal Quantity { get; set; }
}
