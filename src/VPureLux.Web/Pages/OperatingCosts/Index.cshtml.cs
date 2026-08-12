using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using VPureLux.Localization;
using VPureLux.OperatingCosts;
using VPureLux.Permissions;
using Volo.Abp.Application.Dtos;

namespace VPureLux.Web.Pages.OperatingCosts;

[Authorize(VPureLuxPermissions.OperatingCosts.View)]
public class IndexModel : VPureLuxPageModel
{
    private static readonly CultureInfo Vi = CultureInfo.GetCultureInfo("vi-VN");

    private readonly IOperatingCostAppService _appService;
    private readonly IAuthorizationService _authorizationService;
    private readonly IStringLocalizer<VPureLuxResource> _localizer;

    [BindProperty(SupportsGet = true)] public DateTime? FromDate { get; set; }
    [BindProperty(SupportsGet = true)] public DateTime? ToDate { get; set; }
    [BindProperty(SupportsGet = true)] public OperatingCostDirection? Direction { get; set; }
    [BindProperty(SupportsGet = true)] public OperatingCostPaymentStatus? PaymentStatus { get; set; }
    [BindProperty(SupportsGet = true)] public Guid? CategoryId { get; set; }
    [BindProperty(SupportsGet = true)] public string? SearchText { get; set; }

    public bool CanManageEntries { get; private set; }
    public bool CanManageCategories { get; private set; }
    public bool CanDelete { get; private set; }
    public List<OperatingCostCategoryDto> Categories { get; private set; } = [];

    [TempData] public string? StatusMessageKey { get; set; }

    public IndexModel(
        IOperatingCostAppService appService,
        IAuthorizationService authorizationService,
        IStringLocalizer<VPureLuxResource> localizer)
    {
        _appService = appService;
        _authorizationService = authorizationService;
        _localizer = localizer;
    }

    public async Task OnGetAsync()
    {
        SetDefaultDateRange();
        await SetPermissionsAsync();
        Categories = await _appService.GetActiveCategoriesAsync();
    }

    public async Task<JsonResult> OnGetListAsync(GetOperatingCostEntryListInput input)
    {
        var result = await _appService.GetEntryListAsync(NormalizeInput(input));
        return new JsonResult(new PagedResultDto<OperatingCostEntryRow>(
            result.TotalCount,
            result.Items.Select(ToRow).ToList()));
    }

    public async Task<JsonResult> OnGetSummaryAsync(GetOperatingCostEntryListInput input)
    {
        var summary = await _appService.GetSummaryAsync(NormalizeInput(input));
        return new JsonResult(new
        {
            totalIncome = FormatMoney(summary.TotalIncome),
            totalExpense = FormatMoney(summary.TotalExpense),
            netAmount = FormatMoney(summary.NetAmount),
            unpaidReceivable = FormatMoney(summary.UnpaidReceivable),
            unpaidPayable = FormatMoney(summary.UnpaidPayable)
        });
    }

    public async Task<IActionResult> OnPostDeleteAsync(Guid id)
    {
        await _appService.DeleteEntryAsync(id);
        return new JsonResult(new { success = true });
    }

    private async Task SetPermissionsAsync()
    {
        CanManageEntries = (await _authorizationService.AuthorizeAsync(
            User,
            VPureLuxPermissions.OperatingCosts.ManageEntries)).Succeeded;
        CanManageCategories = (await _authorizationService.AuthorizeAsync(
            User,
            VPureLuxPermissions.OperatingCosts.ManageCategories)).Succeeded;
        CanDelete = (await _authorizationService.AuthorizeAsync(
            User,
            VPureLuxPermissions.OperatingCosts.Delete)).Succeeded;
    }

    private void SetDefaultDateRange()
    {
        if (FromDate.HasValue || ToDate.HasValue)
        {
            return;
        }

        var today = DateTime.Today;
        FromDate = new DateTime(today.Year, today.Month, 1);
        ToDate = FromDate.Value.AddMonths(1).AddDays(-1);
    }

    private static GetOperatingCostEntryListInput NormalizeInput(GetOperatingCostEntryListInput input)
    {
        return new GetOperatingCostEntryListInput
        {
            SearchText = input.SearchText,
            Direction = input.Direction,
            PaymentStatus = input.PaymentStatus,
            CategoryId = input.CategoryId,
            FromDate = input.FromDate,
            ToDate = input.ToDate,
            SkipCount = input.SkipCount,
            MaxResultCount = input.MaxResultCount,
            Sorting = input.Sorting
        };
    }

    private OperatingCostEntryRow ToRow(OperatingCostEntryDto entry)
    {
        return new OperatingCostEntryRow(
            entry.Id,
            entry.EntryDate.ToString("dd/MM/yyyy", Vi),
            _localizer[$"OperatingCosts:Direction:{entry.Direction}"].Value,
            $"{entry.CategoryCode} - {entry.CategoryName}",
            entry.Description,
            FormatMoney(entry.Amount),
            _localizer[$"OperatingCosts:PaymentStatus:{entry.PaymentStatus}"].Value,
            GetPaymentStatusBadgeClass(entry.PaymentStatus),
            entry.Counterparty ?? string.Empty);
    }

    private static string FormatMoney(decimal value)
    {
        var amount = decimal.Round(value, 0, MidpointRounding.AwayFromZero);
        return amount.ToString("#,0", Vi) + " ₫";
    }

    private static string GetPaymentStatusBadgeClass(OperatingCostPaymentStatus status) => status switch
    {
        OperatingCostPaymentStatus.Paid => "text-bg-success",
        _ => "text-bg-warning text-dark"
    };

    public sealed record OperatingCostEntryRow(
        Guid Id,
        string EntryDate,
        string Direction,
        string Category,
        string Description,
        string Amount,
        string PaymentStatus,
        string PaymentStatusBadgeClass,
        string Counterparty);
}
