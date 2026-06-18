using Volo.Abp.Application.Dtos;

namespace VPureLux.Catalog.Components;

public class GetComponentListInput : PagedAndSortedResultRequestDto
{
    public string? Keyword { get; set; }
}
