using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using VPureLux.Permissions;
using Volo.Abp;
using Volo.Abp.Application.Services;

namespace VPureLux.Bom;

[Authorize(VPureLuxPermissions.Bom.View)]
public class BomAppService : ApplicationService, IBomAppService
{
    private readonly IBomVersionRepository _bomVersionRepository;
    private readonly BomManager _bomManager;
    private readonly BomCatalogValidator _catalogValidator;
    private readonly BomApplicationMapper _mapper;

    public BomAppService(
        IBomVersionRepository bomVersionRepository,
        BomManager bomManager,
        BomCatalogValidator catalogValidator,
        BomApplicationMapper mapper)
    {
        _bomVersionRepository = bomVersionRepository;
        _bomManager = bomManager;
        _catalogValidator = catalogValidator;
        _mapper = mapper;
    }

    public async Task<List<BomVersionDto>> GetListAsync(Guid productId)
    {
        await _catalogValidator.ValidateProductExistsAsync(productId);
        var versions = await _bomVersionRepository.GetListByProductIdAsync(productId);
        return versions.Select(_mapper.ToDto).ToList();
    }

    public async Task<BomVersionDto> GetAsync(Guid id)
    {
        return _mapper.ToDto(await GetBomVersionAsync(id));
    }

    [Authorize(VPureLuxPermissions.Bom.Create)]
    public async Task<BomVersionDto> CreateAsync(Guid productId, CreateBomVersionDto input)
    {
        await _catalogValidator.ValidateActiveProductAsync(productId);
        await _catalogValidator.ValidateActiveComponentsAsync(input.Items.Select(x => x.ComponentId));

        var bomVersion = await _bomManager.CreateAsync(productId, input.EffectiveFrom);
        foreach (var item in input.Items)
        {
            bomVersion.AddItem(GuidGenerator.Create(), item.ComponentId, item.Quantity);
        }

        await _bomVersionRepository.InsertAsync(bomVersion, autoSave: true);
        return _mapper.ToDto(bomVersion);
    }

    [Authorize(VPureLuxPermissions.Bom.Publish)]
    public async Task PublishAsync(Guid id)
    {
        var bomVersion = await GetBomVersionAsync(id);
        await _catalogValidator.ValidateActiveProductAsync(bomVersion.ProductId);
        await _catalogValidator.ValidateActiveComponentsAsync(bomVersion.Items.Select(x => x.ComponentId));

        await _bomManager.PublishAsync(bomVersion);
        await _bomVersionRepository.UpdateAsync(bomVersion, autoSave: true);
    }

    [Authorize(VPureLuxPermissions.Bom.Create)]
    public async Task<BomVersionDto> UpdateAsync(Guid id, UpdateBomVersionDto input)
    {
        var bomVersion = await GetBomVersionAsync(id);
        await _catalogValidator.ValidateActiveProductAsync(bomVersion.ProductId);
        await _catalogValidator.ValidateActiveComponentsAsync(input.Items.Select(x => x.ComponentId));

        var existingItems = bomVersion.Items.ToList();
        var inputItems = input.Items.ToList();
        var commonItemCount = Math.Min(existingItems.Count, inputItems.Count);

        for (var index = 0; index < commonItemCount; index++)
        {
            bomVersion.UpdateItem(
                existingItems[index].Id,
                inputItems[index].ComponentId,
                inputItems[index].Quantity);
        }

        foreach (var itemId in existingItems.Skip(commonItemCount).Select(x => x.Id).ToList())
        {
            bomVersion.RemoveItem(itemId);
        }

        foreach (var item in inputItems.Skip(commonItemCount))
        {
            bomVersion.AddItem(GuidGenerator.Create(), item.ComponentId, item.Quantity);
        }

        await _bomVersionRepository.UpdateAsync(bomVersion, autoSave: true);
        return _mapper.ToDto(bomVersion);
    }

    [Authorize(VPureLuxPermissions.Bom.Archive)]
    public async Task ArchiveAsync(Guid id)
    {
        var bomVersion = await GetBomVersionAsync(id);
        bomVersion.Archive(Clock.Now);
        await _bomVersionRepository.UpdateAsync(bomVersion, autoSave: true);
    }

    [Authorize(VPureLuxPermissions.Bom.Create)]
    public async Task<CloneBomVersionResultDto> CloneAsync(Guid id, CloneBomVersionDto input)
    {
        var source = await GetBomVersionAsync(id);
        await _catalogValidator.ValidateActiveProductAsync(source.ProductId);
        await _catalogValidator.ValidateActiveComponentsAsync(source.Items.Select(x => x.ComponentId));

        var clone = await _bomManager.CloneAsync(source, input.EffectiveFrom);
        await _bomVersionRepository.InsertAsync(clone, autoSave: true);

        return new CloneBomVersionResultDto
        {
            NewBomVersionId = clone.Id
        };
    }

    private async Task<BomVersion> GetBomVersionAsync(Guid id)
    {
        var bomVersion = await _bomVersionRepository.FindAsync(id, includeDetails: true);
        if (bomVersion == null)
        {
            throw new BusinessException(VPureLuxDomainErrorCodes.EntityNotFound)
                .WithData(nameof(id), id);
        }

        return bomVersion;
    }
}
