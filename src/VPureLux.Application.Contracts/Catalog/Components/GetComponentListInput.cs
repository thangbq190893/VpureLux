using Volo.Abp.Application.Dtos;
using VPureLux.Catalog;

namespace VPureLux.Catalog.Components;

public class GetComponentListInput : PagedAndSortedResultRequestDto
{
    public string? Keyword { get; set; }

    public CatalogItemStatus? Status { get; set; }
}
