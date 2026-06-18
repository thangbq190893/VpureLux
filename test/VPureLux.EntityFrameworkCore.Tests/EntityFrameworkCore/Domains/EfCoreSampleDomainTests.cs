using VPureLux.Samples;
using Xunit;

namespace VPureLux.EntityFrameworkCore.Domains;

[Collection(VPureLuxTestConsts.CollectionDefinitionName)]
public class EfCoreSampleDomainTests : SampleDomainTests<VPureLuxEntityFrameworkCoreTestModule>
{

}
