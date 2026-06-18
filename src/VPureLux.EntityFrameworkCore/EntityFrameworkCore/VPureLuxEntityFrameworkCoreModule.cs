using System;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Uow;
using Volo.Abp.AuditLogging.EntityFrameworkCore;
using Volo.Abp.BackgroundJobs.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore.SqlServer;
using Volo.Abp.FeatureManagement.EntityFrameworkCore;
using Volo.Abp.Identity.EntityFrameworkCore;
using Volo.Abp.OpenIddict.EntityFrameworkCore;
using Volo.Abp.Modularity;
using Volo.Abp.PermissionManagement.EntityFrameworkCore;
using Volo.Abp.SettingManagement.EntityFrameworkCore;
using Volo.Abp.BlobStoring.Database.EntityFrameworkCore;
using Volo.Abp.LanguageManagement.EntityFrameworkCore;
using Volo.FileManagement.EntityFrameworkCore;
using Volo.Abp.TextTemplateManagement.EntityFrameworkCore;
using Volo.Saas.EntityFrameworkCore;
using Volo.Abp.Gdpr;
using Volo.CmsKit.EntityFrameworkCore;
using Volo.Chat.EntityFrameworkCore;
using Volo.Abp.Studio;
using VPureLux.Bom;
using VPureLux.Customers;
using VPureLux.Pricing;
using VPureLux.Inventory;
using VPureLux.Sales;
using VPureLux.Audit;

namespace VPureLux.EntityFrameworkCore;

[DependsOn(
    typeof(VPureLuxDomainModule),
    typeof(AbpPermissionManagementEntityFrameworkCoreModule),
    typeof(AbpSettingManagementEntityFrameworkCoreModule),
    typeof(AbpEntityFrameworkCoreSqlServerModule),
    typeof(AbpBackgroundJobsEntityFrameworkCoreModule),
    typeof(AbpAuditLoggingEntityFrameworkCoreModule),
    typeof(AbpFeatureManagementEntityFrameworkCoreModule),
    typeof(AbpIdentityProEntityFrameworkCoreModule),
    typeof(AbpOpenIddictProEntityFrameworkCoreModule),
    typeof(LanguageManagementEntityFrameworkCoreModule),
    typeof(FileManagementEntityFrameworkCoreModule),
    typeof(SaasEntityFrameworkCoreModule),
    typeof(ChatEntityFrameworkCoreModule),
    typeof(TextTemplateManagementEntityFrameworkCoreModule),
    typeof(AbpGdprEntityFrameworkCoreModule),
    typeof(CmsKitProEntityFrameworkCoreModule),
    typeof(BlobStoringDatabaseEntityFrameworkCoreModule)
    )]
public class VPureLuxEntityFrameworkCoreModule : AbpModule
{
    public override void PreConfigureServices(ServiceConfigurationContext context)
    {

        VPureLuxEfCoreEntityExtensionMappings.Configure();
    }

    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.AddAbpDbContext<VPureLuxDbContext>(options =>
        {
                /* Remove "includeAllEntities: true" to create
                 * default repositories only for aggregate roots */
            options.AddDefaultRepositories(includeAllEntities: true);
            options.AddRepository<BomVersion, EfCoreBomVersionRepository>();
            options.AddRepository<Customer, EfCoreCustomerRepository>();
            options.AddRepository<CustomerGroup, EfCoreCustomerGroupRepository>();
            options.AddRepository<ComponentSuggestedSellingPriceVersion, EfCoreComponentSuggestedSellingPriceVersionRepository>();
            options.AddRepository<ProductSuggestedPriceVersion, EfCoreProductSuggestedPriceVersionRepository>();
            options.AddRepository<StockItem, EfCoreStockItemRepository>();
            options.AddRepository<Warehouse, EfCoreWarehouseRepository>();
            options.AddRepository<InventoryLot, EfCoreInventoryLotRepository>();
            options.AddRepository<InventoryTransaction, EfCoreInventoryTransactionRepository>();
            options.AddRepository<SalesOrder, EfCoreSalesOrderRepository>();
        });

        context.Services.AddTransient<IInventoryBalanceRepository, EfCoreInventoryBalanceRepository>();
        context.Services.AddTransient<IBusinessAuditLogRepository, EfCoreBusinessAuditLogRepository>();

        if (AbpStudioAnalyzeHelper.IsInAnalyzeMode)
        {
            return;
        }

        Configure<AbpDbContextOptions>(options =>
        {
            /* The main point to change your DBMS.
             * See also VPureLuxDbContextFactory for EF Core tooling. */

            options.UseSqlServer();

        });
        
    }
}
