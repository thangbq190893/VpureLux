using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using VPureLux.Catalog.Components;
using VPureLux.Permissions;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Auditing;
using Volo.Abp.Domain.Repositories;

namespace VPureLux.Catalog.Components;

[Authorize(VPureLuxPermissions.Catalog.Components.Default)]
public class ComponentAppService : ApplicationService, IComponentAppService
{
    private readonly IComponentRepository _componentRepository;
    private readonly CatalogManager _catalogManager;
    private readonly CatalogApplicationMapper _mapper;
    private readonly ICatalogImageProcessor _imageProcessor;

    public ComponentAppService(
        IComponentRepository componentRepository,
        CatalogManager catalogManager,
        CatalogApplicationMapper mapper,
        ICatalogImageProcessor imageProcessor)
    {
        _componentRepository = componentRepository;
        _catalogManager = catalogManager;
        _mapper = mapper;
        _imageProcessor = imageProcessor;
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

        var totalCount = await AsyncExecuter.CountAsync(queryable);
        var items = await AsyncExecuter.ToListAsync(
            queryable
                .OrderBy(x => x.Code)
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
        var component = await _catalogManager.CreateComponentAsync(
            input.Code,
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
