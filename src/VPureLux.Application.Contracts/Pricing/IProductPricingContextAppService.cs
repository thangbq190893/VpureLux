using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace VPureLux.Pricing;

public interface IProductPricingContextAppService : IApplicationService
{
    Task<List<ProductPricingContextDto>> GetListAsync();
}
