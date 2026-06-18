using System;
using Microsoft.EntityFrameworkCore;
using Volo.Abp.AuditLogging.EntityFrameworkCore;
using Volo.Abp.BackgroundJobs.EntityFrameworkCore;
using Volo.Abp.BlobStoring.Database.EntityFrameworkCore;
using Volo.Abp.Data;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore.Modeling;
using Volo.Abp.FeatureManagement.EntityFrameworkCore;
using Volo.Abp.Identity;
using Volo.Abp.Identity.UserInvitations;
using Volo.Abp.Identity.EntityFrameworkCore;
using Volo.Abp.PermissionManagement.EntityFrameworkCore;
using Volo.Abp.SettingManagement.EntityFrameworkCore;
using Volo.Abp.OpenIddict.EntityFrameworkCore;
using Volo.Abp.LanguageManagement.EntityFrameworkCore;
using Volo.FileManagement.EntityFrameworkCore;
using Volo.Chat.EntityFrameworkCore;
using Volo.Abp.TextTemplateManagement.EntityFrameworkCore;
using Volo.Saas.EntityFrameworkCore;
using Volo.Saas.Editions;
using Volo.Saas.Tenants;
using Volo.Abp.Gdpr;
using Volo.CmsKit.EntityFrameworkCore;
using VPureLux.Catalog;
using VPureLux.EntityFrameworkCore.Catalog;
using VPureLux.Bom;
using VPureLux.Customers;
using VPureLux.Pricing;
using VPureLux.Inventory;
using VPureLux.Sales;
using VPureLux.Audit;

namespace VPureLux.EntityFrameworkCore;

[ReplaceDbContext(typeof(IIdentityProDbContext))]
[ReplaceDbContext(typeof(ISaasDbContext))]
[ConnectionStringName("Default")]
public class VPureLuxDbContext :
    AbpDbContext<VPureLuxDbContext>,
    ISaasDbContext,
    IIdentityProDbContext
{
    /* Add DbSet properties for your Aggregate Roots / Entities here. */

    public DbSet<Component> Components { get; set; }
    public DbSet<Product> Products { get; set; }
    public DbSet<BomVersion> BomVersions { get; set; }
    public DbSet<Customer> Customers { get; set; }
    public DbSet<CustomerGroup> CustomerGroups { get; set; }
    public DbSet<ComponentSuggestedSellingPriceVersion> ComponentSuggestedSellingPriceVersions { get; set; }
    public DbSet<ProductSuggestedPriceVersion> ProductSuggestedPriceVersions { get; set; }
    public DbSet<StockItem> StockItems { get; set; }
    public DbSet<Warehouse> Warehouses { get; set; }
    public DbSet<InventoryLot> InventoryLots { get; set; }
    public DbSet<InventoryTransaction> InventoryTransactions { get; set; }
    public DbSet<InventoryBalance> InventoryBalances { get; set; }
    public DbSet<SalesOrder> SalesOrders { get; set; }
    public DbSet<NumberSequence> NumberSequences { get; set; }
    public DbSet<BusinessAuditLog> BusinessAuditLogs { get; set; }

    #region Entities from the modules

    /* Notice: We only implemented IIdentityProDbContext and ISaasDbContext
     * and replaced them for this DbContext. This allows you to perform JOIN
     * queries for the entities of these modules over the repositories easily. You
     * typically don't need that for other modules. But, if you need, you can
     * implement the DbContext interface of the needed module and use ReplaceDbContext
     * attribute just like IIdentityProDbContext and ISaasDbContext.
     *
     * More info: Replacing a DbContext of a module ensures that the related module
     * uses this DbContext on runtime. Otherwise, it will use its own DbContext class.
     */

    // Identity
    public DbSet<IdentityUser> Users { get; set; }
    public DbSet<IdentityRole> Roles { get; set; }
    public DbSet<IdentityClaimType> ClaimTypes { get; set; }
    public DbSet<OrganizationUnit> OrganizationUnits { get; set; }
    public DbSet<IdentitySecurityLog> SecurityLogs { get; set; }
    public DbSet<IdentityLinkUser> LinkUsers { get; set; }
    public DbSet<IdentityUserDelegation> UserDelegations { get; set; }
    public DbSet<IdentitySession> Sessions { get; set; }
    public DbSet<UserInvitation> UserInvitations { get; set; }

    // SaaS
    public DbSet<Tenant> Tenants { get; set; }
    public DbSet<Edition> Editions { get; set; }
    public DbSet<TenantConnectionString> TenantConnectionStrings { get; set; }

    #endregion

    public VPureLuxDbContext(DbContextOptions<VPureLuxDbContext> options)
        : base(options)
    {

    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        /* Include modules to your migration db context */

        builder.ConfigurePermissionManagement();
        builder.ConfigureSettingManagement();
        builder.ConfigureBackgroundJobs();
        builder.ConfigureAuditLogging();
        builder.ConfigureFeatureManagement();
        builder.ConfigureIdentityPro();
        builder.ConfigureOpenIddictPro();
        builder.ConfigureLanguageManagement();
        builder.ConfigureFileManagement();
        builder.ConfigureSaas();
        builder.ConfigureChat();
        builder.ConfigureTextTemplateManagement();
        builder.ConfigureGdpr();
        builder.ConfigureCmsKit();
        builder.ConfigureCmsKitPro();
        builder.ConfigureBlobStoring();
        
        builder.ApplyConfiguration(new ComponentConfiguration());
        builder.ApplyConfiguration(new ProductConfiguration());
        builder.Ignore<BomItem>();
        builder.ApplyConfiguration(new BomVersionConfiguration());
        builder.ApplyConfiguration(new CustomerGroupConfiguration());
        builder.ApplyConfiguration(new CustomerConfiguration());
        builder.Ignore<Money>();
        builder.Ignore<EffectivePeriod>();
        builder.ApplyConfiguration(new ComponentSuggestedSellingPriceVersionConfiguration());
        builder.ApplyConfiguration(new ProductSuggestedPriceVersionConfiguration());
        builder.Ignore<InventoryTransactionLine>();
        builder.Ignore<InventoryLotAllocation>();
        builder.ApplyConfiguration(new StockItemConfiguration());
        builder.ApplyConfiguration(new WarehouseConfiguration());
        builder.ApplyConfiguration(new InventoryLotConfiguration());
        builder.ApplyConfiguration(new InventoryTransactionConfiguration());
        builder.ApplyConfiguration(new InventoryBalanceConfiguration());
        builder.Ignore<SalesOrderLine>();
        builder.Ignore<SalesOrderBomSnapshotItem>();
        builder.ApplyConfiguration(new SalesOrderConfiguration());
        builder.ApplyConfiguration(new NumberSequenceConfiguration());
        builder.ApplyConfiguration(new BusinessAuditLogConfiguration());

        if (Database.ProviderName?.Contains("Sqlite", StringComparison.OrdinalIgnoreCase) == true)
        {
            builder.Entity<InventoryLot>().Property(x => x.RowVersion)
                .IsConcurrencyToken()
                .ValueGeneratedNever();
            builder.Entity<InventoryBalance>().Property(x => x.RowVersion)
                .IsConcurrencyToken()
                .ValueGeneratedNever();
            builder.Entity<SalesOrder>().Property(x => x.RowVersion)
                .IsConcurrencyToken()
                .ValueGeneratedNever();
        }
    }
}
