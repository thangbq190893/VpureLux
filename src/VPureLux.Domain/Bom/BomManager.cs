using System;
using System.Threading.Tasks;
using Volo.Abp;
using Volo.Abp.Domain.Services;
using Volo.Abp.Guids;

namespace VPureLux.Bom;

public class BomManager : DomainService
{
    private readonly IBomVersionRepository _bomVersionRepository;
    private readonly IGuidGenerator _guidGenerator;

    public BomManager(IBomVersionRepository bomVersionRepository, IGuidGenerator guidGenerator)
    {
        _bomVersionRepository = bomVersionRepository;
        _guidGenerator = guidGenerator;
    }

    public async Task<BomVersion> CreateAsync(Guid productId, DateTime effectiveFrom)
    {
        var versionNo = await _bomVersionRepository.GetNextVersionNoAsync(productId);
        return new BomVersion(
            _guidGenerator.Create(),
            productId,
            new BomVersionNo(versionNo),
            effectiveFrom);
    }

    public async Task<BomVersion> CloneAsync(BomVersion source, DateTime effectiveFrom)
    {
        Check.NotNull(source, nameof(source));

        var versionNo = await _bomVersionRepository.GetNextVersionNoAsync(source.ProductId);
        return source.CloneVersion(
            _guidGenerator.Create(),
            new BomVersionNo(versionNo),
            effectiveFrom,
            _guidGenerator.Create);
    }

    public async Task PublishAsync(BomVersion bomVersion)
    {
        Check.NotNull(bomVersion, nameof(bomVersion));

        if (await _bomVersionRepository.HasPublishedVersionAsync(bomVersion.ProductId, bomVersion.Id))
        {
            throw new BusinessException(VPureLuxDomainErrorCodes.OnlyOneActiveBomAllowed)
                .WithData(nameof(bomVersion.ProductId), bomVersion.ProductId);
        }

        bomVersion.Publish();
    }
}
