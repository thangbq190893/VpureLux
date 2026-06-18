namespace VPureLux;

public static class VPureLuxDomainErrorCodes
{
    public const string ComponentCodeAlreadyExists = "CATALOG_001";
    public const string ProductCodeAlreadyExists = "CATALOG_002";
    public const string ComponentNotFound = "CATALOG_003";
    public const string ProductNotFound = "CATALOG_004";
    public const string CatalogImageUnsupportedFormat = "CATALOG_005";
    public const string CatalogImageInvalidBase64 = "CATALOG_006";
    public const string CatalogImageTooLarge = "CATALOG_007";
    public const string CatalogImageInvalidSignature = "CATALOG_008";
    public const string CatalogImageUnsafeContent = "CATALOG_009";
    public const string CatalogImageNotFound = "CATALOG_010";

    public const string PublishedBomCannotBeModified = "BOM_001";
    public const string ArchivedBomCannotBeModified = "BOM_002";
    public const string OnlyOneActiveBomAllowed = "BOM_003";
    public const string ComponentNotActive = "BOM_004";

    public const string CustomerCodeAlreadyExists = "CUSTOMER_001";
    public const string CustomerNotFound = "CUSTOMER_002";
    public const string CustomerInactive = "CUSTOMER_003";
    public const string CustomerGroupNotFound = "CUSTOMER_004";
    public const string CustomerGroupInactive = "CUSTOMER_005";
    public const string CustomerGroupCodeAlreadyExists = "CUSTOMER_006";
    public const string CustomerGroupIsInUse = "CUSTOMER_007";

    public const string ActivePriceVersionAlreadyExists = "PRICE_001";
    public const string PriceMustBeGreaterThanZero = "PRICE_002";
    public const string PriceVersionNotFound = "PRICE_003";
    public const string BackdatedPriceVersionNotAllowed = "PRICE_004";
    public const string InvalidPriceEffectivePeriod = "PRICE_005";
    public const string PriceVersionAlreadyClosed = "PRICE_006";

    public const string InsufficientInventory = "INV_001";
    public const string InventoryTransactionAlreadyPosted = "INV_002";
    public const string WarehouseNotFound = "INV_003";
    public const string AdjustmentReasonRequired = "INV_004";
    public const string WarehouseInactive = "INV_005";
    public const string StockItemNotFound = "INV_006";
    public const string StockItemInactive = "INV_007";
    public const string StockItemInventoryDisabled = "INV_008";
    public const string InventoryLotNotFound = "INV_009";
    public const string InventoryTransactionNotFound = "INV_010";
    public const string InventoryIdempotencyConflict = "INV_011";
    public const string WarehouseCodeAlreadyExists = "INV_012";

    public const string DuplicateOrderNo = "SALES_001";
    public const string DuplicateConfirmationKey = "SALES_002";
    public const string SalesConcurrentModification = "SALES_003";
    public const string SalesOrderAlreadyConfirmed = "SALES_004";
    public const string SalesOrderAlreadyCancelled = "SALES_005";
    public const string SalesInventoryValidationFailed = "SALES_006";
    public const string SalesOrderCannotBeModified = "SALES_007";
    public const string SalesOrderNotFound = "SALES_008";
    public const string SalesOverrideReasonRequired = "SALES_009";
    public const string SalesBomMustBePublished = "SALES_010";
    public const string SalesConfirmationIdempotencyConflict = DuplicateConfirmationKey;

    public const string AuditPayloadTooLarge = "AUDIT_001";

    public const string ValidationFailed = "COM_001";
    public const string EntityNotFound = "COM_002";
    public const string AccessDenied = "COM_003";
}
