using Volo.Abp.Application.Dtos;

namespace VPureLux.Catalog.Products;

public class GetProductListInput : PagedAndSortedResultRequestDto
{
    public string? Keyword { get; set; }
}
