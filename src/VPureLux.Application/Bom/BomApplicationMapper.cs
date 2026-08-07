using System.Linq;
using Volo.Abp.DependencyInjection;

namespace VPureLux.Bom;

public class BomApplicationMapper : ITransientDependency
{
    public BomVersionDto ToDto(BomVersion bomVersion)
    {
        return new BomVersionDto
        {
            Id = bomVersion.Id,
            ProductId = bomVersion.ProductId,
            VersionNo = bomVersion.VersionNo.Value,
            Status = bomVersion.Status,
            EffectiveFrom = bomVersion.EffectiveFrom,
            EffectiveTo = bomVersion.EffectiveTo,
            Items = bomVersion.OrderedItems.Select(ToDto).ToList()
        };
    }

    private static BomItemDto ToDto(BomItem item) => new()
    {
        Id = item.Id,
        ComponentId = item.ComponentId,
        LineNo = item.LineNo,
        Quantity = item.Quantity
    };
}
