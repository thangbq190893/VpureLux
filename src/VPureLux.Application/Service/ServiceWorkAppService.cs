using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using VPureLux.Permissions;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Data;

namespace VPureLux.Service;

[Authorize(VPureLuxPermissions.Service.Default)]
[Authorize(VPureLuxPermissions.Service.View)]
public class ServiceWorkAppService : ApplicationService, IServiceWorkAppService
{
    private readonly IServiceWorkRepository _repository;
    private readonly IOptions<ServiceOptions> _options;

    public ServiceWorkAppService(IServiceWorkRepository repository, IOptions<ServiceOptions> options)
    {
        _repository = repository;
        _options = options;
    }

    public async Task<PagedResultDto<ServiceWorkDto>> GetListAsync(GetServiceWorkListInput input)
    {
        EnsureEnabled();
        var count = await _repository.GetCountAsync(input.SearchText, input.Status);
        var items = await _repository.GetListAsync(input.SearchText, input.Status, input.Sorting, input.SkipCount, input.MaxResultCount);
        return new PagedResultDto<ServiceWorkDto>(count, items.Select(Map).ToList());
    }

    public async Task<ServiceWorkDto> GetAsync(Guid id)
    {
        EnsureEnabled();
        return Map(await _repository.GetAsync(id));
    }

    [Authorize(VPureLuxPermissions.Service.ManageWorks)]
    public async Task<ServiceWorkDto> CreateAsync(CreateUpdateServiceWorkDto input)
    {
        EnsureEnabled();
        var entity = new ServiceWork(GuidGenerator.Create(), input.Code, input.Name, input.Unit,
            input.DefaultPrice, input.StandardCost, input.Status, input.Note);
        if (await _repository.CodeExistsAsync(entity.Code))
            throw new BusinessException(ServiceErrorCodes.WorkCodeAlreadyExists).WithData("Code", entity.Code);
        await _repository.InsertAsync(entity, autoSave: true);
        return Map(entity);
    }

    [Authorize(VPureLuxPermissions.Service.ManageWorks)]
    public async Task<ServiceWorkDto> UpdateAsync(Guid id, CreateUpdateServiceWorkDto input)
    {
        EnsureEnabled();
        var entity = await _repository.GetAsync(id);
        if (!string.Equals(entity.Code, input.Code.Trim(), StringComparison.OrdinalIgnoreCase))
            throw new BusinessException(ServiceErrorCodes.InvalidWork).WithData("Field", nameof(input.Code));
        if (string.IsNullOrWhiteSpace(input.ConcurrencyStamp) || input.ConcurrencyStamp != entity.ConcurrencyStamp)
            throw new AbpDbConcurrencyException();
        entity.Update(input.Name, input.Unit, input.DefaultPrice, input.StandardCost, input.Status, input.Note);
        await _repository.UpdateAsync(entity, autoSave: true);
        return Map(entity);
    }

    private void EnsureEnabled()
    {
        if (!_options.Value.IsEnabled) throw new BusinessException(ServiceErrorCodes.Disabled);
    }

    private static ServiceWorkDto Map(ServiceWork entity) => new()
    {
        Id = entity.Id, Code = entity.Code, Name = entity.Name, Unit = entity.Unit,
        DefaultPrice = entity.DefaultPrice, StandardCost = entity.StandardCost,
        Status = entity.Status, Note = entity.Note, ConcurrencyStamp = entity.ConcurrencyStamp
    };
}
