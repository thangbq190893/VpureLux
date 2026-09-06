using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using VPureLux.Service;
using VPureLux.Catalog;
using VPureLux.Customers;
using VPureLux.Permissions;
using VPureLux.Sales;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Uow;

namespace VPureLux.Warranty;

[Authorize(VPureLuxPermissions.Warranty.View)]
public class WarrantyAppService : ApplicationService, IWarrantyAppService
{
    private readonly IComponentReplacementPolicyRepository _policies;
    private readonly IProductMachineSettingRepository _machineSettings;
    private readonly ICustomerCareSyncFailureRepository _syncFailures;
    private readonly VPureLux.Catalog.IProductRepository _products;
    private readonly IComponentRepository _components;
    private readonly ICustomerRepository _customers;
    private readonly IRepository<CustomerAsset, Guid> _assets;
    private readonly IRepository<CustomerAssetComponent, Guid> _assetComponents;
    private readonly IRepository<AssetMaintenanceEvent, Guid> _maintenanceEvents;
    private readonly IRepository<AssetReplacementReminder, Guid> _reminders;
    private readonly IWarrantyReadRepository _readRepository;
    private readonly ISalesOrderRepository _salesOrders;
    private readonly ISalesOrderRevisionRepository _salesRevisions;
    private readonly ISalesOrderCancellationRepository _salesCancellations;
    private readonly SalesOrderOperationCoordinator _salesOrderCoordinator;
    private readonly IUnitOfWorkManager _unitOfWorkManager;
    private readonly CustomerAssetOperationCoordinator _assetCoordinator;
    private readonly IOptions<ServiceOptions> _serviceOptions;

    public WarrantyAppService(
        IComponentReplacementPolicyRepository policies,
        IProductMachineSettingRepository machineSettings,
        ICustomerCareSyncFailureRepository syncFailures,
        VPureLux.Catalog.IProductRepository products,
        IComponentRepository components,
        ICustomerRepository customers,
        IRepository<CustomerAsset, Guid> assets,
        IRepository<CustomerAssetComponent, Guid> assetComponents,
        IRepository<AssetMaintenanceEvent, Guid> maintenanceEvents,
        IRepository<AssetReplacementReminder, Guid> reminders,
        IWarrantyReadRepository readRepository,
        ISalesOrderRepository salesOrders,
        ISalesOrderRevisionRepository salesRevisions,
        ISalesOrderCancellationRepository salesCancellations,
        SalesOrderOperationCoordinator salesOrderCoordinator,
        IUnitOfWorkManager unitOfWorkManager,
        CustomerAssetOperationCoordinator assetCoordinator,
        IOptions<ServiceOptions> serviceOptions)
    {
        _policies = policies;
        _machineSettings = machineSettings;
        _syncFailures = syncFailures;
        _products = products;
        _components = components;
        _customers = customers;
        _assets = assets;
        _assetComponents = assetComponents;
        _maintenanceEvents = maintenanceEvents;
        _reminders = reminders;
        _readRepository = readRepository;
        _salesOrders = salesOrders;
        _salesRevisions = salesRevisions;
        _salesCancellations = salesCancellations;
        _salesOrderCoordinator = salesOrderCoordinator;
        _unitOfWorkManager = unitOfWorkManager;
        _assetCoordinator = assetCoordinator;
        _serviceOptions = serviceOptions;
    }

    public async Task<PagedResultDto<ProductMachineSettingListDto>> GetMachineSettingListAsync(
        GetProductMachineSettingListInput input)
    {
        var filter = new ProductMachineSettingFilter
        {
            SearchText = input.SearchText,
            IsMachine = input.IsMachine,
            Sorting = input.Sorting,
            SkipCount = input.SkipCount,
            MaxResultCount = input.MaxResultCount
        };
        var totalCount = await _readRepository.GetMachineSettingCountAsync(filter);
        var items = await _readRepository.GetMachineSettingListAsync(filter);
        return new PagedResultDto<ProductMachineSettingListDto>(
            totalCount,
            items.Select(ToDto).ToList());
    }

    public async Task<ProductMachineSettingDto?> GetMachineSettingByProductIdAsync(Guid productId)
    {
        var setting = await _machineSettings.FindByProductIdAsync(productId);
        return setting == null ? null : ToDto(setting);
    }

    [Authorize(VPureLuxPermissions.Warranty.ManageMachines)]
    public async Task<List<ProductMachineSettingDto>> GetMachineSettingsByProductIdsAsync(
        IReadOnlyCollection<Guid> productIds)
    {
        var ids = productIds.Where(id => id != Guid.Empty).Distinct().ToArray();
        if (ids.Length == 0)
        {
            return [];
        }

        return (await _machineSettings.GetByProductIdsAsync(ids))
            .Select(ToDto)
            .ToList();
    }

    [Authorize(VPureLuxPermissions.Warranty.ManageMachines)]
    public async Task<ProductMachineSettingDto> SetMachineSettingAsync(
        Guid productId,
        SetProductMachineSettingDto input)
    {
        if (productId == Guid.Empty || await _products.FindAsync(productId) == null)
        {
            throw new BusinessException(VPureLuxDomainErrorCodes.EntityNotFound);
        }

        var setting = await _machineSettings.FindByProductIdAsync(productId);
        if (setting == null)
        {
            if (!input.IsMachine)
            {
                return new ProductMachineSettingDto
                {
                    ProductId = productId,
                    IsMachine = false
                };
            }

            setting = new ProductMachineSetting(GuidGenerator.Create(), productId, true, input.Note);
            await _machineSettings.InsertAsync(setting, autoSave: true);
        }
        else
        {
            setting.Update(input.IsMachine, input.Note);
            await _machineSettings.UpdateAsync(setting, autoSave: true);
        }

        return ToDto(setting);
    }

    [Authorize(VPureLuxPermissions.Warranty.ManageSyncFailures)]
    public async Task<PagedResultDto<CustomerCareSyncFailureListDto>> GetSyncFailureListAsync(
        GetCustomerCareSyncFailureListInput input)
    {
        var filter = new CustomerCareSyncFailureFilter
        {
            SearchText = input.SearchText,
            Status = input.Status,
            Sorting = input.Sorting,
            SkipCount = input.SkipCount,
            MaxResultCount = input.MaxResultCount
        };
        var totalCount = await _readRepository.GetSyncFailureCountAsync(filter);
        var items = await _readRepository.GetSyncFailureListAsync(filter);
        return new PagedResultDto<CustomerCareSyncFailureListDto>(
            totalCount,
            items.Select(ToDto).ToList());
    }

    [Authorize(VPureLuxPermissions.Warranty.ManageSyncFailures)]
    public async Task RetrySyncFailureAsync(Guid id)
    {
        var failure = await _syncFailures.FindAsync(id)
            ?? throw new BusinessException(VPureLuxDomainErrorCodes.EntityNotFound);
        failure.ScheduleRetry(Clock.Now);
        await _syncFailures.UpdateAsync(failure, autoSave: true);
    }

    [Authorize(VPureLuxPermissions.Warranty.ManageInstallations)]
    public async Task<PagedResultDto<PendingInstallationListDto>> GetPendingInstallationListAsync(
        GetPendingInstallationListInput input)
    {
        var filter = new PendingInstallationFilter
        {
            SearchText = input.SearchText,
            Sorting = input.Sorting,
            SkipCount = input.SkipCount,
            MaxResultCount = input.MaxResultCount
        };
        var totalCount = await _readRepository.GetPendingInstallationCountAsync(filter);
        var items = await _readRepository.GetPendingInstallationListAsync(filter);
        return new PagedResultDto<PendingInstallationListDto>(
            totalCount,
            items.Select(ToDto).ToList());
    }

    [Authorize(VPureLuxPermissions.Warranty.ManageInstallations)]
    public async Task<AssetInstallationEditorDto> GetInstallationEditorAsync(Guid id)
    {
        var asset = await _assets.FindAsync(id)
            ?? throw new BusinessException(VPureLuxDomainErrorCodes.EntityNotFound);
        if (asset.Status != CustomerAssetStatus.PendingInstallation)
        {
            throw new BusinessException(VPureLuxDomainErrorCodes.ValidationFailed);
        }

        var positionQuery = (await _assetComponents.GetQueryableAsync())
            .Where(position => position.CustomerAssetId == id)
            .OrderBy(position => position.PositionCode);
        var positions = await AsyncExecuter.ToListAsync(positionQuery);

        return new AssetInstallationEditorDto
        {
            Id = asset.Id,
            AssetNo = asset.AssetNo,
            CustomerCode = asset.CustomerCodeSnapshot,
            CustomerName = asset.CustomerNameSnapshot,
            ProductCode = asset.ProductCodeSnapshot ?? string.Empty,
            ProductName = asset.ProductNameSnapshot ?? string.Empty,
            OrderNo = asset.OrderNoSnapshot ?? string.Empty,
            SerialNo = asset.SerialNo,
            InstallationAddress = asset.InstallationAddress,
            InstalledAt = asset.InstalledAt,
            Status = asset.Status,
            Positions = positions.Select(position => new AssetInstallationPositionDto
            {
                Id = position.Id,
                PositionCode = position.PositionCode,
                PositionName = position.PositionName,
                ComponentId = position.ComponentId,
                ComponentCode = position.ComponentCodeSnapshot,
                ComponentName = position.ComponentNameSnapshot,
                ComponentUnit = position.ComponentUnitSnapshot,
                Quantity = position.Quantity,
                IsIncluded = position.Status != CustomerAssetComponentStatus.Inactive,
                Note = position.Note
            }).ToList()
        };
    }

    [Authorize(VPureLuxPermissions.Warranty.ManageInstallations)]
    [UnitOfWork(IsDisabled = true)]
    public async Task<ConfirmAssetInstallationResultDto> ConfirmInstallationAsync(
        Guid id,
        ConfirmAssetInstallationDto input)
    {
        Guid operationId;
        using (var unitOfWork = _unitOfWorkManager.Begin(requiresNew: true, isTransactional: false))
        {
            var asset = await _assets.GetAsync(id);
            operationId = asset.SalesOrderId ?? asset.Id;
            await unitOfWork.CompleteAsync();
        }

        return await _salesOrderCoordinator.ExecuteAsync(operationId, () => ConfirmInstallationCoreAsync(id, input));
    }

    private async Task<ConfirmAssetInstallationResultDto> ConfirmInstallationCoreAsync(
        Guid id,
        ConfirmAssetInstallationDto input)
    {
        await _assetCoordinator.HoldAsync(id);
        var asset = await _assets.GetAsync(id);
        if (asset.SalesOrderId.HasValue)
        {
            var order = await _salesOrders.FindAsync(asset.SalesOrderId.Value, includeDetails: false)
                ?? throw new BusinessException(VPureLuxDomainErrorCodes.SalesOrderNotFound);
            if (order.Status != SalesOrderStatus.Confirmed)
            {
                throw new BusinessException(VPureLuxDomainErrorCodes.SalesRevisionNotAllowed);
            }
            if (await _salesRevisions.FindActiveByOrderIdAsync(order.Id) != null ||
                await _salesCancellations.FindByOrderIdAsync(order.Id) != null)
            {
                throw new BusinessException(VPureLuxDomainErrorCodes.SalesRevisionAlreadyActive);
            }
        }
        var existingQuery = (await _assetComponents.GetQueryableAsync())
            .Where(position => position.CustomerAssetId == id);
        var existingPositions = await AsyncExecuter.ToListAsync(existingQuery);

        if (asset.InstallationIdempotencyKey == input.IdempotencyKey)
        {
            var reminderQuery = (await _reminders.GetQueryableAsync())
                .Where(reminder =>
                    reminder.CustomerAssetId == id &&
                    reminder.TriggerSource == ReplacementReminderTriggerSource.Installation);
            return new ConfirmAssetInstallationResultDto
            {
                AssetId = asset.Id,
                ActivePositionCount = existingPositions.Count(position => position.Status != CustomerAssetComponentStatus.Inactive),
                CreatedReminderCount = await AsyncExecuter.CountAsync(reminderQuery),
                IsReplay = true
            };
        }

        if (asset.Source != CustomerAssetSource.SoldByCompany ||
            asset.Status != CustomerAssetStatus.PendingInstallation ||
            !asset.ProductId.HasValue)
        {
            throw new BusinessException(VPureLuxDomainErrorCodes.ValidationFailed);
        }

        var machineSetting = await _machineSettings.FindByProductIdAsync(asset.ProductId.Value);
        if (machineSetting is not { IsMachine: true })
        {
            throw new BusinessException(VPureLuxDomainErrorCodes.ValidationFailed);
        }

        var included = input.Positions.Where(position => position.IsIncluded).ToList();
        if (included.Count == 0 ||
            included.Any(position => position.Quantity <= 0 || position.PositionCode.IsNullOrWhiteSpace()) ||
            included.Select(position => position.PositionCode.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).Count() != included.Count)
        {
            throw new BusinessException(VPureLuxDomainErrorCodes.ValidationFailed);
        }

        var existingById = existingPositions.ToDictionary(position => position.Id);
        var referencedIds = input.Positions.Where(position => position.Id.HasValue).Select(position => position.Id!.Value).ToList();
        if (referencedIds.Distinct().Count() != referencedIds.Count ||
            referencedIds.Any(positionId => !existingById.ContainsKey(positionId)))
        {
            throw new BusinessException(VPureLuxDomainErrorCodes.ValidationFailed);
        }

        var includedById = included.Where(position => position.Id.HasValue)
            .ToDictionary(position => position.Id!.Value);
        var finalPositionCodes = existingPositions
            .Select(existing => includedById.TryGetValue(existing.Id, out var replacement)
                ? replacement.PositionCode.Trim()
                : existing.PositionCode)
            .Concat(included.Where(position => !position.Id.HasValue).Select(position => position.PositionCode.Trim()))
            .ToList();
        if (finalPositionCodes.Distinct(StringComparer.OrdinalIgnoreCase).Count() != finalPositionCodes.Count())
        {
            throw new BusinessException(VPureLuxDomainErrorCodes.ValidationFailed);
        }

        var componentIds = included
            .Where(position => position.ComponentId.HasValue)
            .Select(position => position.ComponentId!.Value)
            .Distinct()
            .ToList();
        var componentQuery = (await _components.GetQueryableAsync())
            .Where(component => componentIds.Contains(component.Id) && component.Status == CatalogItemStatus.Active);
        var componentById = (await AsyncExecuter.ToListAsync(componentQuery)).ToDictionary(component => component.Id);
        if (componentById.Count != componentIds.Count)
        {
            throw new BusinessException(VPureLuxDomainErrorCodes.ValidationFailed);
        }

        var policyByComponentId = (await _policies.GetEnabledByComponentIdsAsync(componentIds))
            .ToDictionary(policy => policy.ComponentId);
        var installedAt = input.InstalledAt;
        var activePositions = new List<CustomerAssetComponent>();
        var addedPositions = new List<CustomerAssetComponent>();

        foreach (var positionInput in included)
        {
            var position = positionInput.Id.HasValue
                ? existingById[positionInput.Id.Value]
                : new CustomerAssetComponent(
                    GuidGenerator.Create(),
                    asset.Id,
                    positionInput.PositionCode,
                    positionInput.PositionName,
                    null,
                    null,
                    null,
                    null,
                    positionInput.Quantity,
                    null,
                    pendingInstallation: true,
                    positionInput.Note);

            position.UpdatePosition(
                positionInput.PositionCode.Trim(),
                positionInput.PositionName.Trim(),
                positionInput.Quantity,
                positionInput.Note);
            if (positionInput.ComponentId.HasValue)
            {
                var component = componentById[positionInput.ComponentId.Value];
                position.MapComponent(component.Id, component.Code, component.Name, component.Unit, positionInput.Note);
            }
            else
            {
                position.ClearMapping(positionInput.Note);
            }

            position.SetReplacementBaseline(installedAt);
            activePositions.Add(position);
            if (!positionInput.Id.HasValue)
            {
                addedPositions.Add(position);
            }
        }

        foreach (var position in existingPositions.Where(existing =>
                     input.Positions.All(candidate => candidate.Id != existing.Id || !candidate.IsIncluded)))
        {
            position.Deactivate(position.Note);
        }

        asset.UpdateIdentification(input.SerialNo, asset.Brand, asset.Model, asset.Note);
        asset.ConfirmInstallation(installedAt, input.InstallationAddress, CurrentUser.Id, input.IdempotencyKey);

        var installationEvent = new AssetMaintenanceEvent(
            GuidGenerator.Create(),
            asset.Id,
            null,
            AssetMaintenanceEventType.Installation,
            AssetMaintenanceSourceType.SalesInstallation,
            installedAt,
            HashKey("installation-event", asset.Id, input.IdempotencyKey),
            sourceId: asset.SalesOrderId,
            note: input.InstallationAddress);

        var reminders = activePositions
            .Where(position =>
                position.ComponentId.HasValue &&
                policyByComponentId.ContainsKey(position.ComponentId.Value))
            .Select(position =>
            {
                var policy = policyByComponentId[position.ComponentId!.Value];
                return new AssetReplacementReminder(
                    GuidGenerator.Create(),
                    asset.Id,
                    position.Id,
                    position.ComponentId.Value,
                    asset.SalesOrderId,
                    asset.SalesOrderLineId,
                    position.ComponentCodeSnapshot!,
                    position.ComponentNameSnapshot!,
                    position.ComponentUnitSnapshot!,
                    position.Quantity,
                    installedAt.Date.AddMonths(policy.CycleMonths),
                    policy.CycleMonths,
                    policy.WarningDaysBeforeDue,
                    ReplacementReminderTriggerSource.Installation,
                    nameof(AssetMaintenanceEvent),
                    installationEvent.Id,
                    HashKey("installation-reminder", position.Id, input.IdempotencyKey));
            })
            .ToList();

        if (addedPositions.Count > 0)
        {
            await _assetComponents.InsertManyAsync(addedPositions);
        }

        await _maintenanceEvents.InsertAsync(installationEvent);
        if (reminders.Count > 0)
        {
            await _reminders.InsertManyAsync(reminders);
        }

        await _assets.UpdateAsync(asset, autoSave: true);
        return new ConfirmAssetInstallationResultDto
        {
            AssetId = asset.Id,
            ActivePositionCount = activePositions.Count,
            CreatedReminderCount = reminders.Count,
            IsReplay = false
        };
    }

    [Authorize(VPureLuxPermissions.Warranty.ManageAssets)]
    public async Task<PagedResultDto<CustomerAssetListDto>> GetAssetListAsync(GetCustomerAssetListInput input)
    {
        var filter = new CustomerAssetFilter
        {
            SearchText = input.SearchText,
            Source = input.Source,
            Sorting = input.Sorting,
            SkipCount = input.SkipCount,
            MaxResultCount = input.MaxResultCount
        };
        var totalCount = await _readRepository.GetAssetCountAsync(filter);
        var items = await _readRepository.GetAssetListAsync(filter);
        return new PagedResultDto<CustomerAssetListDto>(totalCount, items.Select(ToDto).ToList());
    }

    [Authorize(VPureLuxPermissions.Warranty.ManageAssets)]
    public async Task<CustomerAssetDetailDto> GetAssetDetailsAsync(Guid id)
    {
        var asset = await _assets.GetAsync(id);
        var positions = await AsyncExecuter.ToListAsync(
            (await _assetComponents.GetQueryableAsync())
            .Where(position => position.CustomerAssetId == id)
            .OrderBy(position => position.PositionCode));
        return new CustomerAssetDetailDto
        {
            Id = asset.Id,
            AssetNo = asset.AssetNo,
            CustomerId = asset.CustomerId,
            CustomerCode = asset.CustomerCodeSnapshot,
            CustomerName = asset.CustomerNameSnapshot,
            Source = asset.Source,
            ProductCode = asset.ProductCodeSnapshot,
            ProductName = asset.ProductNameSnapshot,
            Brand = asset.Brand,
            Model = asset.Model,
            SerialNo = asset.SerialNo,
            InstallationAddress = asset.InstallationAddress,
            ExternalReference = asset.ExternalReference,
            Status = asset.EffectiveStatus,
            Note = asset.Note,
            Positions = positions.Select(position => new ExternalAssetPositionInput
            {
                Id = position.Id,
                PositionCode = position.PositionCode,
                PositionName = position.PositionName,
                ComponentId = position.ComponentId,
                ComponentCode = position.ComponentCodeSnapshot,
                ComponentName = position.ComponentNameSnapshot,
                ComponentUnit = position.ComponentUnitSnapshot,
                Quantity = position.Quantity,
                ReplacementBaselineDate = position.ReplacementBaselineDate,
                Note = position.Note
            }).ToList()
        };
    }

    [Authorize(VPureLuxPermissions.Warranty.ManageAssets)]
    public async Task<ExternalCustomerAssetResultDto> CreateExternalAssetAsync(
        CreateExternalCustomerAssetDto input)
    {
        var customer = await _customers.FindAsync(input.CustomerId);
        if (customer is not { Status: CustomerStatus.Active })
        {
            throw new BusinessException(VPureLuxDomainErrorCodes.ValidationFailed);
        }

        var replayQuery = (await _assets.GetQueryableAsync())
            .Where(asset => asset.InstallationIdempotencyKey == input.IdempotencyKey);
        var replay = await AsyncExecuter.FirstOrDefaultAsync(replayQuery);
        if (replay != null)
        {
            if (replay.Source != CustomerAssetSource.External)
            {
                throw new BusinessException(VPureLuxDomainErrorCodes.ValidationFailed);
            }

            var replayPositionCount = await AsyncExecuter.CountAsync(
                (await _assetComponents.GetQueryableAsync()).Where(position => position.CustomerAssetId == replay.Id));
            var replayReminderCount = await AsyncExecuter.CountAsync(
                (await _reminders.GetQueryableAsync()).Where(reminder => reminder.CustomerAssetId == replay.Id));
            return new ExternalCustomerAssetResultDto
            {
                AssetId = replay.Id,
                AssetNo = replay.AssetNo,
                PositionCount = replayPositionCount,
                CreatedReminderCount = replayReminderCount
            };
        }

        ValidateExternalPositions(input.Positions);
        var componentIds = input.Positions
            .Where(position => position.ComponentId.HasValue)
            .Select(position => position.ComponentId!.Value)
            .Distinct()
            .ToList();
        var componentById = (await AsyncExecuter.ToListAsync(
            (await _components.GetQueryableAsync())
            .Where(component => componentIds.Contains(component.Id) && component.Status == CatalogItemStatus.Active)))
            .ToDictionary(component => component.Id);
        if (componentById.Count != componentIds.Count)
        {
            throw new BusinessException(VPureLuxDomainErrorCodes.ValidationFailed);
        }

        var policyByComponentId = (await _policies.GetEnabledByComponentIdsAsync(componentIds))
            .ToDictionary(policy => policy.ComponentId);
        var serialNo = input.SerialNo?.Trim();
        var duplicateSerialAssetNos = new List<string>();
        if (!serialNo.IsNullOrWhiteSpace())
        {
            duplicateSerialAssetNos = await AsyncExecuter.ToListAsync(
                (await _assets.GetQueryableAsync())
                .Where(asset => asset.SerialNo == serialNo)
                .OrderBy(asset => asset.AssetNo)
                .Select(asset => asset.AssetNo));
        }

        var assetId = GuidGenerator.Create();
        var asset = CustomerAsset.CreateExternal(
            assetId,
            customer.Id,
            CreateExternalAssetNo(assetId),
            customer.Code,
            customer.Name,
            input.Model.Trim(),
            input.Brand?.Trim(),
            serialNo,
            externalReference: input.ExternalReference?.Trim(),
            note: input.Note);
        asset.CompleteExternalOnboarding(
            input.InstallationAddress,
            input.ExternalReference,
            CurrentUser.Id,
            input.IdempotencyKey);

        var positions = input.Positions.Select(positionInput =>
        {
            Component? component = null;
            if (positionInput.ComponentId.HasValue)
            {
                component = componentById[positionInput.ComponentId.Value];
            }

            return new CustomerAssetComponent(
                GuidGenerator.Create(),
                asset.Id,
                positionInput.PositionCode.Trim(),
                positionInput.PositionName.Trim(),
                component?.Id,
                component?.Code,
                component?.Name,
                component?.Unit,
                positionInput.Quantity,
                positionInput.ReplacementBaselineDate,
                pendingInstallation: false,
                positionInput.Note);
        }).ToList();
        var onboardingEvent = new AssetMaintenanceEvent(
            GuidGenerator.Create(),
            asset.Id,
            null,
            AssetMaintenanceEventType.Inspection,
            AssetMaintenanceSourceType.ExternalOnboarding,
            Clock.Now,
            HashKey("external-onboarding", asset.Id, input.IdempotencyKey),
            note: input.Note);
        var reminders = positions
            .Where(position =>
                position.ComponentId.HasValue &&
                position.ReplacementBaselineDate.HasValue &&
                policyByComponentId.ContainsKey(position.ComponentId.Value))
            .Select(position =>
            {
                var policy = policyByComponentId[position.ComponentId!.Value];
                return new AssetReplacementReminder(
                    GuidGenerator.Create(),
                    asset.Id,
                    position.Id,
                    position.ComponentId.Value,
                    null,
                    null,
                    position.ComponentCodeSnapshot!,
                    position.ComponentNameSnapshot!,
                    position.ComponentUnitSnapshot!,
                    position.Quantity,
                    position.ReplacementBaselineDate!.Value.AddMonths(policy.CycleMonths),
                    policy.CycleMonths,
                    policy.WarningDaysBeforeDue,
                    ReplacementReminderTriggerSource.Manual,
                    nameof(AssetMaintenanceEvent),
                    onboardingEvent.Id,
                    HashKey("external-reminder", position.Id, input.IdempotencyKey));
            }).ToList();

        await _assets.InsertAsync(asset);
        await _assetComponents.InsertManyAsync(positions);
        await _maintenanceEvents.InsertAsync(onboardingEvent);
        if (reminders.Count > 0)
        {
            await _reminders.InsertManyAsync(reminders);
        }
        await CurrentUnitOfWork!.SaveChangesAsync();

        return new ExternalCustomerAssetResultDto
        {
            AssetId = asset.Id,
            AssetNo = asset.AssetNo,
            PositionCount = positions.Count,
            CreatedReminderCount = reminders.Count,
            DuplicateSerialAssetNos = duplicateSerialAssetNos
        };
    }

    [Authorize(VPureLuxPermissions.Warranty.ManageAssets)]
    public async Task<ExternalCustomerAssetResultDto> UpdateExternalAssetAsync(
        Guid id,
        UpdateExternalCustomerAssetDto input)
    {
        await _assetCoordinator.HoldAsync(id);
        var asset = await _assets.GetAsync(id);
        if (asset.Source != CustomerAssetSource.External)
        {
            throw new BusinessException(VPureLuxDomainErrorCodes.ValidationFailed);
        }

        var eventKey = HashKey("external-update", asset.Id, input.IdempotencyKey);
        var replayEvent = await AsyncExecuter.FirstOrDefaultAsync(
            (await _maintenanceEvents.GetQueryableAsync())
            .Where(maintenanceEvent => maintenanceEvent.IdempotencyKey == eventKey));
        if (replayEvent != null)
        {
            return new ExternalCustomerAssetResultDto
            {
                AssetId = asset.Id,
                AssetNo = asset.AssetNo,
                PositionCount = await AsyncExecuter.CountAsync(
                    (await _assetComponents.GetQueryableAsync()).Where(position =>
                        position.CustomerAssetId == asset.Id && position.Status != CustomerAssetComponentStatus.Inactive)),
                CreatedReminderCount = await AsyncExecuter.CountAsync(
                    (await _reminders.GetQueryableAsync()).Where(reminder =>
                        reminder.SourceReferenceId == replayEvent.Id))
            };
        }

        ValidateExternalPositions(input.Positions);
        var existingPositions = await AsyncExecuter.ToListAsync(
            (await _assetComponents.GetQueryableAsync()).Where(position => position.CustomerAssetId == id));
        var existingById = existingPositions.ToDictionary(position => position.Id);
        var referencedIds = input.Positions.Where(position => position.Id.HasValue)
            .Select(position => position.Id!.Value).ToList();
        if (referencedIds.Distinct().Count() != referencedIds.Count ||
            referencedIds.Any(positionId => !existingById.ContainsKey(positionId)))
        {
            throw new BusinessException(VPureLuxDomainErrorCodes.ValidationFailed);
        }

        var finalCodes = existingPositions
            .Select(existing => input.Positions.FirstOrDefault(candidate => candidate.Id == existing.Id)?.PositionCode.Trim()
                ?? existing.PositionCode)
            .Concat(input.Positions.Where(position => !position.Id.HasValue).Select(position => position.PositionCode.Trim()))
            .ToList();
        if (finalCodes.Distinct(StringComparer.OrdinalIgnoreCase).Count() != finalCodes.Count)
        {
            throw new BusinessException(VPureLuxDomainErrorCodes.ValidationFailed);
        }

        var componentIds = input.Positions.Where(position => position.ComponentId.HasValue)
            .Select(position => position.ComponentId!.Value).Distinct().ToList();
        var componentById = (await AsyncExecuter.ToListAsync(
            (await _components.GetQueryableAsync()).Where(component =>
                componentIds.Contains(component.Id) && component.Status == CatalogItemStatus.Active)))
            .ToDictionary(component => component.Id);
        if (componentById.Count != componentIds.Count)
        {
            throw new BusinessException(VPureLuxDomainErrorCodes.ValidationFailed);
        }

        var policyByComponentId = (await _policies.GetEnabledByComponentIdsAsync(componentIds))
            .ToDictionary(policy => policy.ComponentId);
        var openReminderByPositionId = (await AsyncExecuter.ToListAsync(
            (await _reminders.GetQueryableAsync()).Where(reminder =>
                reminder.CustomerAssetId == id &&
                reminder.CustomerAssetComponentId.HasValue &&
                reminder.Status == AssetReplacementReminderStatus.Pending)))
            .ToDictionary(reminder => reminder.CustomerAssetComponentId!.Value);

        var activePositions = new List<CustomerAssetComponent>();
        var addedPositions = new List<CustomerAssetComponent>();
        foreach (var positionInput in input.Positions)
        {
            CustomerAssetComponent position;
            if (positionInput.Id.HasValue)
            {
                position = existingById[positionInput.Id.Value];
                var baseline = positionInput.ReplacementBaselineDate?.Date;
                if (openReminderByPositionId.ContainsKey(position.Id) &&
                    (position.ComponentId != positionInput.ComponentId || position.ReplacementBaselineDate != baseline))
                {
                    throw new BusinessException(VPureLuxDomainErrorCodes.ValidationFailed);
                }
            }
            else
            {
                position = new CustomerAssetComponent(
                    GuidGenerator.Create(), asset.Id, positionInput.PositionCode, positionInput.PositionName,
                    null, null, null, null, positionInput.Quantity, null, pendingInstallation: false, positionInput.Note);
                addedPositions.Add(position);
            }

            position.UpdatePosition(positionInput.PositionCode.Trim(), positionInput.PositionName.Trim(), positionInput.Quantity, positionInput.Note);
            if (positionInput.ComponentId.HasValue)
            {
                var component = componentById[positionInput.ComponentId.Value];
                position.MapComponent(component.Id, component.Code, component.Name, component.Unit, positionInput.Note);
            }
            else
            {
                position.ClearMapping(positionInput.Note);
            }

            if (positionInput.ReplacementBaselineDate.HasValue)
            {
                position.SetReplacementBaseline(positionInput.ReplacementBaselineDate.Value);
            }
            else
            {
                position.ClearReplacementBaseline();
            }
            activePositions.Add(position);
        }

        foreach (var omitted in existingPositions.Where(existing => input.Positions.All(candidate => candidate.Id != existing.Id)))
        {
            if (openReminderByPositionId.ContainsKey(omitted.Id))
            {
                throw new BusinessException(VPureLuxDomainErrorCodes.ValidationFailed);
            }
            omitted.Deactivate(omitted.Note);
        }

        var serialNo = input.SerialNo?.Trim();
        var duplicateSerialAssetNos = serialNo.IsNullOrWhiteSpace()
            ? []
            : await AsyncExecuter.ToListAsync(
                (await _assets.GetQueryableAsync())
                .Where(other => other.Id != id && other.SerialNo == serialNo)
                .OrderBy(other => other.AssetNo)
                .Select(other => other.AssetNo));
        asset.UpdateIdentification(serialNo, input.Brand?.Trim(), input.Model.Trim(), input.Note);
        asset.UpdateExternalProfile(input.InstallationAddress, input.ExternalReference?.Trim());

        var updateEvent = new AssetMaintenanceEvent(
            GuidGenerator.Create(), asset.Id, null,
            AssetMaintenanceEventType.MappingChanged,
            AssetMaintenanceSourceType.ExternalOnboarding,
            Clock.Now, eventKey, note: input.Note);
        var reminders = activePositions
            .Where(position =>
                !openReminderByPositionId.ContainsKey(position.Id) &&
                position.ComponentId.HasValue &&
                position.ReplacementBaselineDate.HasValue &&
                policyByComponentId.ContainsKey(position.ComponentId.Value))
            .Select(position =>
            {
                var policy = policyByComponentId[position.ComponentId!.Value];
                return new AssetReplacementReminder(
                    GuidGenerator.Create(), asset.Id, position.Id, position.ComponentId.Value,
                    null, null, position.ComponentCodeSnapshot!, position.ComponentNameSnapshot!,
                    position.ComponentUnitSnapshot!, position.Quantity,
                    position.ReplacementBaselineDate!.Value.AddMonths(policy.CycleMonths),
                    policy.CycleMonths, policy.WarningDaysBeforeDue,
                    ReplacementReminderTriggerSource.Manual,
                    nameof(AssetMaintenanceEvent), updateEvent.Id,
                    HashKey("external-update-reminder", position.Id, input.IdempotencyKey));
            }).ToList();

        if (addedPositions.Count > 0)
        {
            await _assetComponents.InsertManyAsync(addedPositions);
        }
        await _maintenanceEvents.InsertAsync(updateEvent);
        if (reminders.Count > 0)
        {
            await _reminders.InsertManyAsync(reminders);
        }
        await _assets.UpdateAsync(asset, autoSave: true);

        return new ExternalCustomerAssetResultDto
        {
            AssetId = asset.Id,
            AssetNo = asset.AssetNo,
            PositionCount = activePositions.Count,
            CreatedReminderCount = reminders.Count,
            DuplicateSerialAssetNos = duplicateSerialAssetNos
        };
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
    public async Task<List<ComponentReplacementPolicyDto>> GetPoliciesByComponentIdsAsync(
        IReadOnlyCollection<Guid> componentIds)
    {
        var ids = componentIds.Where(id => id != Guid.Empty).Distinct().ToArray();
        if (ids.Length == 0)
        {
            return [];
        }

        return (await _policies.GetByComponentIdsAsync(ids))
            .Select(ToDto)
            .ToList();
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
            TimingStatus = input.TimingStatus,
            AsOfDate = Clock.Now.Date,
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

    public async Task<WarrantyNotificationSummaryDto> GetNotificationSummaryAsync()
    {
        var summary = await _readRepository.GetNotificationSummaryAsync(Clock.Now.Date);
        return new WarrantyNotificationSummaryDto
        {
            WarningCount = summary.WarningCount,
            OverdueCount = summary.OverdueCount
        };
    }

    public async Task<PagedResultDto<AssetMaintenanceEventListDto>> GetAssetHistoryAsync(
        GetAssetMaintenanceHistoryInput input)
    {
        if (input.CustomerAssetId == Guid.Empty)
        {
            throw new BusinessException(VPureLuxDomainErrorCodes.ValidationFailed);
        }
        var filter = new AssetMaintenanceHistoryFilter
        {
            CustomerAssetId = input.CustomerAssetId,
            Sorting = input.Sorting,
            SkipCount = input.SkipCount,
            MaxResultCount = input.MaxResultCount
        };
        var count = await _readRepository.GetAssetHistoryCountAsync(filter);
        var items = await _readRepository.GetAssetHistoryListAsync(filter);
        return new PagedResultDto<AssetMaintenanceEventListDto>(count, items.Select(ToDto).ToList());
    }

    [Authorize(VPureLuxPermissions.Warranty.ManageReminders)]
    public async Task CompleteReminderAsync(Guid id, CompleteReplacementReminderDto input)
    {
        if (_serviceOptions.Value.IsEnabled)
            throw new BusinessException(ServiceErrorCodes.ReplacementRequiresService);
        await HoldReminderAssetAsync(id);
        var reminder = await GetReminderEntityAsync(id);
        var eventKey = HashKey("reminder-complete", id, input.IdempotencyKey);
        if (await MaintenanceEventExistsAsync(eventKey))
        {
            return;
        }
        var completedAt = (input.CompletedAt ?? Clock.Now).Date;
        CustomerAssetComponent? position = null;
        if (reminder.CustomerAssetComponentId.HasValue)
        {
            position = await _assetComponents.GetAsync(reminder.CustomerAssetComponentId.Value);
        }
        var hasCurrentPosition = position is
        {
            Status: CustomerAssetComponentStatus.Active,
            ComponentId: not null
        } && position.ComponentId == reminder.ComponentId;
        var currentPolicy = hasCurrentPosition
            ? await _policies.FindByComponentIdAsync(reminder.ComponentId)
            : null;
        var currentComponent = currentPolicy is { IsEnabled: true }
            ? await _components.FindAsync(reminder.ComponentId)
            : null;
        var maintenanceEvent = new AssetMaintenanceEvent(
            GuidGenerator.Create(), reminder.CustomerAssetId, reminder.CustomerAssetComponentId,
            AssetMaintenanceEventType.Replacement, AssetMaintenanceSourceType.Manual,
            completedAt, eventKey, sourceId: reminder.Id, componentId: reminder.ComponentId,
            componentCode: reminder.ComponentCodeSnapshot, componentName: reminder.ComponentNameSnapshot,
            note: input.Note);
        var nextReminder = hasCurrentPosition &&
                           currentPolicy is { IsEnabled: true } &&
                           currentComponent is { Status: CatalogItemStatus.Active }
            ? new AssetReplacementReminder(
                GuidGenerator.Create(), reminder.CustomerAssetId, position!.Id, reminder.ComponentId,
                reminder.SalesOrderId, reminder.SalesOrderLineId,
                reminder.ComponentCodeSnapshot, reminder.ComponentNameSnapshot, reminder.ComponentUnitSnapshot,
                reminder.QuantityPerProductSnapshot, completedAt.AddMonths(currentPolicy.CycleMonths),
                currentPolicy.CycleMonths, currentPolicy.WarningDaysBeforeDue,
                ReplacementReminderTriggerSource.Replacement, nameof(AssetMaintenanceEvent), maintenanceEvent.Id,
                HashKey("replacement-reminder", position.Id, input.IdempotencyKey))
            : null;

        reminder.Complete(completedAt, CurrentUser.Id, nextReminder?.Id, input.Note);
        if (hasCurrentPosition)
        {
            position!.SetReplacementBaseline(completedAt);
            await _assetComponents.UpdateAsync(position);
        }
        await _maintenanceEvents.InsertAsync(maintenanceEvent);
        await _reminders.UpdateAsync(reminder, autoSave: true);
        if (nextReminder != null)
        {
            await _reminders.InsertAsync(nextReminder, autoSave: true);
        }
    }

    [Authorize(VPureLuxPermissions.Warranty.ManageReminders)]
    public async Task SkipReminderAsync(Guid id, SkipReplacementReminderDto input)
    {
        await HoldReminderAssetAsync(id);
        var reminder = await GetReminderEntityAsync(id);
        var eventKey = HashKey("reminder-skip", id, input.IdempotencyKey);
        if (await MaintenanceEventExistsAsync(eventKey))
        {
            return;
        }
        reminder.Skip(input.Note);
        await _maintenanceEvents.InsertAsync(new AssetMaintenanceEvent(
            GuidGenerator.Create(), reminder.CustomerAssetId, reminder.CustomerAssetComponentId,
            AssetMaintenanceEventType.Inspection, AssetMaintenanceSourceType.Manual,
            Clock.Now, eventKey, sourceId: reminder.Id, componentId: reminder.ComponentId,
            componentCode: reminder.ComponentCodeSnapshot, componentName: reminder.ComponentNameSnapshot,
            note: input.Note));
        await _reminders.UpdateAsync(reminder, autoSave: true);
    }

    [Authorize(VPureLuxPermissions.Warranty.ManageReminders)]
    public async Task RescheduleReminderAsync(Guid id, RescheduleReplacementReminderDto input)
    {
        await HoldReminderAssetAsync(id);
        var reminder = await GetReminderEntityAsync(id);
        var eventKey = HashKey("reminder-reschedule", id, input.IdempotencyKey);
        if (await MaintenanceEventExistsAsync(eventKey))
        {
            return;
        }
        reminder.Reschedule(input.DueDate, input.Note);
        await _maintenanceEvents.InsertAsync(new AssetMaintenanceEvent(
            GuidGenerator.Create(), reminder.CustomerAssetId, reminder.CustomerAssetComponentId,
            AssetMaintenanceEventType.Inspection, AssetMaintenanceSourceType.Manual,
            Clock.Now, eventKey, sourceId: reminder.Id, componentId: reminder.ComponentId,
            componentCode: reminder.ComponentCodeSnapshot, componentName: reminder.ComponentNameSnapshot,
            note: input.Note));
        await _reminders.UpdateAsync(reminder, autoSave: true);
    }

    [Authorize(VPureLuxPermissions.Warranty.ManageReminders)]
    public async Task SuspendAssetAsync(Guid id, SuspendCustomerAssetDto input)
    {
        await _assetCoordinator.HoldAsync(id);
        var eventKey = HashKey("asset-suspend", id, input.IdempotencyKey);
        if (await MaintenanceEventExistsAsync(eventKey))
        {
            return;
        }
        var asset = await _assets.GetAsync(id);
        var reminders = await AsyncExecuter.ToListAsync(
            (await _reminders.GetQueryableAsync()).Where(reminder =>
                reminder.CustomerAssetId == id && reminder.Status == AssetReplacementReminderStatus.Pending));
        foreach (var reminder in reminders)
        {
            reminder.Cancel(input.Reason);
        }
        asset.Deactivate(input.Reason);
        await _maintenanceEvents.InsertAsync(new AssetMaintenanceEvent(
            GuidGenerator.Create(), asset.Id, null,
            AssetMaintenanceEventType.Deactivated, AssetMaintenanceSourceType.Manual,
            Clock.Now, eventKey, note: input.Reason));
        if (reminders.Count > 0)
        {
            await _reminders.UpdateManyAsync(reminders);
        }
        await _assets.UpdateAsync(asset, autoSave: true);
    }

    private async Task HoldReminderAssetAsync(Guid id)
    {
        var assetId = await AsyncExecuter.FirstOrDefaultAsync((await _reminders.GetQueryableAsync())
            .Where(x => x.Id == id).Select(x => x.CustomerAssetId));
        if (assetId == Guid.Empty) throw new BusinessException(VPureLuxDomainErrorCodes.EntityNotFound);
        await _assetCoordinator.HoldAsync(assetId);
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

    private async Task<bool> MaintenanceEventExistsAsync(string idempotencyKey) =>
        await AsyncExecuter.AnyAsync(
            (await _maintenanceEvents.GetQueryableAsync()).Where(maintenanceEvent =>
                maintenanceEvent.IdempotencyKey == idempotencyKey));

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

    private static ProductMachineSettingDto ToDto(ProductMachineSetting setting) =>
        new()
        {
            Id = setting.Id,
            ProductId = setting.ProductId,
            IsMachine = setting.IsMachine,
            Note = setting.Note
        };

    private static ProductMachineSettingListDto ToDto(ProductMachineSettingListItem item) =>
        new()
        {
            ProductId = item.ProductId,
            ProductCode = item.ProductCode,
            ProductName = item.ProductName,
            ProductStatus = item.ProductStatus,
            SettingId = item.SettingId,
            IsMachine = item.IsMachine,
            Note = item.Note
        };

    private static CustomerCareSyncFailureListDto ToDto(CustomerCareSyncFailureListItem item) =>
        new()
        {
            Id = item.Id,
            OrderNo = item.OrderNo,
            LineNo = item.LineNo,
            ProductCode = item.ProductCode,
            ProductName = item.ProductName,
            Quantity = item.Quantity,
            ErrorCode = item.ErrorCode,
            ErrorMessage = item.ErrorMessage,
            ErrorContext = item.ErrorContext,
            AttemptCount = item.AttemptCount,
            LastOccurredAt = item.LastOccurredAt,
            NextRetryAt = item.NextRetryAt,
            Status = item.Status
        };

    private static PendingInstallationListDto ToDto(PendingInstallationListItem item) =>
        new()
        {
            Id = item.Id,
            AssetNo = item.AssetNo,
            CustomerCode = item.CustomerCode,
            CustomerName = item.CustomerName,
            ProductCode = item.ProductCode,
            ProductName = item.ProductName,
            OrderNo = item.OrderNo,
            LineNo = item.LineNo,
            UnitIndex = item.UnitIndex,
            SoldDate = item.SoldDate,
            PositionCount = item.PositionCount
        };

    private static CustomerAssetListDto ToDto(CustomerAssetListItem item) =>
        new()
        {
            Id = item.Id,
            AssetNo = item.AssetNo,
            CustomerCode = item.CustomerCode,
            CustomerName = item.CustomerName,
            Source = item.Source,
            ProductCode = item.ProductCode,
            ProductName = item.ProductName,
            Brand = item.Brand,
            Model = item.Model,
            SerialNo = item.SerialNo,
            Status = item.Status,
            PositionCount = item.PositionCount,
            NextDueDate = item.NextDueDate
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
            CustomerAssetId = item.CustomerAssetId,
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
            WarningDate = item.WarningDate,
            CycleMonths = item.CycleMonths,
            WarningDaysBeforeDue = item.WarningDaysBeforeDue,
            Status = item.Status,
            TimingStatus = item.TimingStatus,
            OrderNo = item.OrderNo,
            LineNo = item.LineNo,
            Note = item.Note
        };

    private static AssetMaintenanceEventListDto ToDto(AssetMaintenanceEventListItem item) =>
        new()
        {
            ServiceOrderLineId = item.ServiceOrderLineId,
            Id = item.Id,
            CustomerAssetId = item.CustomerAssetId,
            CustomerAssetComponentId = item.CustomerAssetComponentId,
            EventType = item.EventType,
            SourceType = item.SourceType,
            ComponentCode = item.ComponentCode,
            ComponentName = item.ComponentName,
            OccurredAt = item.OccurredAt,
            CreatorId = item.CreatorId,
            Note = item.Note
        };


    private static string HashKey(string scope, Guid id, string key)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes($"{scope}:{id:N}:{key}"));
        return $"{scope}:{Convert.ToHexString(hash)}";
    }

    private static void ValidateExternalPositions(IReadOnlyCollection<ExternalAssetPositionInput> positions)
    {
        if (positions.Count == 0 ||
            positions.Any(position =>
                position.Quantity <= 0 ||
                position.PositionCode.IsNullOrWhiteSpace() ||
                position.PositionName.IsNullOrWhiteSpace()) ||
            positions.Select(position => position.PositionCode.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase).Count() != positions.Count)
        {
            throw new BusinessException(VPureLuxDomainErrorCodes.ValidationFailed);
        }
    }

    private static string CreateExternalAssetNo(Guid id) =>
        $"EXT-{id.ToString("N")[..12].ToUpperInvariant()}";
}
