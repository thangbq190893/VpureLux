using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VPureLux.Catalog;
using Volo.Abp;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Guids;

namespace VPureLux.Warranty;

public sealed record PerformedReplacement(Guid ServiceOrderLineId, Guid PositionId, Guid ComponentId,
    string Code, string Name, string Unit, int Quantity);
public sealed record CustomerCareCompletion(Guid ServiceOrderId, Guid AssetId, DateTime CompletedAt,
    Guid? OperatorId, IReadOnlyList<PerformedReplacement> Replacements);

public interface ICustomerCareServiceCompletion
{
    Task ApplyAsync(CustomerCareCompletion facts);
}

// Internal module boundary, not a remote API. Caller owns the atomic Service UoW.
public class CustomerCareServiceCompletion(
    IRepository<CustomerAsset, Guid> assets,
    IRepository<CustomerAssetComponent, Guid> positions,
    IRepository<AssetMaintenanceEvent, Guid> events,
    IRepository<AssetReplacementReminder, Guid> reminders,
    IComponentReplacementPolicyRepository policies,
    IComponentRepository components,
    CustomerAssetOperationCoordinator coordinator,
    IGuidGenerator guidGenerator) : ICustomerCareServiceCompletion, ITransientDependency
{
    public virtual async Task ApplyAsync(CustomerCareCompletion facts)
    {
        await coordinator.HoldAsync(facts.AssetId);
        var asset = await assets.GetAsync(facts.AssetId);
        if (asset.Status is not (CustomerAssetStatus.Active or CustomerAssetStatus.PendingReview))
            throw new BusinessException(VPureLuxDomainErrorCodes.ValidationFailed).WithData("CustomerAssetId", facts.AssetId);
        var ids = facts.Replacements.Select(x => x.PositionId).Distinct().ToArray();
        var mapped = await positions.GetListAsync(x => x.CustomerAssetId == facts.AssetId && ids.Contains(x.Id));
        var byId = mapped.ToDictionary(x => x.Id);
        if (mapped.Count != ids.Length || facts.Replacements.Any(x => x.Quantity <= 0) ||
            facts.Replacements.Select(x => x.PositionId).Distinct().Count() != facts.Replacements.Count)
            throw new BusinessException(VPureLuxDomainErrorCodes.ValidationFailed).WithData("Reason", "InvalidReplacementPositions");
        var componentIds = facts.Replacements.Select(x => x.ComponentId).Distinct().ToArray();
        var currentPolicies = (await policies.GetListAsync(x => componentIds.Contains(x.ComponentId))).ToDictionary(x => x.ComponentId);
        var currentComponents = (await components.GetListAsync(x => componentIds.Contains(x.Id))).ToDictionary(x => x.Id);
        var pending = await reminders.GetListAsync(x => x.CustomerAssetId == facts.AssetId &&
            x.CustomerAssetComponentId.HasValue && ids.Contains(x.CustomerAssetComponentId.Value) &&
            x.Status == AssetReplacementReminderStatus.Pending);
        var appended = new List<AssetMaintenanceEvent>
        {
            new(guidGenerator.Create(), facts.AssetId, null, AssetMaintenanceEventType.ServiceCompleted,
                AssetMaintenanceSourceType.ServiceOrder, facts.CompletedAt, $"service:{facts.ServiceOrderId:N}",
                sourceId: facts.ServiceOrderId)
        };
        var successors = new List<AssetReplacementReminder>();
        var changedPositions = new List<CustomerAssetComponent>();
        var closed = new List<AssetReplacementReminder>();
        foreach (var replacement in facts.Replacements)
        {
            var position = byId[replacement.PositionId];
            // Never infer mapping or activation from an old Service planning line.
            var eligiblePosition = position.ComponentId == replacement.ComponentId &&
                position.Status is CustomerAssetComponentStatus.Active or CustomerAssetComponentStatus.MissingBaseline;
            if (eligiblePosition && position.ReplacementBaselineDate > facts.CompletedAt.Date)
                throw new BusinessException(VPureLuxDomainErrorCodes.ValidationFailed)
                    .WithData("Reason", "ReplacementPredatesCurrentBaseline").WithData("PositionId", position.Id);
            var maintenance = new AssetMaintenanceEvent(guidGenerator.Create(), facts.AssetId, position.Id,
                AssetMaintenanceEventType.Replacement, AssetMaintenanceSourceType.ServiceOrder, facts.CompletedAt,
                $"service-line:{replacement.ServiceOrderLineId:N}", facts.ServiceOrderId, replacement.ComponentId,
                replacement.Code, replacement.Name, serviceOrderLineId: replacement.ServiceOrderLineId);
            appended.Add(maintenance);
            AssetReplacementReminder? successor = null;
            if (eligiblePosition)
            {
                position.SetReplacementBaseline(facts.CompletedAt);
                changedPositions.Add(position);
                if (currentPolicies.TryGetValue(replacement.ComponentId, out var policy) && policy.IsEnabled &&
                    currentComponents.TryGetValue(replacement.ComponentId, out var component) && component.Status == CatalogItemStatus.Active)
                {
                    successor = new AssetReplacementReminder(guidGenerator.Create(), asset.Id, position.Id,
                        replacement.ComponentId, asset.SalesOrderId, asset.SalesOrderLineId, replacement.Code, replacement.Name,
                        replacement.Unit, replacement.Quantity, facts.CompletedAt.Date.AddMonths(policy.CycleMonths),
                        policy.CycleMonths, policy.WarningDaysBeforeDue, ReplacementReminderTriggerSource.Replacement,
                        nameof(AssetMaintenanceEvent), maintenance.Id, $"service-cycle:{replacement.ServiceOrderLineId:N}");
                    successors.Add(successor);
                }
            }
            foreach (var old in pending.Where(x => x.CustomerAssetComponentId == position.Id && x.ComponentId == replacement.ComponentId))
            {
                old.Complete(facts.CompletedAt, facts.OperatorId, successor?.Id, null, maintenance.Id);
                closed.Add(old);
            }
        }
        await events.InsertManyAsync(appended);
        // Flush old pending rows first for the filtered unique pending-position index.
        await reminders.UpdateManyAsync(closed, autoSave: true);
        await positions.UpdateManyAsync(changedPositions);
        await reminders.InsertManyAsync(successors);
    }
}
