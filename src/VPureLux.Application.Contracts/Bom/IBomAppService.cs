using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace VPureLux.Bom;

public interface IBomAppService : IApplicationService
{
    Task<List<BomVersionDto>> GetListAsync(Guid productId);

    Task<BomVersionDto> GetAsync(Guid id);

    Task<BomVersionDto> CreateAsync(Guid productId, CreateBomVersionDto input);

    Task<BomVersionDto> UpdateAsync(Guid id, UpdateBomVersionDto input);

    Task PublishAsync(Guid id);

    Task ArchiveAsync(Guid id);

    Task<CloneBomVersionResultDto> CloneAsync(Guid id, CloneBomVersionDto input);
}
