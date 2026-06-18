using Microsoft.AspNetCore.Authorization;
using VPureLux.Permissions;
namespace VPureLux.Web.Pages.Audit;
[Authorize(VPureLuxPermissions.Audit.View)]
public class ReportsModel : VPureLuxPageModel { public void OnGet() { } }
