using System;
using System.IO;
using Shouldly;
using Xunit;

namespace VPureLux.Pages;

public class WarrantyPagesTests
{
    [Fact]
    public void Configuration_lists_should_use_abp_modals_and_server_side_datatables()
    {
        var policies = Read("src/VPureLux.Web/Pages/Warranty/Policies.js");
        var machines = Read("src/VPureLux.Web/Pages/Warranty/Machines.js");

        foreach (var source in new[] { policies, machines })
        {
            source.ShouldContain("new abp.ModalManager");
            source.ShouldContain("serverSide: true");
            source.ShouldContain("abp.libs.datatables.createAjax");
            source.ShouldContain("dataTable.ajax.reload(null, false)");
            source.ShouldNotContain("window.prompt");
            source.ShouldNotContain("window.alert");
            source.ShouldNotContain("window.confirm");
        }
    }

    [Fact]
    public void Configuration_modals_should_post_typed_inputs_with_permission_guards()
    {
        var policyModel = Read("src/VPureLux.Web/Pages/Warranty/PolicyModal.cshtml.cs");
        var machineModel = Read("src/VPureLux.Web/Pages/Warranty/MachineSettingModal.cshtml.cs");
        var policyMarkup = Read("src/VPureLux.Web/Pages/Warranty/PolicyModal.cshtml");

        policyModel.ShouldContain("Warranty.ManagePolicies");
        policyModel.ShouldContain("SetComponentReplacementPolicyDto Input");
        machineModel.ShouldContain("Warranty.ManageMachines");
        machineModel.ShouldContain("SetProductMachineSettingDto Input");
        policyMarkup.ShouldContain("Input.WarningDaysBeforeDue");
        policyMarkup.ShouldContain("AbpModalButtons.Cancel|AbpModalButtons.Save");
    }

    [Fact]
    public void Intake_worker_and_failure_page_should_keep_sales_decoupled_and_retry_explicitly()
    {
        var worker = Read("src/VPureLux.Web/Warranty/CustomerCareSalesIntakeWorker.cs");
        var service = Read("src/VPureLux.Application/Warranty/CustomerCareSalesIntakeService.cs");
        var failures = Read("src/VPureLux.Web/Pages/Warranty/SyncFailures.js");
        var sales = Read("src/VPureLux.Application/Sales/SalesOrderAppService.cs");

        worker.ShouldContain("AsyncPeriodicBackgroundWorkerBase");
        service.ShouldContain("IAbpDistributedLock");
        service.ShouldContain("requiresNew: true, isTransactional: true");
        failures.ShouldContain("serverSide: true");
        failures.ShouldContain("RequestVerificationToken");
        failures.ShouldContain("abp.message.confirm");
        sales.ShouldNotContain("CustomerCareSalesIntake");
        sales.ShouldNotContain("WarrantySalesIntegration");
    }

    [Fact]
    public void Installation_workflow_should_use_server_side_list_and_antiforgery_full_page_form()
    {
        var list = Read("src/VPureLux.Web/Pages/Warranty/PendingInstallations.js");
        var pageModel = Read("src/VPureLux.Web/Pages/Warranty/Install.cshtml.cs");
        var markup = Read("src/VPureLux.Web/Pages/Warranty/Install.cshtml");
        var appService = Read("src/VPureLux.Application/Warranty/WarrantyAppService.cs");

        list.ShouldContain("serverSide: true");
        list.ShouldContain("abp.libs.datatables.createAjax");
        pageModel.ShouldContain("Warranty.ManageInstallations");
        pageModel.ShouldContain("IWarrantyAppService");
        pageModel.ShouldNotContain("IRepository<");
        markup.ShouldContain("<form method=\"post\"");
        markup.ShouldContain("Input.IdempotencyKey");
        markup.ShouldContain("Input.Positions[i].ComponentId");
        appService.ShouldContain("GetEnabledByComponentIdsAsync(componentIds)");
        appService.ShouldContain("HashKey(\"installation-reminder\"");
    }

    [Fact]
    public void External_asset_workflow_should_preserve_positions_and_use_application_services()
    {
        var list = Read("src/VPureLux.Web/Pages/Warranty/Assets/Index.js");
        var pageModel = Read("src/VPureLux.Web/Pages/Warranty/Assets/CreateExternal.cshtml.cs");
        var markup = Read("src/VPureLux.Web/Pages/Warranty/Assets/CreateExternal.cshtml");

        list.ShouldContain("serverSide: true");
        list.ShouldContain("abp.libs.datatables.createAjax");
        pageModel.ShouldContain("Warranty.ManageAssets");
        pageModel.ShouldContain("CreateExternalAssetAsync");
        pageModel.ShouldNotContain("IRepository<");
        markup.ShouldContain("<form method=\"post\"");
        markup.ShouldContain("Input.IdempotencyKey");
        markup.ShouldContain("ReplacementBaselineDate");
        markup.ShouldNotContain("window.prompt");
    }

    [Fact]
    public void Reminder_lifecycle_should_use_abp_modal_and_machine_history_should_be_server_paged()
    {
        var reminders = Read("src/VPureLux.Web/Pages/Warranty/Index.js");
        var modal = Read("src/VPureLux.Web/Pages/Warranty/ReminderActionModal.cshtml.cs");
        var history = Read("src/VPureLux.Web/Pages/Warranty/Assets/Details.js");

        reminders.ShouldContain("new abp.ModalManager");
        reminders.ShouldContain("actionModal.open");
        reminders.ShouldNotContain("window.prompt");
        reminders.ShouldNotContain("window.alert");
        reminders.ShouldNotContain("window.confirm");
        modal.ShouldContain("Warranty.ManageReminders");
        modal.ShouldContain("IdempotencyKey");
        modal.ShouldContain("SuspendAssetAsync");
        history.ShouldContain("serverSide: true");
        history.ShouldContain("abp.libs.datatables.createAjax");
    }

    private static string Read(string relativePath)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory != null)
        {
            var path = Path.Combine(directory.FullName, relativePath);
            if (File.Exists(path))
            {
                return File.ReadAllText(path);
            }

            directory = directory.Parent;
        }

        throw new FileNotFoundException(relativePath);
    }
}
