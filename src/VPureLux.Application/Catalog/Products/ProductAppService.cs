using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using VPureLux.Catalog.Products;
using VPureLux.Permissions;
using VPureLux.Warranty;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Auditing;
using Volo.Abp.Authorization;

namespace VPureLux.Catalog.Products;

[Authorize(VPureLuxPermissions.Catalog.Products.Default)]
public class ProductAppService : ApplicationService, IProductAppService
{
    private const string DefaultSorting = "CreationTime DESC";

    private readonly IProductRepository _productRepository;
    private readonly CatalogManager _catalogManager;
    private readonly CatalogApplicationMapper _mapper;
    private readonly ICatalogImageProcessor _imageProcessor;
    private readonly IProductMachineSettingRepository _machineSettings;

    public ProductAppService(
        IProductRepository productRepository,
        CatalogManager catalogManager,
        CatalogApplicationMapper mapper,
        ICatalogImageProcessor imageProcessor,
        IProductMachineSettingRepository machineSettings)
    {
        _productRepository = productRepository;
        _catalogManager = catalogManager;
        _mapper = mapper;
        _imageProcessor = imageProcessor;
        _machineSettings = machineSettings;
    }

    [Authorize(VPureLuxPermissions.Catalog.Products.View)]
    public async Task<PagedResultDto<ProductDto>> GetListAsync(GetProductListInput input)
    {
        var queryable = await _productRepository.GetQueryableAsync();

        if (!input.Keyword.IsNullOrWhiteSpace())
        {
            queryable = queryable.Where(x =>
                x.Code.Contains(input.Keyword!) ||
                x.Name.Contains(input.Keyword!));
        }

        if (input.Status.HasValue)
        {
            queryable = queryable.Where(x => x.Status == input.Status.Value);
        }

        queryable = ApplySorting(queryable, input.Sorting);

        var totalCount = await AsyncExecuter.CountAsync(queryable);
        var machineSettings = await _machineSettings.GetQueryableAsync();
        var pageQuery = queryable
            .Skip(input.SkipCount)
            .Take(input.MaxResultCount);
        var items = await AsyncExecuter.ToListAsync(
            from product in pageQuery
            join setting in machineSettings on product.Id equals setting.ProductId into productSettings
            from setting in productSettings.DefaultIfEmpty()
            select new ProductDto
                {
                    Id = product.Id,
                    Code = product.Code,
                    Name = product.Name,
                    Description = product.Description,
                    Status = product.Status,
                    IsMachine = setting != null && setting.IsMachine,
                    CreationTime = product.CreationTime,
                    HasImage = product.Image!.ImageHash != null,
                    ImageHash = product.Image.ImageHash
                });

        return new PagedResultDto<ProductDto>(totalCount, items);
    }

    [Authorize(VPureLuxPermissions.Catalog.Products.View)]
    public async Task<ProductDto> GetAsync(Guid id)
    {
        var product = await GetProductAsync(id);
        return ToDto(product, await _machineSettings.FindByProductIdAsync(id));
    }

    [Authorize(VPureLuxPermissions.Catalog.Products.Create)]
    public async Task<ProductDto> CreateAsync(CreateProductDto input)
    {
        var code = ResolveManualCode(input.Code);
        var product = await _catalogManager.CreateProductAsync(
            code,
            input.Name,
            input.Description);

        await _productRepository.InsertAsync(product, autoSave: true);

        ProductMachineSetting? setting = null;
        if (input.MachineSetting != null)
        {
            await EnsureMachineSettingPermissionAsync();
            if (input.MachineSetting.IsMachine)
            {
                setting = new ProductMachineSetting(GuidGenerator.Create(), product.Id, true);
                await _machineSettings.InsertAsync(setting, autoSave: true);
            }
        }

        return ToDto(product, setting);
    }

    [Authorize(VPureLuxPermissions.Catalog.Products.Edit)]
    public async Task<ProductDto> UpdateAsync(Guid id, UpdateProductDto input)
    {
        var product = await GetProductAsync(id);

        var code = ResolveManualCode(input.Code);
        await _catalogManager.UpdateProductAsync(product, code, input.Name, input.Description);
        await _productRepository.UpdateAsync(product, autoSave: true);

        var setting = await _machineSettings.FindByProductIdAsync(id);
        if (input.MachineSetting != null)
        {
            await EnsureMachineSettingPermissionAsync();
            if (setting == null && input.MachineSetting.IsMachine)
            {
                setting = new ProductMachineSetting(GuidGenerator.Create(), product.Id, true);
                await _machineSettings.InsertAsync(setting, autoSave: true);
            }
            else if (setting != null)
            {
                setting.Update(input.MachineSetting.IsMachine, setting.Note);
                await _machineSettings.UpdateAsync(setting, autoSave: true);
            }
        }

        return ToDto(product, setting);
    }

    [Authorize(VPureLuxPermissions.Catalog.Products.Edit)]
    public async Task ActivateAsync(Guid id)
    {
        var product = await GetProductAsync(id);
        product.Activate();
        await _productRepository.UpdateAsync(product, autoSave: true);
    }

    [Authorize(VPureLuxPermissions.Catalog.Products.Edit)]
    public async Task DeactivateAsync(Guid id)
    {
        var product = await GetProductAsync(id);
        product.Deactivate();
        await _productRepository.UpdateAsync(product, autoSave: true);
    }

    [Authorize(VPureLuxPermissions.Catalog.Products.View)]
    [DisableAuditing]
    public async Task<CatalogImageDto> GetImageAsync(Guid id)
    {
        var image = (await GetProductAsync(id)).Image;
        if (image == null)
        {
            throw ImageNotFound(id);
        }

        return ToImageDto(image, Convert.FromBase64String(image.ImageBase64));
    }

    [Authorize(VPureLuxPermissions.Catalog.Products.View)]
    [DisableAuditing]
    public async Task<CatalogImageDto> GetThumbnailAsync(Guid id)
    {
        var image = (await GetProductAsync(id)).Image;
        if (image == null)
        {
            throw ImageNotFound(id);
        }

        return ToImageDto(image, _imageProcessor.CreateThumbnail(image), "image/webp", "thumbnail.webp");
    }

    [Authorize(VPureLuxPermissions.Catalog.Products.Edit)]
    [DisableAuditing]
    public async Task<CatalogImageMetadataDto> SetImageAsync(Guid id, CatalogImageUploadDto input)
    {
        var product = await GetProductAsync(id);
        var image = _imageProcessor.Process(input.ImageBase64, input.MimeType, input.FileName);
        if (product.Image?.ImageHash != image.ImageHash)
        {
            product.SetImage(image);
            await _productRepository.UpdateAsync(product, autoSave: true);
        }

        return ToMetadataDto(product.Image!);
    }

    [Authorize(VPureLuxPermissions.Catalog.Products.Edit)]
    public async Task RemoveImageAsync(Guid id)
    {
        var product = await GetProductAsync(id);
        product.RemoveImage();
        await _productRepository.UpdateAsync(product, autoSave: true);
    }

    private async Task<Product> GetProductAsync(Guid id)
    {
        var product = await _productRepository.FindAsync(id);
        if (product == null)
        {
            throw new BusinessException(VPureLuxDomainErrorCodes.ProductNotFound)
                .WithData("Id", id);
        }

        return product;
    }

    private static BusinessException ImageNotFound(Guid id) =>
        new BusinessException(VPureLuxDomainErrorCodes.CatalogImageNotFound).WithData("Id", id);

    private static IQueryable<Product> ApplySorting(IQueryable<Product> queryable, string? sorting)
    {
        var normalizedSorting = NormalizeSorting(sorting);

        return normalizedSorting switch
        {
            "Code ASC" => queryable.OrderBy(x => x.Code),
            "Code DESC" => queryable.OrderByDescending(x => x.Code),
            "Name ASC" => queryable.OrderBy(x => x.Name),
            "Name DESC" => queryable.OrderByDescending(x => x.Name),
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
            "status" => $"Status {direction}",
            "creationtime" => $"CreationTime {direction}",
            _ => DefaultSorting
        };
    }

    private static string ResolveManualCode(string? code)
    {
        var trimmedCode = code?.Trim();

        if (!trimmedCode.IsNullOrWhiteSpace())
        {
            return trimmedCode;
        }

        throw new BusinessException(VPureLuxDomainErrorCodes.ProductCodeRequired);
    }

    private async Task EnsureMachineSettingPermissionAsync()
    {
        if (!(await AuthorizationService.AuthorizeAsync(VPureLuxPermissions.Warranty.ManageMachines)).Succeeded)
        {
            throw new AbpAuthorizationException();
        }
    }

    private ProductDto ToDto(Product product, ProductMachineSetting? setting)
    {
        var dto = _mapper.ToDto(product);
        dto.IsMachine = setting?.IsMachine == true;
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
