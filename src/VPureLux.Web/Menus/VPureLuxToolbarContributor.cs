using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using VPureLux.Permissions;
using VPureLux.Web.Components.Toolbar.WarrantyNotification;
using Volo.Abp.Authorization.Permissions;
using Volo.Abp.AspNetCore.Mvc.UI.Theme.Shared.Toolbars;
using Volo.Abp.Users;
using VPureLux.Web.Components.Toolbar.LoginLink;

namespace VPureLux.Web.Menus;

public class VPureLuxToolbarContributor : IToolbarContributor
{
    public virtual Task ConfigureToolbarAsync(IToolbarConfigurationContext context)
    {
        if (context.Toolbar.Name != StandardToolbars.Main)
        {
            return Task.CompletedTask;
        }

        if (context.ServiceProvider.GetRequiredService<ICurrentUser>().IsAuthenticated)
        {
            context.Toolbar.Items.Insert(
                0,
                new ToolbarItem(typeof(WarrantyNotificationViewComponent))
                    .RequirePermissions(VPureLuxPermissions.Warranty.View));
        }
        else
        {
            context.Toolbar.Items.Add(new ToolbarItem(typeof(LoginLinkViewComponent)));
        }
		
        return Task.CompletedTask;
    }
}
