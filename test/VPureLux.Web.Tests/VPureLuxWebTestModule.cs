using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Globalization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.AspNetCore.DataProtection;
using VPureLux.EntityFrameworkCore;
using VPureLux.Web;
using VPureLux.Web.Menus;
using Volo.Abp.AspNetCore.TestBase;
using Volo.Abp.Modularity;
using Volo.Abp.OpenIddict;
using Volo.Abp.UI.Navigation;
using Volo.Abp.Identity.Session;
using Medallion.Threading;
using Volo.Abp.AspNetCore.ExceptionHandling;

namespace VPureLux;

[DependsOn(
    typeof(AbpAspNetCoreTestBaseModule),
    typeof(VPureLuxWebModule),
    typeof(VPureLuxApplicationTestModule),
    typeof(VPureLuxEntityFrameworkCoreTestModule)
)]
public class VPureLuxWebTestModule : AbpModule
{
    public override void PreConfigureServices(ServiceConfigurationContext context)
    {
        var builder = new ConfigurationBuilder();
        builder.AddJsonFile("appsettings.json", false);
        builder.AddJsonFile("appsettings.secrets.json", true);
        context.Services.ReplaceConfiguration(builder.Build());

        context.Services.PreConfigure<IMvcBuilder>(builder =>
        {
            builder.PartManager.ApplicationParts.Add(new CompiledRazorAssemblyPart(typeof(VPureLuxWebModule).Assembly));
        });

        context.Services.GetPreConfigureActions<OpenIddictServerBuilder>().Clear();
        PreConfigure<AbpOpenIddictAspNetCoreOptions>(options =>
        {
            options.AddDevelopmentEncryptionAndSigningCertificate = true;
        });
    }

    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.AddTransient<Microsoft.AspNetCore.Hosting.IStartupFilter, VPureLux.Pages.ServiceTestAuthorizationFilter>();
        // Explicit opt-in for a local browser review of this SQLite/in-memory test host only.
        if (System.Environment.GetEnvironmentVariable("VPURELUX_SERVICE_UI_REVIEW") == "1")
        {
            Configure<VPureLux.Service.ServiceOptions>(options => options.IsEnabled = true);
        }
        context.Services.Replace(
            ServiceDescriptor.Singleton<IDistributedLockProvider, InMemoryDistributedLockProvider>());
        context.Services.RemoveAll<IDistributedCache>();
        context.Services.AddDistributedMemoryCache();
        context.Services.AddDataProtection().UseEphemeralDataProtectionProvider();
        context.Services.AddAuthentication()
            .AddScheme<AuthenticationSchemeOptions, WarrantyTestAuthenticationHandler>(
                WarrantyTestAuthenticationHandler.AuthenticationSchemeName,
                _ => { })
            .AddPolicyScheme("W008TestSelector", null, options =>
            {
                options.ForwardDefaultSelector = context =>
                    context.Request.Headers.ContainsKey("X-W008-Test-Auth")
                        ? WarrantyTestAuthenticationHandler.AuthenticationSchemeName
                        : IdentityConstants.ApplicationScheme;
            });
        PostConfigure<AuthenticationOptions>(options =>
        {
            options.DefaultAuthenticateScheme = "W008TestSelector";
        });

        Configure<IdentitySessionCleanupOptions>(options =>
        {
            options.IsCleanupEnabled = false;
        });
        Configure<AbpExceptionHandlingOptions>(options =>
        {
            options.SendExceptionsDetailsToClients = true;
        });

        ConfigureLocalizationServices(context.Services);
        ConfigureNavigationServices(context.Services);
    }

    private static void ConfigureLocalizationServices(IServiceCollection services)
    {
        var cultures = new List<CultureInfo> { new CultureInfo("en"), new CultureInfo("tr") };
        services.Configure<RequestLocalizationOptions>(options =>
        {
            options.DefaultRequestCulture = new RequestCulture("en");
            options.SupportedCultures = cultures;
            options.SupportedUICultures = cultures;
        });
    }

    private static void ConfigureNavigationServices(IServiceCollection services)
    {
        services.Configure<AbpNavigationOptions>(options =>
        {
            options.MenuContributors.Add(new VPureLuxMenuContributor());
        });
    }
}

public class WarrantyTestAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string AuthenticationSchemeName = "W008Test";

    public WarrantyTestAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.ContainsKey("X-W008-Test-Auth"))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var identity = new ClaimsIdentity(
            [new Claim(ClaimTypes.NameIdentifier, "2e701e62-0953-4dd3-910b-dc6cc93ccb0d"), new Claim(ClaimTypes.Name, "admin")],
            AuthenticationSchemeName);
        return Task.FromResult(AuthenticateResult.Success(
            new AuthenticationTicket(new ClaimsPrincipal(identity), AuthenticationSchemeName)));
    }
}
