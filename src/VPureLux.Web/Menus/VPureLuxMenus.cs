namespace VPureLux.Web.Menus;

public class VPureLuxMenus
{
    private const string Prefix = "VPureLux";

    public const string Home = Prefix + ".Home";

    public const string HostDashboard = Prefix + ".HostDashboard";
    
    public const string TenantDashboard = Prefix + ".TenantDashboard";

    public const string Catalog = Prefix + ".Catalog";
    public const string CatalogComponents = Catalog + ".Components";
    public const string CatalogProducts = Catalog + ".Products";

    public const string Bom = Prefix + ".Bom";

    public const string Customers = Prefix + ".Customers";
    public const string CustomerGroups = Prefix + ".CustomerGroups";
    public const string Pricing = Prefix + ".Pricing";
    public const string Inventory = Prefix + ".Inventory";
    public const string InventoryLedger = Inventory + ".Ledger";
    public const string InventoryReceipt = Inventory + ".Receipt";
    public const string InventoryIssue = Inventory + ".Issue";
    public const string InventoryAdjustment = Inventory + ".Adjustment";
    public const string InventoryBalances = Inventory + ".Balances";
    public const string InventoryLots = Inventory + ".Lots";
    public const string Sales = Prefix + ".Sales";
    public const string Audit = Prefix + ".Audit";
}
