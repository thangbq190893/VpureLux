using System.Threading;
using System.Threading.Tasks;

namespace VPureLux.BusinessCodes;

public interface IBusinessCodeGenerator
{
    Task<string> GenerateAsync(
        BusinessCodeGenerationContext context,
        CancellationToken cancellationToken = default);
}
