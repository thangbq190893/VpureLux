using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using VPureLux.Permissions;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace VPureLux.Inventory;

[Authorize(VPureLuxPermissions.Inventory.View)]
public class WarehouseAppService : ApplicationService, IWarehouseAppService
{
    private readonly IWarehouseRepository _repository;
    private readonly InventoryManager _manager;
    private readonly InventoryApplicationMapper _mapper;

    public WarehouseAppService(
        IWarehouseRepository repository,
        InventoryManager manager,
        InventoryApplicationMapper mapper)
    {
        _repository = repository;
        _manager = manager;
        _mapper = mapper;
    }

    public async Task<PagedResultDto<WarehouseDto>> GetListAsync(GetInventoryListInput input)
    {
        var query = await _repository.GetQueryableAsync();
        if (!input.SearchText.IsNullOrWhiteSpace())
        {
            query = query.Where(x => x.Code.Contains(input.SearchText!) || x.Name.Contains(input.SearchText!));
        }
        if (input.Status.HasValue)
        {
            query = query.Where(x => x.Status == input.Status.Value);
        }

        var count = await AsyncExecuter.CountAsync(query);
        var items = await AsyncExecuter.ToListAsync(query.OrderBy(x => x.Code).PageBy(input));
        return new PagedResultDto<WarehouseDto>(count, items.Select(_mapper.ToDto).ToList());
    }

    public async Task<WarehouseDto> GetAsync(Guid id) => _mapper.ToDto(await GetWarehouseAsync(id));

    [Authorize(VPureLuxPermissions.Inventory.ManageWarehouses)]
    public async Task<WarehouseDto> CreateAsync(CreateWarehouseDto input)
    {
        var warehouse = await _manager.CreateWarehouseAsync(input.Code, input.Name, input.Address, input.IsDefault);
        await _repository.InsertAsync(warehouse, autoSave: true);
        return _mapper.ToDto(warehouse);
    }

    [Authorize(VPureLuxPermissions.Inventory.ManageWarehouses)]
    public async Task<WarehouseDto> UpdateAsync(Guid id, UpdateWarehouseDto input)
    {
        var warehouse = await GetWarehouseAsync(id);
        warehouse.UpdateInfo(input.Name, input.Address, input.IsDefault);
        await _repository.UpdateAsync(warehouse, autoSave: true);
        return _mapper.ToDto(warehouse);
    }

    [Authorize(VPureLuxPermissions.Inventory.ManageWarehouses)]
    public async Task ActivateAsync(Guid id)
    {
        var warehouse = await GetWarehouseAsync(id);
        warehouse.Activate();
        await _repository.UpdateAsync(warehouse, autoSave: true);
    }

    [Authorize(VPureLuxPermissions.Inventory.ManageWarehouses)]
    public async Task DeactivateAsync(Guid id)
    {
        var warehouse = await GetWarehouseAsync(id);
        warehouse.Deactivate();
        await _repository.UpdateAsync(warehouse, autoSave: true);
    }

    private async Task<Warehouse> GetWarehouseAsync(Guid id) =>
        await _repository.FindAsync(id)
        ?? throw new BusinessException(VPureLuxDomainErrorCodes.WarehouseNotFound).WithData(nameof(id), id);
}
