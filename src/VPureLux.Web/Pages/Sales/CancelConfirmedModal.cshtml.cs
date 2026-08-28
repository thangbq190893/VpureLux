using System;
using System.Globalization;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VPureLux.Permissions;
using VPureLux.Sales;

namespace VPureLux.Web.Pages.Sales;

[Authorize(VPureLuxPermissions.Sales.CancelConfirmedBeforeInstallation)]
public class CancelConfirmedModalModel : VPureLuxPageModel
{
    private static readonly CultureInfo Vi = CultureInfo.GetCultureInfo("vi-VN");
    private readonly ISalesOrderAppService _sales;
    private readonly ISalesPostConfirmationAppService _postConfirmation;

    [BindProperty(SupportsGet = true)] public Guid Id { get; set; }
    [BindProperty] public CancelConfirmedSalesOrderDto Input { get; set; } = new();
    public SalesPostConfirmationStateDto State { get; private set; } = new();
    public string OrderNo { get; private set; } = string.Empty;
    public string CustomerName { get; private set; } = string.Empty;

    public CancelConfirmedModalModel(
        ISalesOrderAppService sales,
        ISalesPostConfirmationAppService postConfirmation)
    {
        _sales = sales;
        _postConfirmation = postConfirmation;
    }

    public async Task OnGetAsync() => await LoadAsync();

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            await LoadAsync();
            return Page();
        }

        await _postConfirmation.CancelConfirmedAsync(Id, Input);
        return NoContent();
    }

    public string FormatMoney(decimal value) =>
        decimal.Round(value, 0, MidpointRounding.AwayFromZero).ToString("#,0", Vi) + " đ";

    private async Task LoadAsync()
    {
        var order = await _sales.GetAsync(Id);
        State = await _postConfirmation.GetOrderStateAsync(Id);
        OrderNo = order.OrderNo;
        CustomerName = $"{order.CustomerCodeSnapshot} - {order.CustomerNameSnapshot}".Trim(' ', '-');
    }
}
