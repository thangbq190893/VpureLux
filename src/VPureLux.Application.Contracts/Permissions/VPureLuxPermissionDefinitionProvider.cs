using VPureLux.Localization;
using Volo.Abp.Authorization.Permissions;
using Volo.Abp.Localization;
using Volo.Abp.MultiTenancy;

namespace VPureLux.Permissions;

public class VPureLuxPermissionDefinitionProvider : PermissionDefinitionProvider
{
    public override void Define(IPermissionDefinitionContext context)
    {
        var myGroup = context.AddGroup(VPureLuxPermissions.GroupName);

        myGroup.AddPermission(VPureLuxPermissions.Dashboard.Host, L("Permission:Dashboard"), MultiTenancySides.Host);
        myGroup.AddPermission(VPureLuxPermissions.Dashboard.Tenant, L("Permission:Dashboard"), MultiTenancySides.Tenant);

        var catalog = myGroup.AddPermission(VPureLuxPermissions.Catalog.Group, L("Permission:Catalog"));

        var components = catalog.AddChild(VPureLuxPermissions.Catalog.Components.Default, L("Permission:Catalog.Components"));
        components.AddChild(VPureLuxPermissions.Catalog.Components.View, L("Permission:Catalog.Components.View"));
        components.AddChild(VPureLuxPermissions.Catalog.Components.Create, L("Permission:Catalog.Components.Create"));
        components.AddChild(VPureLuxPermissions.Catalog.Components.Edit, L("Permission:Catalog.Components.Edit"));

        var products = catalog.AddChild(VPureLuxPermissions.Catalog.Products.Default, L("Permission:Catalog.Products"));
        products.AddChild(VPureLuxPermissions.Catalog.Products.View, L("Permission:Catalog.Products.View"));
        products.AddChild(VPureLuxPermissions.Catalog.Products.Create, L("Permission:Catalog.Products.Create"));
        products.AddChild(VPureLuxPermissions.Catalog.Products.Edit, L("Permission:Catalog.Products.Edit"));

        var bom = myGroup.AddPermission(VPureLuxPermissions.Bom.Default, L("Permission:Bom"));
        bom.AddChild(VPureLuxPermissions.Bom.View, L("Permission:Bom.View"));
        bom.AddChild(VPureLuxPermissions.Bom.Create, L("Permission:Bom.Create"));
        bom.AddChild(VPureLuxPermissions.Bom.Publish, L("Permission:Bom.Publish"));
        bom.AddChild(VPureLuxPermissions.Bom.Archive, L("Permission:Bom.Archive"));

        var customers = myGroup.AddPermission(VPureLuxPermissions.Customers.Default, L("Permission:Customers"));
        customers.AddChild(VPureLuxPermissions.Customers.View, L("Permission:Customers.View"));
        customers.AddChild(VPureLuxPermissions.Customers.Create, L("Permission:Customers.Create"));
        customers.AddChild(VPureLuxPermissions.Customers.Edit, L("Permission:Customers.Edit"));
        customers.AddChild(VPureLuxPermissions.Customers.ManageStatus, L("Permission:Customers.ManageStatus"));

        var customerGroups = myGroup.AddPermission(
            VPureLuxPermissions.CustomerGroups.Default,
            L("Permission:CustomerGroups"));
        customerGroups.AddChild(VPureLuxPermissions.CustomerGroups.View, L("Permission:CustomerGroups.View"));
        customerGroups.AddChild(VPureLuxPermissions.CustomerGroups.Create, L("Permission:CustomerGroups.Create"));
        customerGroups.AddChild(VPureLuxPermissions.CustomerGroups.Edit, L("Permission:CustomerGroups.Edit"));
        customerGroups.AddChild(
            VPureLuxPermissions.CustomerGroups.ManageStatus,
            L("Permission:CustomerGroups.ManageStatus"));

        var pricing = myGroup.AddPermission(VPureLuxPermissions.Pricing.Default, L("Permission:Pricing"));
        pricing.AddChild(VPureLuxPermissions.Pricing.View, L("Permission:Pricing.View"));
        pricing.AddChild(VPureLuxPermissions.Pricing.History, L("Permission:Pricing.History"));

        var componentSuggestedSellingPrices = pricing.AddChild(
            VPureLuxPermissions.Pricing.ComponentSuggestedSellingPrices.Default,
            L("Permission:Pricing.ComponentSuggestedSellingPrices"));
        componentSuggestedSellingPrices.AddChild(
            VPureLuxPermissions.Pricing.ComponentSuggestedSellingPrices.Create,
            L("Permission:Pricing.ComponentSuggestedSellingPrices.Create"));
        componentSuggestedSellingPrices.AddChild(
            VPureLuxPermissions.Pricing.ComponentSuggestedSellingPrices.History,
            L("Permission:Pricing.ComponentSuggestedSellingPrices.History"));

        var productSuggestedPrices = pricing.AddChild(
            VPureLuxPermissions.Pricing.ProductSuggestedPrices.Default,
            L("Permission:Pricing.ProductSuggestedPrices"));
        productSuggestedPrices.AddChild(
            VPureLuxPermissions.Pricing.ProductSuggestedPrices.Create,
            L("Permission:Pricing.ProductSuggestedPrices.Create"));

        var inventory = myGroup.AddPermission(VPureLuxPermissions.Inventory.Default, L("Permission:Inventory"));
        inventory.AddChild(VPureLuxPermissions.Inventory.View, L("Permission:Inventory.View"));
        inventory.AddChild(VPureLuxPermissions.Inventory.Receive, L("Permission:Inventory.Receive"));
        inventory.AddChild(VPureLuxPermissions.Inventory.Issue, L("Permission:Inventory.Issue"));
        inventory.AddChild(VPureLuxPermissions.Inventory.Adjust, L("Permission:Inventory.Adjust"));
        inventory.AddChild(VPureLuxPermissions.Inventory.ManageWarehouses, L("Permission:Inventory.ManageWarehouses"));
        inventory.AddChild(VPureLuxPermissions.Inventory.ViewLedger, L("Permission:Inventory.ViewLedger"));

        var sales = myGroup.AddPermission(VPureLuxPermissions.Sales.Default, L("Permission:Sales"));
        sales.AddChild(VPureLuxPermissions.Sales.View, L("Permission:Sales.View"));
        sales.AddChild(VPureLuxPermissions.Sales.Create, L("Permission:Sales.Create"));
        sales.AddChild(VPureLuxPermissions.Sales.Edit, L("Permission:Sales.Edit"));
        sales.AddChild(VPureLuxPermissions.Sales.OverridePrice, L("Permission:Sales.OverridePrice"));
        sales.AddChild(VPureLuxPermissions.Sales.Confirm, L("Permission:Sales.Confirm"));
        sales.AddChild(VPureLuxPermissions.Sales.Cancel, L("Permission:Sales.Cancel"));
        sales.AddChild(VPureLuxPermissions.Sales.ViewCost, L("Permission:Sales.ViewCost"));
        sales.AddChild(VPureLuxPermissions.Sales.ViewProfit, L("Permission:Sales.ViewProfit"));
        sales.AddChild(VPureLuxPermissions.Sales.ViewCustomerHistory, L("Permission:Sales.ViewCustomerHistory"));

        var audit = myGroup.AddPermission(VPureLuxPermissions.Audit.Default, L("Permission:Audit"));
        audit.AddChild(VPureLuxPermissions.Audit.View, L("Permission:Audit.View"));
        audit.AddChild(VPureLuxPermissions.Audit.Export, L("Permission:Audit.Export"));
    }

    private static LocalizableString L(string name)
    {
        return LocalizableString.Create<VPureLuxResource>(name);
    }
}
