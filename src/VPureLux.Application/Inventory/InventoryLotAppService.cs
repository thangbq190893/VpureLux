using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using VPureLux.Permissions;
using VPureLux.Suppliers;
using Volo.Abp;
using Volo.Abp.Application.Services;

namespace VPureLux.Inventory;

[Authorize(VPureLuxPermissions.Inventory.Receive)]
public class InventoryLotAppService : ApplicationService, IInventoryLotAppService
{
    private readonly IInventoryLotRepository _lotRepository;
    private readonly IInventoryLotSupplierRepository _lotSupplierRepository;
    private readonly ISupplierRepository _supplierRepository;
    private readonly InventoryManager _manager;
    private readonly InventoryApplicationMapper _mapper;

    public InventoryLotAppService(
        IInventoryLotRepository lotRepository,
        IInventoryLotSupplierRepository lotSupplierRepository,
        ISupplierRepository supplierRepository,
        InventoryManager manager,
        InventoryApplicationMapper mapper)
    {
        _lotRepository = lotRepository;
        _lotSupplierRepository = lotSupplierRepository;
        _supplierRepository = supplierRepository;
        _manager = manager;
        _mapper = mapper;
    }

    public async Task<InventoryLotDto> UpdateSupplierAsync(Guid id, UpdateInventoryLotSupplierDto input)
    {
        var lot = await _lotRepository.FindAsync(id)
            ?? throw new BusinessException(VPureLuxDomainErrorCodes.InventoryLotNotFound)
                .WithData(nameof(id), id);
        var supplier = await _supplierRepository.FindAsync(input.SupplierId)
            ?? throw new BusinessException(VPureLuxDomainErrorCodes.SupplierNotFound)
                .WithData(nameof(input.SupplierId), input.SupplierId);

        var existing = (await _lotSupplierRepository.GetListByLotIdsAsync([id])).SingleOrDefault();
        if (existing == null)
        {
            await _lotSupplierRepository.InsertAsync(_manager.CreateLotSupplier(lot, supplier), autoSave: true);
        }
        else
        {
            existing.ChangeSupplier(supplier);
            await _lotSupplierRepository.UpdateAsync(existing, autoSave: true);
        }

        var dto = _mapper.ToDto(lot);
        dto.SupplierId = supplier.Id;
        dto.SupplierCode = supplier.Code;
        dto.SupplierName = supplier.Name;
        return dto;
    }
}
