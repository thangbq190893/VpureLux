using VPureLux.Samples;
using Xunit;

namespace VPureLux.EntityFrameworkCore.Applications;

[Collection(VPureLuxTestConsts.CollectionDefinitionName)]
public class EfCoreSampleAppServiceTests : SampleAppServiceTests<VPureLuxEntityFrameworkCoreTestModule>
{

}
