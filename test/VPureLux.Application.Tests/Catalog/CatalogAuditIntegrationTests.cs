using System.Linq;
using Shouldly;
using VPureLux.Catalog.Events;
using Volo.Abp.EventBus;
using Xunit;

namespace VPureLux.Catalog;

public class CatalogAuditIntegrationTests
{
    [Fact]
    public void Audit_Handler_Should_Cover_All_Catalog_Events()
    {
        var interfaces = typeof(CatalogBusinessAuditEventHandler).GetInterfaces();

        interfaces.ShouldContain(typeof(ILocalEventHandler<ComponentCreatedEvent>));
        interfaces.ShouldContain(typeof(ILocalEventHandler<ComponentUpdatedEvent>));
        interfaces.ShouldContain(typeof(ILocalEventHandler<ComponentDeactivatedEvent>));
        interfaces.ShouldContain(typeof(ILocalEventHandler<ProductCreatedEvent>));
        interfaces.ShouldContain(typeof(ILocalEventHandler<ProductUpdatedEvent>));
        interfaces.ShouldContain(typeof(ILocalEventHandler<ProductDeactivatedEvent>));
        interfaces.ShouldContain(typeof(ILocalEventHandler<ProductImageChangedEvent>));
        interfaces.ShouldContain(typeof(ILocalEventHandler<ProductImageRemovedEvent>));
        interfaces.ShouldContain(typeof(ILocalEventHandler<ComponentImageChangedEvent>));
        interfaces.ShouldContain(typeof(ILocalEventHandler<ComponentImageRemovedEvent>));
    }
}
