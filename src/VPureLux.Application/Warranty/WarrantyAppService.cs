using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using VPureLux.Permissions;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;

namespace VPureLux.Warranty;

[Authorize(VPureLuxPermissions.Warranty.View)]
public class WarrantyAppService : ApplicationService, IWarrantyAppService
{
    private readonly IComponentReplacementPolicyRepository _policies;
    private readonly IRepository<AssetReplacementReminder, Guid> _reminders;
    private readonly IWarrantyReadRepository _readRepository;

    public WarrantyAppService(
        IComponentReplacementPolicyRepository policies,
        IRepository<AssetReplacementReminder, Guid> reminders,
        IWarrantyReadRepository readRepository)
    {
        _policies = policies;
        _reminders = reminders;
        _readRepository = readRepository;
    }

    public async Task<PagedResultDto<WarrantyPolicyListDto>> GetPolicyListAsync(GetWarrantyPolicyListInput input)
    {
        var filter = new WarrantyPolicyFilter
        {
            SearchText = input.SearchText,
            IsEnabled = input.IsEnabled,
            Sorting = input.Sorting,
            SkipCount = input.SkipCount,
            MaxResultCount = input.MaxResultCount
        };

        var totalCount = await _readRepository.GetPolicyCountAsync(filter);
        var items = await _readRepository.GetPolicyListAsync(filter);
        return new PagedResultDto<WarrantyPolicyListDto>(
            totalCount,
            items.Select(ToDto).ToList());
    }

    public async Task<ComponentReplacementPolicyDto?> GetPolicyByComponentIdAsync(Guid componentId)
    {
        var policy = await _policies.FindByComponentIdAsync(componentId);
        return policy == null ? null : ToDto(policy);
    }

    [Authorize(VPureLuxPermissions.Warranty.ManagePolicies)]
    public async Task<ComponentReplacementPolicyDto> SetPolicyAsync(Guid componentId, SetComponentReplacementPolicyDto input)
    {
        if (componentId == Guid.Empty)
        {
            throw new BusinessException(VPureLuxDomainErrorCodes.ValidationFailed);
        }

        var policy = await _policies.FindByComponentIdAsync(componentId);
        if (policy == null)
        {
            policy = new ComponentReplacementPolicy(
                GuidGenerator.Create(),
                componentId,
                input.CycleMonths,
                input.WarningDaysBeforeDue,
                input.Note,
                input.IsEnabled);
            await _policies.InsertAsync(policy, autoSave: true);
        }
        else
        {
            policy.Update(input.CycleMonths, input.WarningDaysBeforeDue, input.Note, input.IsEnabled);
            await _policies.UpdateAsync(policy, autoSave: true);
        }

        return ToDto(policy);
    }

    public async Task<PagedResultDto<WarrantyReminderListDto>> GetReminderListAsync(GetWarrantyReminderListInput input)
    {
        var filter = new WarrantyReminderFilter
        {
            SearchText = input.SearchText,
            Status = input.Status,
            DueFrom = input.DueFrom,
            DueTo = input.DueTo,
            Sorting = input.Sorting,
            SkipCount = input.SkipCount,
            MaxResultCount = input.MaxResultCount
        };

        var totalCount = await _readRepository.GetReminderCountAsync(filter);
        var items = await _readRepository.GetReminderListAsync(filter);
        return new PagedResultDto<WarrantyReminderListDto>(
            totalCount,
            items.Select(ToDto).ToList());
    }

    [Authorize(VPureLuxPermissions.Warranty.ManageReminders)]
    public async Task CompleteReminderAsync(Guid id, CompleteReplacementReminderDto input)
    {
        var reminder = await GetReminderEntityAsync(id);
        var completedAt = (input.CompletedAt ?? Clock.Now).Date;
        var nextReminder = new AssetReplacementReminder(
            GuidGenerator.Create(),
            reminder.CustomerAssetId,
            reminder.ComponentId,
            reminder.SalesOrderId,
            reminder.SalesOrderLineId,
            reminder.ComponentCodeSnapshot,
            reminder.ComponentNameSnapshot,
            reminder.ComponentUnitSnapshot,
            reminder.QuantityPerProductSnapshot,
            completedAt.AddMonths(reminder.CycleMonthsSnapshot),
            reminder.CycleMonthsSnapshot,
            reminder.WarningDaysBeforeDueSnapshot);

        await _reminders.InsertAsync(nextReminder);
        reminder.Complete(completedAt, CurrentUser.Id, nextReminder.Id, input.Note);
        await _reminders.UpdateAsync(reminder, autoSave: true);
    }

    [Authorize(VPureLuxPermissions.Warranty.ManageReminders)]
    public async Task SkipReminderAsync(Guid id, SkipReplacementReminderDto input)
    {
        var reminder = await GetReminderEntityAsync(id);
        reminder.Skip(input.Note);
        await _reminders.UpdateAsync(reminder, autoSave: true);
    }

    [Authorize(VPureLuxPermissions.Warranty.ManageReminders)]
    public async Task RescheduleReminderAsync(Guid id, RescheduleReplacementReminderDto input)
    {
        var reminder = await GetReminderEntityAsync(id);
        reminder.Reschedule(input.DueDate, input.Note);
        await _reminders.UpdateAsync(reminder, autoSave: true);
    }

    private async Task<AssetReplacementReminder> GetReminderEntityAsync(Guid id)
    {
        var reminder = await _reminders.FindAsync(id);
        if (reminder == null)
        {
            throw new BusinessException(VPureLuxDomainErrorCodes.EntityNotFound);
        }

        return reminder;
    }

    private static ComponentReplacementPolicyDto ToDto(ComponentReplacementPolicy policy) =>
        new()
        {
            Id = policy.Id,
            ComponentId = policy.ComponentId,
            IsEnabled = policy.IsEnabled,
            CycleMonths = policy.CycleMonths,
            WarningDaysBeforeDue = policy.WarningDaysBeforeDue,
            Note = policy.Note
        };

    private static WarrantyPolicyListDto ToDto(WarrantyPolicyListItem item) =>
        new()
        {
            ComponentId = item.ComponentId,
            ComponentCode = item.ComponentCode,
            ComponentName = item.ComponentName,
            ComponentUnit = item.ComponentUnit,
            PolicyId = item.PolicyId,
            IsEnabled = item.IsEnabled,
            CycleMonths = item.CycleMonths,
            WarningDaysBeforeDue = item.WarningDaysBeforeDue,
            Note = item.Note
        };

    private static WarrantyReminderListDto ToDto(WarrantyReminderListItem item) =>
        new()
        {
            Id = item.Id,
            AssetNo = item.AssetNo,
            CustomerCode = item.CustomerCode,
            CustomerName = item.CustomerName,
            ProductCode = item.ProductCode,
            ProductName = item.ProductName,
            ComponentCode = item.ComponentCode,
            ComponentName = item.ComponentName,
            ComponentUnit = item.ComponentUnit,
            QuantityPerProduct = item.QuantityPerProduct,
            DueDate = item.DueDate,
            CycleMonths = item.CycleMonths,
            WarningDaysBeforeDue = item.WarningDaysBeforeDue,
            Status = item.Status,
            OrderNo = item.OrderNo,
            LineNo = item.LineNo,
            Note = item.Note
        };
}
