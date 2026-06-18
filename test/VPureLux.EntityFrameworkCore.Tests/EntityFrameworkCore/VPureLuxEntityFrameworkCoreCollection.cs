using Xunit;

namespace VPureLux.EntityFrameworkCore;

[CollectionDefinition(VPureLuxTestConsts.CollectionDefinitionName)]
public class VPureLuxEntityFrameworkCoreCollection : ICollectionFixture<VPureLuxEntityFrameworkCoreFixture>
{

}
