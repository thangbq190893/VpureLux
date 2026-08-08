using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using VPureLux.Permissions;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace VPureLux.Suppliers;

[Authorize(VPureLuxPermissions.Suppliers.Default)]
public class SupplierAppService : ApplicationService, ISupplierAppService
{
    private readonly ISupplierRepository _supplierRepository;
    private readonly SupplierManager _supplierManager;

    public SupplierAppService(ISupplierRepository supplierRepository, SupplierManager supplierManager)
    {
        _supplierRepository = supplierRepository;
        _supplierManager = supplierManager;
    }

    [Authorize(VPureLuxPermissions.Suppliers.View)]
    public async Task<PagedResultDto<SupplierDto>> GetListAsync(GetSupplierListInput input)
    {
        var totalCount = await _supplierRepository.GetCountAsync(input.SearchText);
        var suppliers = await _supplierRepository.GetListAsync(
            input.SearchText,
            input.Sorting,
            input.MaxResultCount,
            input.SkipCount);

        return new PagedResultDto<SupplierDto>(
            totalCount,
            suppliers.Select(ToDto).ToList());
    }

    [Authorize(VPureLuxPermissions.Suppliers.View)]
    public async Task<SupplierDto> GetAsync(Guid id)
    {
        return ToDto(await GetSupplierAsync(id));
    }

    [Authorize(VPureLuxPermissions.Suppliers.Create)]
    public async Task<SupplierDto> CreateAsync(CreateSupplierDto input)
    {
        var supplier = await _supplierManager.CreateAsync(
            input.Code,
            input.Name,
            input.ContactName,
            input.Phone,
            input.Email,
            input.TaxCode,
            input.Address,
            input.Note);

        await _supplierRepository.InsertAsync(supplier, autoSave: true);
        return ToDto(supplier);
    }

    [Authorize(VPureLuxPermissions.Suppliers.Edit)]
    public async Task<SupplierDto> UpdateAsync(Guid id, UpdateSupplierDto input)
    {
        var supplier = await GetSupplierAsync(id);
        supplier.UpdateInfo(
            input.Name,
            input.ContactName,
            input.Phone,
            input.Email,
            input.TaxCode,
            input.Address,
            input.Note);

        await _supplierRepository.UpdateAsync(supplier, autoSave: true);
        return ToDto(supplier);
    }

    [Authorize(VPureLuxPermissions.Suppliers.Delete)]
    public async Task DeleteAsync(Guid id)
    {
        var supplier = await GetSupplierAsync(id);
        await _supplierRepository.DeleteAsync(supplier, autoSave: true);
    }

    private async Task<Supplier> GetSupplierAsync(Guid id)
    {
        var supplier = await _supplierRepository.FindAsync(id);
        if (supplier == null)
        {
            throw new BusinessException(VPureLuxDomainErrorCodes.SupplierNotFound)
                .WithData(nameof(id), id);
        }

        return supplier;
    }

    private static SupplierDto ToDto(Supplier supplier) => new()
    {
        Id = supplier.Id,
        Code = supplier.Code,
        Name = supplier.Name,
        ContactName = supplier.ContactName,
        Phone = supplier.Phone,
        Email = supplier.Email,
        TaxCode = supplier.TaxCode,
        Address = supplier.Address,
        Note = supplier.Note,
        CreationTime = supplier.CreationTime
    };
}
