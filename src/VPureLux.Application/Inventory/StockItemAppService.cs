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
public class StockItemAppService : ApplicationService, IStockItemAppService
{
    private readonly IStockItemRepository _repository;
    private readonly InventoryApplicationMapper _mapper;

    public StockItemAppService(IStockItemRepository repository, InventoryApplicationMapper mapper)
    {
        _repository = repository;
        _mapper = mapper;
    }

    public async Task<PagedResultDto<StockItemDto>> GetListAsync(GetInventoryListInput input)
    {
        var query = await _repository.GetQueryableAsync();
        if (!input.SearchText.IsNullOrWhiteSpace())
        {
            query = query.Where(x => x.CodeSnapshot.Contains(input.SearchText!) ||
                                     x.NameSnapshot.Contains(input.SearchText!));
        }
        if (input.Status.HasValue)
        {
            query = query.Where(x => x.Status == input.Status.Value);
        }

        var count = await AsyncExecuter.CountAsync(query);
        var items = await AsyncExecuter.ToListAsync(query.OrderBy(x => x.CodeSnapshot).PageBy(input));
        return new PagedResultDto<StockItemDto>(count, items.Select(_mapper.ToDto).ToList());
    }

    public async Task<StockItemDto> GetAsync(Guid id)
    {
        var item = await _repository.FindAsync(id)
            ?? throw new BusinessException(VPureLuxDomainErrorCodes.StockItemNotFound);
        return _mapper.ToDto(item);
    }
}
