using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Logging;
using VPureLux;
using Volo.Abp.AspNetCore.TestBase;

var builder = WebApplication.CreateBuilder();
builder.Logging.ClearProviders();
builder.Environment.EnvironmentName = "Development";
builder.Environment.ContentRootPath = GetWebProjectContentRootPathHelper.Get("VPureLux.Web.csproj"); 
await builder.RunAbpModuleAsync<VPureLuxWebTestModule>(applicationName: "VPureLux.Web");

public partial class Program
{
}
