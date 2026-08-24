using System;

namespace VPureLux.CustomerCare;

public class CustomerCareOptions
{
    public const string SectionName = "CustomerCare";

    public bool IsEnabled { get; set; }

    public bool IsSalesIntakeEnabled { get; set; }

    public DateTimeOffset? SalesIntakeGoLiveFrom { get; set; }

    public int SalesIntakeBatchSize { get; set; } = 100;

    public int SalesIntakeWorkerPeriodMilliseconds { get; set; } = 60_000;

    public bool CanRunSalesIntake(DateTimeOffset now) =>
        IsEnabled &&
        IsSalesIntakeEnabled &&
        SalesIntakeGoLiveFrom.HasValue &&
        now >= SalesIntakeGoLiveFrom.Value;
}
