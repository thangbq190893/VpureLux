using System.Threading.Tasks;

namespace VPureLux.Data;

public interface IVPureLuxDbSchemaMigrator
{
    Task MigrateAsync();
}
