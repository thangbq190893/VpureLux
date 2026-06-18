using System;
using System.IO;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace VPureLux.EntityFrameworkCore;

/* This class is needed for EF Core console commands
 * (like Add-Migration and Update-Database commands) */
public class VPureLuxDbContextFactory : IDesignTimeDbContextFactory<VPureLuxDbContext>
{
    public VPureLuxDbContext CreateDbContext(string[] args)
    {
        var configuration = BuildConfiguration();
        
        VPureLuxEfCoreEntityExtensionMappings.Configure();

        var builder = new DbContextOptionsBuilder<VPureLuxDbContext>()
            .UseSqlServer(configuration.GetConnectionString("Default"));
        
        return new VPureLuxDbContext(builder.Options);
    }

    private static IConfigurationRoot BuildConfiguration()
    {
        var builder = new ConfigurationBuilder()
            .SetBasePath(Path.Combine(Directory.GetCurrentDirectory(), "../VPureLux.DbMigrator/"))
            .AddJsonFile("appsettings.json", optional: false)
            .AddEnvironmentVariables();

        return builder.Build();
    }
}
