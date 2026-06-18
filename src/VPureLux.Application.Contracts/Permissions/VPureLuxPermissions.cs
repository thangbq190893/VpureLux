namespace VPureLux.Permissions;

public static class VPureLuxPermissions
{
    public const string GroupName = "VPureLux";

    public static class Dashboard
    {
        public const string DashboardGroup = GroupName + ".Dashboard";
        public const string Host = DashboardGroup + ".Host";
        public const string Tenant = DashboardGroup + ".Tenant";
    }

    public static class Catalog
    {
        public const string Group = GroupName + ".Catalog";

        public static class Components
        {
            public const string Default = Group + ".Components";
            public const string View = Default + ".View";
            public const string Create = Default + ".Create";
            public const string Edit = Default + ".Edit";
        }

        public static class Products
        {
            public const string Default = Group + ".Products";
            public const string View = Default + ".View";
            public const string Create = Default + ".Create";
            public const string Edit = Default + ".Edit";
        }
    }

    public static class Bom
    {
        public const string Default = GroupName + ".Bom";
        public const string View = Default + ".View";
        public const string Create = Default + ".Create";
        public const string Publish = Default + ".Publish";
        public const string Archive = Default + ".Archive";
    }

    public static class Customers
    {
        public const string Default = GroupName + ".Customers";
        public const string View = Default + ".View";
        public const string Create = Default + ".Create";
        public const string Edit = Default + ".Edit";
        public const string ManageStatus = Default + ".ManageStatus";
    }

    public static class CustomerGroups
    {
        public const string Default = GroupName + ".CustomerGroups";
        public const string View = Default + ".View";
        public const string Create = Default + ".Create";
        public const string Edit = Default + ".Edit";
        public const string ManageStatus = Default + ".ManageStatus";
    }

    public static class Pricing
    {
        public const string Default = GroupName + ".Pricing";
        public const string View = Default + ".View";
        public const string History = Default + ".History";

        public static class ComponentSuggestedSellingPrices
        {
            public const string Default = Pricing.Default + ".ComponentSuggestedSellingPrices";
            public const string Create = Default + ".Create";
            public const string History = Default + ".History";
        }

        public static class ProductSuggestedPrices
        {
            public const string Default = Pricing.Default + ".ProductSuggestedPrices";
            public const string Create = Default + ".Create";
        }
    }

    public static class Inventory
    {
        public const string Default = GroupName + ".Inventory";
        public const string View = Default + ".View";
        public const string Receive = Default + ".Receive";
        public const string Issue = Default + ".Issue";
        public const string Adjust = Default + ".Adjust";
        public const string ManageWarehouses = Default + ".ManageWarehouses";
        public const string ViewLedger = Default + ".ViewLedger";
    }

    public static class Sales
    {
        public const string Default = GroupName + ".Sales";
        public const string View = Default + ".View";
        public const string Create = Default + ".Create";
        public const string Edit = Default + ".Edit";
        public const string OverridePrice = Default + ".OverridePrice";
        public const string Confirm = Default + ".Confirm";
        public const string Cancel = Default + ".Cancel";
        public const string ViewCost = Default + ".ViewCost";
        public const string ViewProfit = Default + ".ViewProfit";
        public const string ViewCustomerHistory = Default + ".ViewCustomerHistory";
    }

    public static class Audit
    {
        public const string Default = GroupName + ".Audit";
        public const string View = Default + ".View";
        public const string Export = Default + ".Export";
    }
}
