using System;
using System.Threading;
using System.Threading.Tasks;
using Volo.Abp.Domain.Repositories;

namespace VPureLux.Catalog;

public interface IProductRepository : IRepository<Product, Guid>
{
    Task<Product?> FindByCodeAsync(string code, CancellationToken cancellationToken = default);

    Task<bool> CodeExistsAsync(string code, Guid? excludedId = null, CancellationToken cancellationToken = default);
}
