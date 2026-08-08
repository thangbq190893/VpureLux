using System.Threading.Tasks;
using Volo.Abp;
using Volo.Abp.Domain.Services;
using Volo.Abp.Guids;

namespace VPureLux.Suppliers;

public class SupplierManager : DomainService
{
    private readonly ISupplierRepository _supplierRepository;
    private readonly IGuidGenerator _guidGenerator;

    public SupplierManager(ISupplierRepository supplierRepository, IGuidGenerator guidGenerator)
    {
        _supplierRepository = supplierRepository;
        _guidGenerator = guidGenerator;
    }

    public async Task<Supplier> CreateAsync(
        string code,
        string name,
        string? contactName,
        string? phone,
        string? email,
        string? taxCode,
        string? address,
        string? note)
    {
        code = Supplier.NormalizeCode(code);
        if (await _supplierRepository.CodeExistsAsync(code))
        {
            throw new BusinessException(VPureLuxDomainErrorCodes.SupplierCodeAlreadyExists);
        }

        return new Supplier(_guidGenerator.Create(), code, name, contactName, phone, email, taxCode, address, note);
    }
}
