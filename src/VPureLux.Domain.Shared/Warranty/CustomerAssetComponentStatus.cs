namespace VPureLux.Warranty;

public enum CustomerAssetComponentStatus : byte
{
    PendingInstallation = 1,
    Active = 2,
    MissingMapping = 3,
    MissingBaseline = 4,
    Inactive = 5
}
