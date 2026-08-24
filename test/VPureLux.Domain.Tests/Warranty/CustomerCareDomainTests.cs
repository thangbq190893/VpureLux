using System;
using Shouldly;
using Volo.Abp;
using Xunit;

namespace VPureLux.Warranty;

public class CustomerCareDomainTests
{
    [Fact]
    public void Sales_intake_gate_should_require_both_flags_and_an_active_go_live_boundary()
    {
        var now = DateTimeOffset.UtcNow;
        var options = new VPureLux.CustomerCare.CustomerCareOptions
        {
            IsEnabled = true,
            IsSalesIntakeEnabled = true
        };

        options.CanRunSalesIntake(now).ShouldBeFalse();
        options.SalesIntakeGoLiveFrom = now.AddMinutes(1);
        options.CanRunSalesIntake(now).ShouldBeFalse();
        options.SalesIntakeGoLiveFrom = now.AddMinutes(-1);
        options.CanRunSalesIntake(now).ShouldBeTrue();
    }

    [Fact]
    public void Legacy_active_asset_without_installation_should_be_pending_review_by_interpretation()
    {
        var asset = new CustomerAsset(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            1,
            "WA-LEGACY-01",
            "SO-LEGACY",
            "CUS-01",
            "Khach hang cu",
            "MACHINE-01",
            "May cu",
            DateTime.Today,
            DateTime.Today);

        asset.Status.ShouldBe(CustomerAssetStatus.Active);
        asset.InstalledAt.ShouldBeNull();
        asset.EffectiveStatus.ShouldBe(CustomerAssetStatus.PendingReview);
    }

    [Fact]
    public void Sold_machine_should_start_pending_and_install_idempotently()
    {
        var asset = CustomerAsset.CreateSoldMachine(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            2,
            1,
            "WA-SOLD-01",
            "SO-01",
            "CUS-01",
            "Khach hang A",
            "RH8",
            "May loc nuoc RH8",
            DateTime.Today);

        asset.Status.ShouldBe(CustomerAssetStatus.PendingInstallation);
        asset.WarrantyStartDate.ShouldBeNull();

        var installedAt = DateTime.Today.AddHours(9);
        asset.ConfirmInstallation(installedAt, "Ha Noi", Guid.NewGuid(), "install-01");
        asset.ConfirmInstallation(installedAt.AddDays(1), "Ignored replay", null, "install-01");

        asset.Status.ShouldBe(CustomerAssetStatus.Active);
        asset.InstalledAt.ShouldBe(installedAt);
        asset.WarrantyStartDate.ShouldBe(installedAt.Date);
        asset.InstallationAddress.ShouldBe("Ha Noi");
        Should.Throw<BusinessException>(() =>
            asset.ConfirmInstallation(installedAt, null, null, "install-conflict"));
    }

    [Fact]
    public void External_position_should_preserve_missing_mapping_and_baseline_states()
    {
        var asset = CustomerAsset.CreateExternal(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "EXT-01",
            "CUS-01",
            "Khach hang A",
            "May ngoai he thong");
        var position = new CustomerAssetComponent(
            Guid.NewGuid(),
            asset.Id,
            "CORE-01",
            "Loi 1",
            null,
            null,
            null,
            null,
            1,
            null,
            pendingInstallation: false);

        asset.Source.ShouldBe(CustomerAssetSource.External);
        asset.SalesOrderId.ShouldBeNull();
        asset.EffectiveStatus.ShouldBe(CustomerAssetStatus.PendingReview);
        position.Status.ShouldBe(CustomerAssetComponentStatus.MissingMapping);

        position.MapComponent(Guid.NewGuid(), "PP-01", "Loi PP", "Cai", null);
        position.Status.ShouldBe(CustomerAssetComponentStatus.MissingBaseline);
        position.SetReplacementBaseline(DateTime.Today);
        position.Status.ShouldBe(CustomerAssetComponentStatus.Active);
    }

    [Fact]
    public void Position_reminder_should_snapshot_warning_date_and_idempotency()
    {
        var dueDate = DateTime.Today.AddMonths(3);
        var reminder = new AssetReplacementReminder(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            null,
            null,
            "PP-01",
            "Loi PP",
            "Cai",
            1,
            dueDate,
            3,
            10,
            ReplacementReminderTriggerSource.Installation,
            "AssetMaintenanceEvent",
            Guid.NewGuid(),
            "reminder-01");

        reminder.WarningDate.ShouldBe(dueDate.AddDays(-10));
        reminder.Status.ShouldBe(AssetReplacementReminderStatus.Pending);
        reminder.IdempotencyKey.ShouldBe("reminder-01");
    }

    [Fact]
    public void Sync_failure_should_count_attempts_and_stop_retry_after_resolution()
    {
        var now = DateTime.UtcNow;
        var failure = new CustomerCareSyncFailure(
            Guid.NewGuid(),
            "sales-line-01",
            "Invalid machine quantity",
            now,
            errorCode: "CUSTOMERCARE_INVALID_QUANTITY");

        failure.RecordAttempt("CUSTOMERCARE_INVALID_QUANTITY", "Still invalid", null, now.AddMinutes(5), now.AddMinutes(15));
        failure.AttemptCount.ShouldBe(2);
        failure.Resolve(now.AddMinutes(10));
        failure.Status.ShouldBe(CustomerCareSyncFailureStatus.Resolved);
        failure.NextRetryAt.ShouldBeNull();
        Should.Throw<BusinessException>(() =>
            failure.RecordAttempt(null, "Retry", null, now.AddMinutes(20), null));
    }
}
