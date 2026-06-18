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
        await CreateIfMissingAsync(CustomerGroupSeedIds.Retail, "RETAIL", "Retail", 10);
        await CreateIfMissingAsync(CustomerGroupSeedIds.Dealer, "DEALER", "Dealer", 20);
        await CreateIfMissingAsync(CustomerGroupSeedIds.Distributor, "DISTRIBUTOR", "Distributor", 30);
        await CreateIfMissingAsync(CustomerGroupSeedIds.Project, "PROJECT", "Project", 40);
    }

    private async Task CreateIfMissingAsync(Guid id, string code, string name, int sortOrder)
    {
        if (await _customerGroupRepository.FindByCodeAsync(code) != null)
        {
            return;
        }

        await _customerGroupRepository.InsertAsync(
            new CustomerGroup(id, code, name, description: null, sortOrder),
            autoSave: true);
    }
}
