namespace VPureLux.Warranty;

public enum CustomerAssetStatus : byte
{
    Active = 1,
    Inactive = 2,
    Cancelled = 3,
    PendingInstallation = 4,
    Transferred = 5,
    PendingReview = 6
}
