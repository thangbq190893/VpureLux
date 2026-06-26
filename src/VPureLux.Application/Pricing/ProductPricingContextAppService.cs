using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using VPureLux.Permissions;
using Volo.Abp.Application.Services;

namespace VPureLux.Pricing;

[Authorize(VPureLuxPermissions.Pricing.View)]
public class ProductPricingContextAppService : ApplicationService, IProductPricingContextAppService
{
    private readonly IProductPricingContextLookupService _lookupService;

    public ProductPricingContextAppService(IProductPricingContextLookupService lookupService)
    {
        _lookupService = lookupService;
    }

    public async Task<List<ProductPricingContextDto>> GetListAsync()
    {
        return await _lookupService.GetListAsync(Clock.Now);
    }
}
