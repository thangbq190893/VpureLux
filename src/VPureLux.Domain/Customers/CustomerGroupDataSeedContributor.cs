using System;
using System.Threading.Tasks;
using Volo.Abp.Data;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Uow;

namespace VPureLux.Customers;

public class CustomerGroupDataSeedContributor : IDataSeedContributor, ITransientDependency
{
    private readonly ICustomerGroupRepository _customerGroupRepository;

    public CustomerGroupDataSeedContributor(ICustomerGroupRepository customerGroupRepository)
    {
        _customerGroupRepository = customerGroupRepository;
    }

    [UnitOfWork]
    public virtual async Task SeedAsync(DataSeedContext context)
    {
        await CreateOrNormalizeDefaultAsync(CustomerGroupSeedIds.Retail, "RETAIL", "Khách lẻ", "Retail", 10);
        await CreateOrNormalizeDefaultAsync(CustomerGroupSeedIds.Dealer, "DEALER", "Đại lý", "Dealer", 20);
        await CreateOrNormalizeDefaultAsync(CustomerGroupSeedIds.Distributor, "DISTRIBUTOR", "Nhà phân phối", "Distributor", 30);
        await CreateOrNormalizeDefaultAsync(CustomerGroupSeedIds.Project, "PROJECT", "Khách dự án", "Project", 40);
    }

    private async Task CreateOrNormalizeDefaultAsync(
        Guid id,
        string code,
        string vietnameseName,
        string legacyName,
        int sortOrder)
    {
        var existing = await _customerGroupRepository.FindByCodeAsync(code);
        if (existing != null)
        {
            if (existing.Name == legacyName)
            {
                existing.UpdateInfo(vietnameseName, existing.Description, existing.SortOrder);
                await _customerGroupRepository.UpdateAsync(existing, autoSave: true);
            }

            return;
        }

        await _customerGroupRepository.InsertAsync(
            new CustomerGroup(id, code, vietnameseName, description: null, sortOrder),
            autoSave: true);
    }
}
