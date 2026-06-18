using System.Threading.Tasks;
using VPureLux.Localization;
using VPureLux.Permissions;
using VPureLux.MultiTenancy;
using Volo.Abp.SettingManagement.Web.Navigation;
using Volo.Abp.Authorization.Permissions;
using Volo.Abp.Identity.Web.Navigation;
using Volo.Abp.UI.Navigation;
using Volo.Abp.AuditLogging.Web.Navigation;
using Volo.Abp.LanguageManagement.Navigation;
using Volo.FileManagement.Web.Navigation;
using Volo.Abp.TextTemplateManagement.Web.Navigation;
using Volo.Abp.OpenIddict.Pro.Web.Menus;
using Volo.CmsKit.Pro.Admin.Web.Menus;
using Volo.Saas.Host.Navigation;

namespace VPureLux.Web.Menus;

public class VPureLuxMenuContributor : IMenuContributor
{
    public async Task ConfigureMenuAsync(MenuConfigurationContext context)
    {
        if (context.Menu.Name == StandardMenus.Main)
        {
            await ConfigureMainMenuAsync(context);
        }
    }

    private static Task ConfigureMainMenuAsync(MenuConfigurationContext context)
    {
        var l = context.GetLocalizer<VPureLuxResource>();

        //Home
        context.Menu.AddItem(
            new ApplicationMenuItem(
                VPureLuxMenus.Home,
                l["Menu:Home"],
                "~/",
                icon: "fa fa-home",
                order: 1
            )
        );

        //HostDashboard
        context.Menu.AddItem(
            new ApplicationMenuItem(
                VPureLuxMenus.HostDashboard,
                l["Menu:Dashboard"],
                "~/HostDashboard",
                icon: "fa fa-line-chart",
                order: 2
            ).RequirePermissions(VPureLuxPermissions.Dashboard.Host)
        );

        //TenantDashboard
        context.Menu.AddItem(
            new ApplicationMenuItem(
                VPureLuxMenus.TenantDashboard,
                l["Menu:Dashboard"],
                "~/Dashboard",
                icon: "fa fa-line-chart",
                order: 2
            ).RequirePermissions(VPureLuxPermissions.Dashboard.Tenant)
        );

        var catalog = new ApplicationMenuItem(
            VPureLuxMenus.Catalog,
            l["Menu:Catalog"],
            icon: "fa fa-cubes",
            order: 3
        ).RequirePermissions(VPureLuxPermissions.Catalog.Group);

        catalog.AddItem(new ApplicationMenuItem(
            VPureLuxMenus.CatalogComponents,
            l["Menu:Catalog.Components"],
            "~/Catalog/Components",
            icon: "fa fa-cog"
        ).RequirePermissions(VPureLuxPermissions.Catalog.Components.View));

        catalog.AddItem(new ApplicationMenuItem(
            VPureLuxMenus.CatalogProducts,
            l["Menu:Catalog.Products"],
            "~/Catalog/Products",
            icon: "fa fa-filter"
        ).RequirePermissions(VPureLuxPermissions.Catalog.Products.View));

        context.Menu.AddItem(catalog);

        context.Menu.AddItem(new ApplicationMenuItem(
            VPureLuxMenus.Bom,
            l["Menu:Bom"],
            "~/Bom",
            icon: "fa fa-sitemap",
            order: 4
        ).RequirePermissions(VPureLuxPermissions.Bom.View));

        context.Menu.AddItem(new ApplicationMenuItem(
            VPureLuxMenus.Customers,
            l["Menu:Customers"],
            "~/Customers",
            icon: "fa fa-users",
            order: 5
        ).RequirePermissions(VPureLuxPermissions.Customers.View));

        context.Menu.AddItem(new ApplicationMenuItem(
            VPureLuxMenus.CustomerGroups,
            l["Menu:CustomerGroups"],
            "~/CustomerGroups",
            icon: "fa fa-tags",
            order: 6
        ).RequirePermissions(VPureLuxPermissions.CustomerGroups.View));

        context.Menu.AddItem(new ApplicationMenuItem(
            VPureLuxMenus.Pricing,
            l["Menu:Pricing"],
            "~/Pricing",
            icon: "fa fa-money",
            order: 7
        ).RequirePermissions(VPureLuxPermissions.Pricing.View));

        context.Menu.AddItem(new ApplicationMenuItem(
            VPureLuxMenus.Inventory,
            l["Menu:Inventory"],
            "~/Inventory",
            icon: "fa fa-warehouse",
            order: 8
        ).RequirePermissions(VPureLuxPermissions.Inventory.View));

        context.Menu.AddItem(new ApplicationMenuItem(
            VPureLuxMenus.Sales,
            l["Menu:Sales"],
            "~/Sales",
            icon: "fa fa-shopping-cart",
            order: 9
        ).RequirePermissions(VPureLuxPermissions.Sales.View));

        context.Menu.AddItem(new ApplicationMenuItem(
            VPureLuxMenus.Audit,
            l["Menu:Audit"],
            "~/Audit",
            icon: "fa fa-history",
            order: 10
        ).RequirePermissions(VPureLuxPermissions.Audit.View));

        //Saas
        context.Menu.SetSubItemOrder(SaasHostMenuNames.GroupName, 5);
    
        //CMS
        context.Menu.SetSubItemOrder(CmsKitProAdminMenus.GroupName, 5);
    
        //File management
        context.Menu.SetSubItemOrder(FileManagementMenuNames.GroupName, 6);

        //Administration
        var administration = context.Menu.GetAdministration();
        administration.Order = 10;

        //Administration->Identity
        administration.SetSubItemOrder(IdentityMenuNames.GroupName, 2);

        //Administration->OpenIddict
        administration.SetSubItemOrder(OpenIddictProMenus.GroupName, 3);

        //Administration->Language Management
        administration.SetSubItemOrder(LanguageManagementMenuNames.GroupName, 4);

        //Administration->Text Template Management
        administration.SetSubItemOrder(TextTemplateManagementMainMenuNames.GroupName, 6);

        //Administration->Audit Logs
        administration.SetSubItemOrder(AbpAuditLoggingMainMenuNames.GroupName, 7);

        //Administration->Settings
        administration.SetSubItemOrder(SettingManagementMenuNames.GroupName, 8);
        
        return Task.CompletedTask;
    }
}
