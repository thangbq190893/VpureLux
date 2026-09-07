using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using VPureLux.Permissions;
using VPureLux.Reports;
using VPureLux.Service;

namespace VPureLux.Web.Pages.Reports;

[Authorize(VPureLuxPermissions.Reports.Consolidated.View)]
public class BusinessRevenueModel(IBusinessRevenueAppService reports, IAuthorizationService authorization, IOptions<ServiceOptions> options)
    : BusinessRevenuePageModel(reports, authorization, options)
{
    public override bool ServiceOnly => false;
}
