using System;
using System.Threading.Tasks;
using Volo.Abp.Domain.Services;
using Volo.Abp.Guids;

namespace VPureLux.Sales;

public class SalesManager : DomainService
{
    private readonly ISalesOrderRepository _repository;
    private readonly IGuidGenerator _guidGenerator;

    public SalesManager(ISalesOrderRepository repository, IGuidGenerator guidGenerator)
    {
        _repository = repository;
        _guidGenerator = guidGenerator;
    }

    public async Task<SalesOrder> CreateAsync(Guid customerId, Guid warehouseId, DateTime orderDate)
    {
        var yearMonth = orderDate.ToString("yyyyMM");
        var sequence = await _repository.GetNextOrderSequenceAsync(yearMonth);
        if (sequence <= 0 || sequence > 999999)
        {
            throw new Volo.Abp.BusinessException(VPureLuxDomainErrorCodes.ValidationFailed)
                .WithData(nameof(sequence), sequence);
        }
        var orderNo = $"{SalesConsts.OrderNoPrefix}-{yearMonth}-{sequence.ToString().PadLeft(SalesConsts.OrderNoSequenceLength, '0')}";
        return new SalesOrder(_guidGenerator.Create(), orderNo, customerId, warehouseId, orderDate);
    }
}
