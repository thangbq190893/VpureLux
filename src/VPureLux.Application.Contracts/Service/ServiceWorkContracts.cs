using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace VPureLux.Service;

public interface IServiceWorkAppService : IApplicationService
{
    Task<PagedResultDto<ServiceWorkDto>> GetListAsync(GetServiceWorkListInput input);
    Task<ServiceWorkDto> GetAsync(Guid id);
    Task<ServiceWorkDto> CreateAsync(CreateUpdateServiceWorkDto input);
    Task<ServiceWorkDto> UpdateAsync(Guid id, CreateUpdateServiceWorkDto input);
}

public class GetServiceWorkListInput : PagedAndSortedResultRequestDto
{
    [StringLength(256)] public string? SearchText { get; set; }
    [EnumDataType(typeof(ServiceWorkStatus))] public ServiceWorkStatus? Status { get; set; }
}

public class CreateUpdateServiceWorkDto
{
    [Required, StringLength(ServiceConsts.MaxCodeLength)] public string Code { get; set; } = string.Empty;
    [Required, StringLength(ServiceConsts.MaxNameLength)] public string Name { get; set; } = string.Empty;
    [Required, StringLength(ServiceConsts.MaxUnitLength)] public string Unit { get; set; } = string.Empty;
    [Range(typeof(decimal), "0", "9999999999999999.99", ParseLimitsInInvariantCulture = true)] public decimal DefaultPrice { get; set; }
    [Range(typeof(decimal), "0", "9999999999999999.99", ParseLimitsInInvariantCulture = true)] public decimal? StandardCost { get; set; }
    [EnumDataType(typeof(ServiceWorkStatus))] public ServiceWorkStatus Status { get; set; } = ServiceWorkStatus.Active;
    [StringLength(ServiceConsts.MaxNoteLength)] public string? Note { get; set; }
    [StringLength(40)] public string? ConcurrencyStamp { get; set; }
}

public class ServiceWorkDto : EntityDto<Guid>
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Unit { get; set; }
    public decimal DefaultPrice { get; set; }
    public decimal? StandardCost { get; set; }
    public ServiceWorkStatus Status { get; set; }
    public string? Note { get; set; }
    public string ConcurrencyStamp { get; set; } = string.Empty;
}
