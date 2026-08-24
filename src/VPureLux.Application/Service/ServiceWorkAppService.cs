using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using VPureLux.Permissions;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace VPureLux.Service;

[Authorize(VPureLuxPermissions.Service.ManageWorks)]
public class ServiceWorkAppService : ApplicationService, IServiceWorkAppService
{
    private readonly IServiceWorkRepository _repository;

    public ServiceWorkAppService(IServiceWorkRepository repository)
    {
        _repository = repository;
    }

    public async Task<PagedResultDto<ServiceWorkDto>> GetListAsync(GetServiceWorkListInput input)
    {
        var query = await _repository.GetQueryableAsync();
        var search = input.SearchText?.Trim();
        query = query
            .Where(x => string.IsNullOrEmpty(search) || x.Code.Contains(search) || x.Name.Contains(search))
            .Where(x => !input.Status.HasValue || x.Status == input.Status.Value)
            .OrderBy(x => x.Code);
        var count = await AsyncExecuter.LongCountAsync(query);
        var items = await AsyncExecuter.ToListAsync(query.Skip(input.SkipCount).Take(input.MaxResultCount));
        return new PagedResultDto<ServiceWorkDto>(count, items.Select(Map).ToList());
    }

    public async Task<ServiceWorkDto> GetAsync(Guid id) => Map(await _repository.GetAsync(id));

    public async Task<ServiceWorkDto> CreateAsync(CreateUpdateServiceWorkDto input)
    {
        var code = input.Code.Trim().ToUpperInvariant();
        if (await _repository.CodeExistsAsync(code))
        {
            throw new BusinessException(VPureLuxDomainErrorCodes.ServiceWorkCodeAlreadyExists).WithData("Code", code);
        }
        var entity = new ServiceWork(GuidGenerator.Create(), code, input.Name, input.DefaultPrice, input.Note);
        await _repository.InsertAsync(entity, autoSave: true);
        return Map(entity);
    }

    public async Task<ServiceWorkDto> UpdateAsync(Guid id, CreateUpdateServiceWorkDto input)
    {
        var entity = await _repository.GetAsync(id);
        if (!string.Equals(entity.Code, input.Code.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            throw new BusinessException(VPureLuxDomainErrorCodes.ValidationFailed);
        }
        entity.Update(input.Name, input.DefaultPrice, input.Note);
        await _repository.UpdateAsync(entity, autoSave: true);
        return Map(entity);
    }

    public async Task SetActiveAsync(Guid id, bool isActive)
    {
        var entity = await _repository.GetAsync(id);
        if (isActive) entity.Activate(); else entity.Deactivate();
        await _repository.UpdateAsync(entity, autoSave: true);
    }

    private static ServiceWorkDto Map(ServiceWork entity) => new()
    {
        Id = entity.Id,
        Code = entity.Code,
        Name = entity.Name,
        DefaultPrice = entity.DefaultPrice,
        Status = entity.Status,
        Note = entity.Note
    };
}
