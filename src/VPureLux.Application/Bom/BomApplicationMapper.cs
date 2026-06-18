using Riok.Mapperly.Abstractions;
using Volo.Abp.DependencyInjection;

namespace VPureLux.Bom;

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
public partial class BomApplicationMapper : ITransientDependency
{
    public partial BomVersionDto ToDto(BomVersion bomVersion);

    private static int MapVersionNo(BomVersionNo versionNo)
    {
        return versionNo.Value;
    }
}
