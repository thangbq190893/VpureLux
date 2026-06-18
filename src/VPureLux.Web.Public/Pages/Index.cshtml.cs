using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;

namespace VPureLux.Web.Public.Pages;

public class IndexModel : VPureLuxPublicPageModel
{
    public void OnGet()
    {

    }

    public async Task OnPostLoginAsync()
    {
        await HttpContext.ChallengeAsync("oidc");
    }
}
