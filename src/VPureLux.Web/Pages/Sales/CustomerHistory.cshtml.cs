using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using VPureLux.Catalog.Products;
using VPureLux.Customers;
using VPureLux.Permissions;
using VPureLux.Sales;
using Volo.Abp.Application.Dtos;

namespace VPureLux.Web.Pages.Sales;

[Authorize(VPureLuxPermissions.Sales.ViewCustomerHistory)]
[Authorize(VPureLuxPermissions.Sales.ViewProfit)]
public class CustomerHistoryModel : VPureLuxPageModel
{
    private readonly ISalesOrderAppService _service;
    private readonly IProductAppService _products;
    private readonly ICustomerAppService _customers;
    [BindProperty(SupportsGet = true)] public Guid? CustomerId { get; set; }
    public IReadOnlyList<CustomerPurchaseHistoryDto> Items { get; private set; } = Array.Empty<CustomerPurchaseHistoryDto>();
    public List<SelectListItem> Customers { get; private set; } = new();
    public Dictionary<Guid, string> ProductLabels { get; private set; } = new();

    public CustomerHistoryModel(
        ISalesOrderAppService service,
        IProductAppService products,
        ICustomerAppService customers)
    {
        _service = service;
        _products = products;
        _customers = customers;
    }

    public async Task OnGetAsync()
    {
        await LoadCustomerOptionsAsync();
        if (CustomerId.HasValue)
        {
            Items = await _service.GetCustomerHistoryAsync(CustomerId.Value);
            ProductLabels = (await _products.GetListAsync(new GetProductListInput { MaxResultCount = 1000 })).Items
                .Where(x => Items.Any(item => item.ProductId == x.Id))
                .ToDictionary(x => x.Id, x => $"{x.Code} - {x.Name}");
        }
    }

    public string GetProductLabel(CustomerPurchaseHistoryDto item)
    {
        return ProductLabels.TryGetValue(item.ProductId, out var product)
            ? product
            : L["Sales:ProductContextUnavailable"];
    }

    private async Task LoadCustomerOptionsAsync()
    {
        Customers = (await _customers.GetListAsync(new GetCustomerListInput
            {
                MaxResultCount = LimitedResultRequestDto.MaxMaxResultCount
            })).Items
            .OrderBy(x => x.Code)
            .Select(x => new SelectListItem($"{x.Code} - {x.Name}", x.Id.ToString()))
            .ToList();
    }
}
