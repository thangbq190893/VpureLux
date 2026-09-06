namespace VPureLux.Service;

public static class ServiceErrorCodes
{
    public const string WorkCodeAlreadyExists = "VPureLux:SERVICE_001";
    public const string Disabled = "VPureLux:SERVICE_009";
    public const string InvalidWork = "VPureLux:SERVICE_010";
    public const string OrderNotFound = "VPureLux:SERVICE_011";
    public const string OrderCannotBeModified = "VPureLux:SERVICE_012";
    public const string InvalidLine = "VPureLux:SERVICE_013";
    public const string AssetUnavailable = "VPureLux:SERVICE_014";
    public const string AssetCustomerMismatch = "VPureLux:SERVICE_015";
    public const string MaterialUnavailable = "VPureLux:SERVICE_016";
    public const string WorkUnavailable = "VPureLux:SERVICE_017";
    public const string MaterialPositionMismatch = "VPureLux:SERVICE_018";
    public const string ConcurrentModification = "VPureLux:SERVICE_019";
    public const string CancellationReasonRequired = "VPureLux:SERVICE_020";
    public const string DuplicateOrderNo = "VPureLux:SERVICE_021";
}
