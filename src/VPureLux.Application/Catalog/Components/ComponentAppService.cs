using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using VPureLux.BusinessCodes;
using VPureLux.Catalog.Components;
using VPureLux.Permissions;
using VPureLux.Warranty;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Auditing;
using Volo.Abp.Authorization;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Timing;

namespace VPureLux.Catalog.Components;

[Authorize(VPureLuxPermissions.Catalog.Components.Default)]
public class ComponentAppService : ApplicationService, IComponentAppService
{
    private const string MaterialSequenceName = "Material";
    private const string MaterialPrefix = "MAT";
    private const string DefaultSorting = "CreationTime DESC";

    private readonly IComponentRepository _componentRepository;
    private readonly CatalogManager _catalogManager;
    private readonly CatalogApplicationMapper _mapper;
    private readonly ICatalogImageProcessor _imageProcessor;
    private readonly IBusinessCodeGenerator _businessCodeGenerator;
    private readonly IClock _clock;
    private readonly IComponentReplacementPolicyRepository _replacementPolicies;

    public ComponentAppService(
        IComponentRepository componentRepository,
        CatalogManager catalogManager,
        CatalogApplicationMapper mapper,
        ICatalogImageProcessor imageProcessor,
        IBusinessCodeGenerator businessCodeGenerator,
        IClock clock,
        IComponentReplacementPolicyRepository replacementPolicies)
    {
        _componentRepository = componentRepository;
        _catalogManager = catalogManager;
        _mapper = mapper;
        _imageProcessor = imageProcessor;
        _businessCodeGenerator = businessCodeGenerator;
        _clock = clock;
        _replacementPolicies = replacementPolicies;
    }

    [Authorize(VPureLuxPermissions.Catalog.Components.View)]
    public async Task<PagedResultDto<ComponentDto>> GetListAsync(GetComponentListInput input)
    {
        var queryable = await _componentRepository.GetQueryableAsync();

        if (!input.Keyword.IsNullOrWhiteSpace())
        {
            queryable = queryable.Where(x =>
                x.Code.Contains(input.Keyword!) ||
                x.Name.Contains(input.Keyword!) ||
                x.Unit.Contains(input.Keyword!));
        }

        if (input.Status.HasValue)
        {
            queryable = queryable.Where(x => x.Status == input.Status.Value);
        }

        queryable = ApplySorting(queryable, input.Sorting);

        var totalCount = await AsyncExecuter.CountAsync(queryable);
        var replacementPolicies = await _replacementPolicies.GetQueryableAsync();
        var pageQuery = queryable
            .Skip(input.SkipCount)
            .Take(input.MaxResultCount);
        var items = await AsyncExecuter.ToListAsync(
            from component in pageQuery
            join policy in replacementPolicies on component.Id equals policy.ComponentId into componentPolicies
            from policy in componentPolicies.DefaultIfEmpty()
            select new ComponentDto
                {
                    Id = component.Id,
                    Code = component.Code,
                    Name = component.Name,
                    Description = component.Description,
                    Unit = component.Unit,
                    Status = component.Status,
                    IsReplacementTracked = policy != null && policy.IsEnabled,
                    ReplacementCycleMonths = policy == null ? null : policy.CycleMonths,
                    WarningDaysBeforeDue = policy == null ? null : policy.WarningDaysBeforeDue,
                    ReplacementPolicyNote = policy == null ? null : policy.Note,
                    CreationTime = component.CreationTime,
                    HasImage = component.Image!.ImageHash != null,
                    ImageHash = component.Image.ImageHash
                });

        return new PagedResultDto<ComponentDto>(totalCount, items);
    }

    [Authorize(VPureLuxPermissions.Catalog.Components.View)]
    public async Task<ComponentDto> GetAsync(Guid id)
    {
        var component = await GetComponentAsync(id);
        return ToDto(component, await _replacementPolicies.FindByComponentIdAsync(id));
    }

    [Authorize(VPureLuxPermissions.Catalog.Components.Create)]
    public async Task<ComponentDto> CreateAsync(CreateComponentDto input)
    {
        var code = await ResolveCodeAsync(input.Code);
        var component = await _catalogManager.CreateComponentAsync(
            code,
            input.Name,
            input.Description,
            input.Unit);

        await _componentRepository.InsertAsync(component, autoSave: true);

        ComponentReplacementPolicy? policy = null;
        if (input.ReplacementPolicy != null)
        {
            await EnsureReplacementPolicyPermissionAsync();
            if (input.ReplacementPolicy.IsEnabled)
            {
                policy = CreateReplacementPolicy(component.Id, input.ReplacementPolicy);
                await _replacementPolicies.InsertAsync(policy, autoSave: true);
            }
        }

        return ToDto(component, policy);
    }

    [Authorize(VPureLuxPermissions.Catalog.Components.Edit)]
    public async Task<ComponentDto> UpdateAsync(Guid id, UpdateComponentDto input)
    {
        var component = await GetComponentAsync(id);

        await _catalogManager.UpdateComponentAsync(component, input.Name, input.Description, input.Unit);
        await _componentRepository.UpdateAsync(component, autoSave: true);

        var policy = await _replacementPolicies.FindByComponentIdAsync(id);
        if (input.ReplacementPolicy != null)
        {
            await EnsureReplacementPolicyPermissionAsync();
            if (policy == null && input.ReplacementPolicy.IsEnabled)
            {
                policy = CreateReplacementPolicy(component.Id, input.ReplacementPolicy);
                await _replacementPolicies.InsertAsync(policy, autoSave: true);
            }
            else if (policy != null)
            {
                policy.Update(
                    input.ReplacementPolicy.CycleMonths,
                    input.ReplacementPolicy.WarningDaysBeforeDue,
                    input.ReplacementPolicy.Note,
                    input.ReplacementPolicy.IsEnabled);
                await _replacementPolicies.UpdateAsync(policy, autoSave: true);
            }
        }

        return ToDto(component, policy);
    }

    [Authorize(VPureLuxPermissions.Catalog.Components.Edit)]
    public async Task ActivateAsync(Guid id)
    {
        var component = await GetComponentAsync(id);
        component.Activate();
        await _componentRepository.UpdateAsync(component, autoSave: true);
    }

    [Authorize(VPureLuxPermissions.Catalog.Components.Edit)]
    public async Task DeactivateAsync(Guid id)
    {
        var component = await GetComponentAsync(id);
        component.Deactivate();
        await _componentRepository.UpdateAsync(component, autoSave: true);
    }

    [Authorize(VPureLuxPermissions.Catalog.Components.View)]
    [DisableAuditing]
    public async Task<CatalogImageDto> GetImageAsync(Guid id)
    {
        var image = (await GetComponentAsync(id)).Image;
        if (image == null)
        {
            throw ImageNotFound(id);
        }

        return ToImageDto(image, Convert.FromBase64String(image.ImageBase64));
    }

    [Authorize(VPureLuxPermissions.Catalog.Components.View)]
    [DisableAuditing]
    public async Task<CatalogImageDto> GetThumbnailAsync(Guid id)
    {
        var image = (await GetComponentAsync(id)).Image;
        if (image == null)
        {
            throw ImageNotFound(id);
        }

        return ToImageDto(image, _imageProcessor.CreateThumbnail(image), "image/webp", "thumbnail.webp");
    }

    [Authorize(VPureLuxPermissions.Catalog.Components.Edit)]
    [DisableAuditing]
    public async Task<CatalogImageMetadataDto> SetImageAsync(Guid id, CatalogImageUploadDto input)
    {
        var component = await GetComponentAsync(id);
        var image = _imageProcessor.Process(input.ImageBase64, input.MimeType, input.FileName);
        if (component.Image?.ImageHash != image.ImageHash)
        {
            component.SetImage(image);
            await _componentRepository.UpdateAsync(component, autoSave: true);
        }

        return ToMetadataDto(component.Image!);
    }

    [Authorize(VPureLuxPermissions.Catalog.Components.Edit)]
    public async Task RemoveImageAsync(Guid id)
    {
        var component = await GetComponentAsync(id);
        component.RemoveImage();
        await _componentRepository.UpdateAsync(component, autoSave: true);
    }

    private async Task<Component> GetComponentAsync(Guid id)
    {
        var component = await _componentRepository.FindAsync(id);
        if (component == null)
        {
            throw new BusinessException(VPureLuxDomainErrorCodes.ComponentNotFound)
                .WithData("Id", id);
        }

        return component;
    }

    private static BusinessException ImageNotFound(Guid id) =>
        new BusinessException(VPureLuxDomainErrorCodes.CatalogImageNotFound).WithData("Id", id);

    private static IQueryable<Component> ApplySorting(IQueryable<Component> queryable, string? sorting)
    {
        var normalizedSorting = NormalizeSorting(sorting);

        return normalizedSorting switch
        {
            "Code ASC" => queryable.OrderBy(x => x.Code),
            "Code DESC" => queryable.OrderByDescending(x => x.Code),
            "Name ASC" => queryable.OrderBy(x => x.Name),
            "Name DESC" => queryable.OrderByDescending(x => x.Name),
            "Unit ASC" => queryable.OrderBy(x => x.Unit),
            "Unit DESC" => queryable.OrderByDescending(x => x.Unit),
            "Status ASC" => queryable.OrderBy(x => x.Status),
            "Status DESC" => queryable.OrderByDescending(x => x.Status),
            "CreationTime ASC" => queryable.OrderBy(x => x.CreationTime),
            _ => queryable.OrderByDescending(x => x.CreationTime)
        };
    }

    private static string NormalizeSorting(string? sorting)
    {
        if (sorting.IsNullOrWhiteSpace())
        {
            return DefaultSorting;
        }

        var parts = sorting.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var field = parts[0].Split('.').Last();
        var direction = parts.Length > 1 && parts[1].Equals("desc", StringComparison.OrdinalIgnoreCase)
            ? "DESC"
            : "ASC";

        return field.ToLowerInvariant() switch
        {
            "code" => $"Code {direction}",
            "name" => $"Name {direction}",
            "unit" => $"Unit {direction}",
            "status" => $"Status {direction}",
            "creationtime" => $"CreationTime {direction}",
            _ => DefaultSorting
        };
    }

    private async Task<string> ResolveCodeAsync(string? code)
    {
        if (!code.IsNullOrWhiteSpace())
        {
            return code;
        }

        var businessDate = _clock.Now.Date;
        var datePart = businessDate.ToString("yyyyMMdd", CultureInfo.InvariantCulture);
        var codePrefix = $"{MaterialPrefix}-{datePart}";

        return await _businessCodeGenerator.GenerateAsync(new BusinessCodeGenerationContext
        {
            SequenceName = MaterialSequenceName,
            Prefix = MaterialPrefix,
            Date = businessDate,
            ExistsAsync = (candidate, cancellationToken) =>
                _componentRepository.CodeExistsAsync(candidate, cancellationToken: cancellationToken),
            SeedMaxAsync = async cancellationToken =>
                (int?)await _componentRepository.GetMaxCodeSequenceAsync(codePrefix, cancellationToken)
        });
    }

    private async Task EnsureReplacementPolicyPermissionAsync()
    {
        if (!(await AuthorizationService.AuthorizeAsync(VPureLuxPermissions.Warranty.ManagePolicies)).Succeeded)
        {
            throw new AbpAuthorizationException();
        }
    }

    private ComponentReplacementPolicy CreateReplacementPolicy(
        Guid componentId,
        ComponentReplacementPolicyInputDto input) =>
        new(
            GuidGenerator.Create(),
            componentId,
            input.CycleMonths,
            input.WarningDaysBeforeDue,
            input.Note,
            input.IsEnabled);

    private ComponentDto ToDto(Component component, ComponentReplacementPolicy? policy)
    {
        var dto = _mapper.ToDto(component);
        dto.IsReplacementTracked = policy?.IsEnabled == true;
        dto.ReplacementCycleMonths = policy?.CycleMonths;
        dto.WarningDaysBeforeDue = policy?.WarningDaysBeforeDue;
        dto.ReplacementPolicyNote = policy?.Note;
        return dto;
    }

    private static CatalogImageDto ToImageDto(
        ImageData image,
        byte[] content,
        string? mimeType = null,
        string? fileName = null) => new()
    {
        Content = content,
        MimeType = mimeType ?? image.MimeType,
        FileName = fileName ?? image.FileName,
        ImageHash = image.ImageHash
    };

    private static CatalogImageMetadataDto ToMetadataDto(ImageData image) => new()
    {
        MimeType = image.MimeType,
        FileName = image.FileName,
        ImageHash = image.ImageHash
    };
}
