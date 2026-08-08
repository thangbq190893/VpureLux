using System;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace VPureLux.Suppliers;

public interface ISupplierAppService : IApplicationService
{
    Task<PagedResultDto<SupplierDto>> GetListAsync(GetSupplierListInput input);

    Task<SupplierDto> GetAsync(Guid id);

    Task<SupplierDto> CreateAsync(CreateSupplierDto input);

    Task<SupplierDto> UpdateAsync(Guid id, UpdateSupplierDto input);

    Task DeleteAsync(Guid id);
}
