using Volo.Abp.Application.Dtos;
using VPureLux.Catalog;

namespace VPureLux.Catalog.Products;

public class GetProductListInput : PagedAndSortedResultRequestDto
{
    public string? Keyword { get; set; }

    public CatalogItemStatus? Status { get; set; }
}
