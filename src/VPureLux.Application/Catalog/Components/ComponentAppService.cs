using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using VPureLux.BusinessCodes;
using VPureLux.Catalog.Components;
using VPureLux.Permissions;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Auditing;
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

    public ComponentAppService(
        IComponentRepository componentRepository,
        CatalogManager catalogManager,
        CatalogApplicationMapper mapper,
        ICatalogImageProcessor imageProcessor,
        IBusinessCodeGenerator businessCodeGenerator,
        IClock clock)
    {
        _componentRepository = componentRepository;
        _catalogManager = catalogManager;
        _mapper = mapper;
        _imageProcessor = imageProcessor;
        _businessCodeGenerator = businessCodeGenerator;
        _clock = clock;
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
        var items = await AsyncExecuter.ToListAsync(
            queryable
                .Skip(input.SkipCount)
                .Take(input.MaxResultCount)
                .Select(x => new ComponentDto
                {
                    Id = x.Id,
                    Code = x.Code,
                    Name = x.Name,
                    Description = x.Description,
                    Unit = x.Unit,
                    Status = x.Status,
                    CreationTime = x.CreationTime,
                    HasImage = x.Image!.ImageHash != null,
                    ImageHash = x.Image.ImageHash
                }));

        return new PagedResultDto<ComponentDto>(totalCount, items);
    }

    [Authorize(VPureLuxPermissions.Catalog.Components.View)]
    public async Task<ComponentDto> GetAsync(Guid id)
    {
        return _mapper.ToDto(await GetComponentAsync(id));
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

        return _mapper.ToDto(component);
    }

    [Authorize(VPureLuxPermissions.Catalog.Components.Edit)]
    public async Task<ComponentDto> UpdateAsync(Guid id, UpdateComponentDto input)
    {
        var component = await GetComponentAsync(id);

        await _catalogManager.UpdateComponentAsync(component, input.Name, input.Description, input.Unit);
        await _componentRepository.UpdateAsync(component, autoSave: true);

        return _mapper.ToDto(component);
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
