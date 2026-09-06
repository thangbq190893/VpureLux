using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using VPureLux.EntityFrameworkCore.Warranty;

namespace VPureLux.Pages;

// TestServer does not forward the calling test's AsyncLocal context to HTTP requests.
public class ServiceTestAuthorizationFilter : IStartupFilter
{
    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) => app =>
    {
        app.Use(async (context, nextMiddleware) =>
        {
            if (context.Request.Headers.TryGetValue("X-S001-Deny", out var permission))
            {
                using (WarrantyMatrixAuthorizationService.Deny(permission.ToString())) await nextMiddleware();
            }
            else await nextMiddleware();
        });
        next(app);
    };
}
