using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Serilog;
using System.Text.Json;
using VPureLux.Service;
using VPureLux.Warranty;
using VPureLux.Web;
using Volo.Abp;
using Volo.Abp.BackgroundJobs;
using Volo.Abp.BackgroundWorkers;
using Volo.Abp.Caching;
using Volo.Abp.FeatureManagement;
using Volo.Abp.Identity;
using Volo.Abp.Identity.Session;
using Volo.Abp.Modularity;
using Volo.Abp.PermissionManagement;
using Volo.Abp.TextTemplateManagement;
using Volo.Abp.Uow;
using Volo.Abp.Domain.Repositories;

var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,"../../../../../../../"));
var webRoot = Path.Combine(root,"src/VPureLux.Web");
var config = new ConfigurationBuilder().SetBasePath(webRoot).AddJsonFile("appsettings.json").Build();
var connection = new SqlConnectionStringBuilder(config.GetConnectionString("Default"));
if (connection.InitialCatalog != "VPL" || Environment.GetEnvironmentVariable("S006_ALLOW_FIXTURES") != "1") throw new InvalidOperationException("Explicit VPL fixture authorization required before startup");
connection.ApplicationName="UATSVC_20260907_RUNTIME";
await using (var sql = new SqlConnection(connection.ConnectionString))
{
    await sql.OpenAsync();
    await using var q=sql.CreateCommand(); q.CommandText="SELECT DB_NAME()";
    if ((string?)await q.ExecuteScalarAsync() != "VPL") throw new InvalidOperationException("STOP: target mismatch");
}
var builder=WebApplication.CreateBuilder(new WebApplicationOptions { ContentRootPath=webRoot,EnvironmentName="Development",ApplicationName=typeof(VPureLuxWebModule).Assembly.GetName().Name });
builder.Configuration.AddJsonFile("appsettings.secrets.json",optional:true).AddInMemoryCollection(new Dictionary<string,string?>
{
    ["ConnectionStrings:Default"]=connection.ConnectionString,
    ["Redis:Configuration"]="127.0.0.1:6379,defaultDatabase=13,abortConnect=true",
    ["App:SelfUrl"]="http://localhost:5196",["AuthServer:Authority"]="http://localhost:5196",
    ["AuthServer:RequireHttpsMetadata"]="false",["App:RedirectAllowedUrls"]="http://localhost:5196",
    ["Service:IsEnabled"]="false",["CustomerCare:IsEnabled"]="true",["CustomerCare:IsSalesIntakeEnabled"]="false"
});
builder.Host.UseAutofac().UseSerilog((_,logging)=>logging.MinimumLevel.Warning().MinimumLevel.Override("Volo.Abp.AspNetCore.Mvc.AntiForgery",Serilog.Events.LogEventLevel.Debug).WriteTo.Console());
await builder.AddApplicationAsync<S006RuntimeModule>();
var app=builder.Build();
await app.InitializeApplicationAsync();
var anti=app.Services.GetRequiredService<IOptions<Volo.Abp.AspNetCore.Mvc.AntiForgery.AbpAntiForgeryOptions>>().Value;
var actions=app.Services.GetRequiredService<Microsoft.AspNetCore.Mvc.Infrastructure.IActionDescriptorCollectionProvider>().ActionDescriptors.Items
    .OfType<Microsoft.AspNetCore.Mvc.Controllers.ControllerActionDescriptor>().Where(x=>x.ControllerName.StartsWith("Service"));
File.WriteAllText(Path.Combine(root,"artifacts/s006/antiforgery-runtime-options.json"),JsonSerializer.Serialize(new {
    anti.AutoValidate,anti.AuthCookieSchemaName,TokenCookie=anti.TokenCookie.Name,
    AuthCookie=app.Services.GetRequiredService<Volo.Abp.AspNetCore.Mvc.AntiForgery.AbpAntiForgeryCookieNameProvider>().GetAuthCookieNameOrNull(),
    Controllers=actions.Select(x=>new{x.ControllerName,x.ActionName,Selected=anti.AutoValidateFilter(x.ControllerTypeInfo.AsType()),Filters=x.FilterDescriptors.Select(f=>f.Filter.GetType().FullName).ToArray()}).ToArray()
},new JsonSerializerOptions{WriteIndented=true}));
var authPath=Path.Combine(root,"artifacts/s006/uat-auth.json");
if (!File.Exists(authPath))
{
    var id=Guid.NewGuid(); var roleId=Guid.NewGuid();
    const string name="UATSVC_20260907_OPERATOR";
    var password="S006@aA"+Guid.NewGuid().ToString("N");
    var control=Guid.NewGuid().ToString("N");
    File.WriteAllText(authPath,JsonSerializer.Serialize(new { UserId=id,RoleId=roleId,UserName=name,RoleName=name,Password=password,Control=control },new JsonSerializerOptions{WriteIndented=true}));
    using var scope=app.Services.CreateScope();
    using var uow=scope.ServiceProvider.GetRequiredService<IUnitOfWorkManager>().Begin(requiresNew:true,isTransactional:true);
    var users=scope.ServiceProvider.GetRequiredService<IdentityUserManager>();
    var roles=scope.ServiceProvider.GetRequiredService<IdentityRoleManager>();
    var role=new IdentityRole(roleId,name);
    var rr=await roles.CreateAsync(role);if(!rr.Succeeded)throw new Exception(string.Join(";",rr.Errors.Select(x=>x.Description)));
    var user=new IdentityUser(id,name,name+"@example.invalid");
    var ur=await users.CreateAsync(user,password);if(!ur.Succeeded)throw new Exception(string.Join(";",ur.Errors.Select(x=>x.Description)));
    var ar=await users.AddToRoleAsync(user,name);if(!ar.Succeeded)throw new Exception(string.Join(";",ar.Errors.Select(x=>x.Description)));
    var permissions=scope.ServiceProvider.GetRequiredService<Volo.Abp.Authorization.Permissions.IPermissionDefinitionManager>();
    var grants=scope.ServiceProvider.GetRequiredService<IPermissionDataSeeder>();
    await grants.SeedAsync("R",name,(await permissions.GetPermissionsAsync()).Where(x=>x.IsEnabled).Select(x=>x.Name));
    await uow.CompleteAsync();
}
var auth=JsonDocument.Parse(File.ReadAllText(authPath));
var secret=auth.RootElement.GetProperty("Control").GetString();
using(var scope=app.Services.CreateScope())
{
    var user=await scope.ServiceProvider.GetRequiredService<IdentityUserManager>().FindByIdAsync(auth.RootElement.GetProperty("UserId").GetGuid().ToString());
    if(user?.UserName!="UATSVC_20260907_OPERATOR")throw new Exception("Fixture identity bootstrap incomplete; do not silently reseed");
}
// Only this local evidence host has the control endpoint; no production assembly/config gains a bypass.
app.MapPost("/s006-control/service/{enabled:bool}",(bool enabled,HttpContext context)=>
{
    if(context.Request.Headers["X-S006-Control"]!=secret)return Results.Unauthorized();
    app.Services.GetRequiredService<IOptions<ServiceOptions>>().Value.IsEnabled=enabled;
    return Results.Ok(new { ServiceEnabled=enabled });
}).AllowAnonymous();
app.MapPost("/s006-control/permission",async (HttpContext context,PermissionControl input,IPermissionManager permissions,IUnitOfWorkManager units)=>
{
    if(context.Request.Headers["X-S006-Control"]!=secret)return Results.Unauthorized();
    if(!input.Name.StartsWith("VPureLux."))return Results.BadRequest();
    using var unit=units.Begin(requiresNew:true,isTransactional:true);
    await permissions.SetAsync(input.Name,"R","UATSVC_20260907_OPERATOR",input.Allowed);
    await unit.CompleteAsync();
    return Results.Ok();
}).AllowAnonymous();
app.MapPost("/s006-control/failure/{id:guid}",async (Guid id,HttpContext context,IRepository<ServiceOrder,Guid> orders,S006Controls controls)=>
{
    if(context.Request.Headers["X-S006-Control"]!=secret)return Results.Unauthorized();
    var order=await orders.GetAsync(id);
    if(order.Note?.StartsWith("UATSVC_20260907 ")!=true)return Results.BadRequest();
    controls.FailOrder=id;
    return Results.Ok();
}).AllowAnonymous();
Console.WriteLine("S006_RUNTIME_READY VPL http://localhost:5196 Redis=127.0.0.1:6379/13");
await app.RunAsync("http://127.0.0.1:5196");

[DependsOn(typeof(VPureLuxWebModule))]
public class S006RuntimeModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        Configure<AbpBackgroundWorkerOptions>(o=>o.IsEnabled=false);
        Configure<AbpBackgroundJobOptions>(o=>o.IsJobExecutionEnabled=false);
        Configure<IdentitySessionCleanupOptions>(o=>o.IsCleanupEnabled=false);
        Configure<PermissionManagementOptions>(o=>{o.SaveStaticPermissionsToDatabase=false;o.IsDynamicPermissionStoreEnabled=false;});
        Configure<FeatureManagementOptions>(o=>{o.SaveStaticFeaturesToDatabase=false;o.IsDynamicFeatureStoreEnabled=false;});
        Configure<TextTemplateManagementOptions>(o=>{o.SaveStaticTemplatesToDatabase=false;o.IsDynamicTemplateStoreEnabled=false;});
        Configure<AbpDistributedCacheOptions>(o=>o.KeyPrefix="UATSVC_20260907:");
        context.Services.AddSingleton<S006Controls>();
        context.Services.Replace(ServiceDescriptor.Transient<ICustomerCareServiceCompletion,S006Care>());
    }
}
public record PermissionControl(string Name,bool Allowed);
public class S006Controls { public Guid? FailOrder {get;set;} }
public class S006Care(CustomerCareServiceCompletion inner,S006Controls controls,IUnitOfWorkManager uow) : ICustomerCareServiceCompletion
{
    public async Task ApplyAsync(CustomerCareCompletion facts)
    {
        await inner.ApplyAsync(facts);
        if(controls.FailOrder==facts.ServiceOrderId)
        {
            await uow.Current!.SaveChangesAsync();
            throw new BusinessException("S006:InjectedPostStockFailure");
        }
    }
}
