using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using HtmlAgilityPack;
using Microsoft.Extensions.Options;
using Shouldly;
using VPureLux.Permissions;
using VPureLux.Sales;
using VPureLux.Service;
using VPureLux.Web.Pages.Service;
using Xunit;

namespace VPureLux.Pages;

public partial class ServiceOrderWebTests
{
    private IServicePaymentAppService ServiceMoney => GetRequiredService<IServicePaymentAppService>();
    private async Task<ServiceOrderDto> CreateMoneyOrderAsync()
    {
        Enable();
        var f = await CreateFixtureAsync("WEB-MONEY", "Work");
        return await GetRequiredService<IServiceOrderAppService>().CreateAsync(new()
        {
            CustomerAssetId = f.AssetId, WarehouseId = f.WarehouseId,
            Lines = [new() { LineType = ServiceOrderLineType.Labor, CatalogItemId = f.WorkId, Quantity = 1, UnitPrice = 5000000 }]
        });
    }

    private static Dictionary<string, string> MoneyPost(Guid id, bool refund, string amount) => new()
    {
        ["Id"] = id.ToString(), ["Refund"] = refund.ToString(), ["Input.Amount"] = amount,
        ["Input.OccurredAt"] = "2026-09-07T01:30", ["Input.Method"] = "1",
        ["Input.IdempotencyKey"] = Guid.NewGuid().ToString("N"),
        ["Input.ReferenceNo"] = "<img src=x onerror=alert(1)>", ["Input.Note"] = "<script>unsafe</script>"
    };

    private async Task<HttpResponseMessage> SubmitMoneyModal(string url, Dictionary<string, string> input, string? denied = null)
    {
        var get = await Client.GetAsync(url);
        get.StatusCode.ShouldBe(HttpStatusCode.OK, await get.Content.ReadAsStringAsync());
        AddAntiforgery(get, input);
        using var post = new HttpRequestMessage(HttpMethod.Post, url) { Content = new FormUrlEncodedContent(input) };
        AddCookies(get, post);
        if (denied != null) post.Headers.Add("X-S001-Deny", denied);
        return await Client.SendAsync(post);
    }

    [Fact]
    public async Task Money_payment_and_refund_modal_bind_vi_money_and_enforce_csrf_permissions()
    {
        var order = await CreateMoneyOrderAsync();
        var url = $"/Service/MoneyModal?Id={order.Id}";
        var form = MoneyPost(order.Id, false, "1.500.000,50");
        using (var missing = await Client.PostAsync(url, new FormUrlEncodedContent(form)))
        {
            missing.StatusCode.ShouldBe(HttpStatusCode.Found);
            missing.Headers.Location!.ToString().ShouldBe("/Error?httpStatusCode=400");
        }
        using (var denied = new HttpRequestMessage(HttpMethod.Get, url))
        {
            denied.Headers.Add("X-S001-Deny", VPureLuxPermissions.Service.ManagePayments);
            (await Client.SendAsync(denied)).IsSuccessStatusCode.ShouldBeFalse();
        }
        var deniedPost = await SubmitMoneyModal(url, form, VPureLuxPermissions.Service.ManagePayments);
        deniedPost.IsSuccessStatusCode.ShouldBeFalse();
        (await ServiceMoney.GetSummaryAsync(order.Id)).NetPaid.ShouldBe(0);
        var result = await SubmitMoneyModal(url, form);
        result.StatusCode.ShouldBe(HttpStatusCode.NoContent, await result.Content.ReadAsStringAsync());
        var payment = (await ServiceMoney.GetListAsync(new() { ServiceOrderId = order.Id })).Items.Single();
        payment.Amount.ShouldBe(1500000.50m);
        payment.PaymentDate.ShouldBe(new DateTime(2026, 9, 6, 18, 30, 0));
        (await ServiceMoney.GetSummaryAsync(order.Id)).Revenue.ShouldBe(0);
        var invalidForm = MoneyPost(order.Id, false, "-1");
        (await SubmitMoneyModal(url, invalidForm)).StatusCode.ShouldBe(HttpStatusCode.OK);
        (await ServiceMoney.GetListAsync(new() { ServiceOrderId = order.Id })).TotalCount.ShouldBe(1);

        await GetRequiredService<IServiceOrderAppService>().CancelAsync(order.Id,
            new() { ConcurrencyStamp = order.ConcurrencyStamp, Reason = "cancel" });
        var refundUrl = url + "&Refund=true";
        var refund = MoneyPost(order.Id, true, "500.000,50");
        using (var missing = await Client.PostAsync(refundUrl, new FormUrlEncodedContent(refund)))
            missing.Headers.Location!.ToString().ShouldBe("/Error?httpStatusCode=400");
        (await SubmitMoneyModal(refundUrl, refund, VPureLuxPermissions.Service.ManagePayments)).IsSuccessStatusCode.ShouldBeFalse();
        var refunded = await SubmitMoneyModal(refundUrl, refund);
        refunded.StatusCode.ShouldBe(HttpStatusCode.NoContent, await refunded.Content.ReadAsStringAsync());
        (await ServiceMoney.GetSummaryAsync(order.Id)).RefundDue.ShouldBe(1000000);
        var detail = await Client.GetStringAsync($"/Service/Details/{order.Id}");
        detail.ShouldContain("AddServiceRefund"); detail.ShouldNotContain("id=\"AddServicePayment\"");
        GetRequiredService<IOptions<ServiceOptions>>().Value.IsEnabled = false;
        var disabled = await Client.GetAsync(refundUrl);
        disabled.IsSuccessStatusCode.ShouldBeFalse();
    }

    [Fact]
    public async Task Money_void_modal_requires_reason_permission_and_antiforgery_and_encodes_errors()
    {
        var o = await CreateMoneyOrderAsync();
        var p = await ServiceMoney.AddPaymentAsync(o.Id, new() { Amount = 10, PaymentDate = DateTimeOffset.UtcNow,
            PaymentMethod = SalesPaymentMethod.Cash, IdempotencyKey = "payment" });
        var url = $"/Service/VoidPaymentModal?Id={o.Id}&paymentId={p.Id}&concurrencyStamp={p.ConcurrencyStamp}";
        var form = new Dictionary<string, string>
        {
            ["Id"] = o.Id.ToString(), ["Input.PaymentId"] = p.Id.ToString(), ["Input.ConcurrencyStamp"] = p.ConcurrencyStamp,
            ["Input.IdempotencyKey"] = "void", ["Input.Reason"] = ""
        };
        using (var missing = await Client.PostAsync(url, new FormUrlEncodedContent(form)))
            missing.Headers.Location!.ToString().ShouldBe("/Error?httpStatusCode=400");
        (await SubmitMoneyModal(url, form, VPureLuxPermissions.Service.ManagePayments)).IsSuccessStatusCode.ShouldBeFalse();
        (await SubmitMoneyModal(url, form)).StatusCode.ShouldBe(HttpStatusCode.OK);
        (await ServiceMoney.GetSummaryAsync(o.Id)).NetPaid.ShouldBe(10);
        form["Input.Reason"] = "<script>bad</script>";
        form["Input.ConcurrencyStamp"] = "stale";
        var stale = await SubmitMoneyModal(url, form);
        var html = await stale.Content.ReadAsStringAsync();
        html.ShouldContain("&lt;script&gt;bad&lt;/script&gt;"); html.ShouldNotContain("<script>bad</script>");
        form["Input.ConcurrencyStamp"] = p.ConcurrencyStamp;
        var result = await SubmitMoneyModal(url, form);
        result.StatusCode.ShouldBe(HttpStatusCode.NoContent, await result.Content.ReadAsStringAsync());
        (await ServiceMoney.GetSummaryAsync(o.Id)).NetPaid.ShouldBe(0);
        var voided = (await ServiceMoney.GetListAsync(new() { ServiceOrderId = o.Id })).Items.Single();
        voided.VoidReason.ShouldBe("<script>bad</script>"); voided.VoidedAt.ShouldNotBeNull(); voided.VoidedBy.ShouldNotBeNull();
    }

    [Fact]
    public async Task Money_history_HTTP_page_two_and_filters_return_only_requested_order()
    {
        var o = await CreateMoneyOrderAsync();
        for (var i = 0; i < 12; i++)
            await ServiceMoney.AddPaymentAsync(o.Id, new() { Amount = 1, PaymentDate = DateTimeOffset.UtcNow.AddMinutes(i),
                PaymentMethod = SalesPaymentMethod.Cash, IdempotencyKey = $"p{i}", ReferenceNo = $"PAY-{i:D2}" });
        var page = await Client.GetAsync($"/Service/Details/{o.Id}?handler=Payments&skipCount=10&maxResultCount=10&sorting=PaymentDate%20asc");
        page.StatusCode.ShouldBe(HttpStatusCode.OK, await page.Content.ReadAsStringAsync());
        using var json = JsonDocument.Parse(await page.Content.ReadAsStringAsync());
        json.RootElement.GetProperty("totalCount").GetInt32().ShouldBe(12);
        json.RootElement.GetProperty("items").GetArrayLength().ShouldBe(2);
        json.RootElement.GetProperty("items")[0].GetProperty("referenceNo").GetString().ShouldBe("PAY-10");
        await GetRequiredService<IServiceOrderAppService>().CancelAsync(o.Id, new() { ConcurrencyStamp = o.ConcurrencyStamp, Reason = "cancel" });
        for (var i = 0; i < 12; i++)
            await ServiceMoney.RefundAsync(o.Id, new() { Amount = 1, RefundDate = DateTimeOffset.UtcNow.AddMinutes(i),
                Method = SalesPaymentMethod.Cash, IdempotencyKey = $"r{i}", Reason = $"REF-{i:D2}" });
        using var refunds = JsonDocument.Parse(await Client.GetStringAsync($"/Service/Details/{o.Id}?handler=Refunds&skipCount=10&maxResultCount=10&sorting=RefundDate%20asc"));
        refunds.RootElement.GetProperty("totalCount").GetInt32().ShouldBe(12);
        refunds.RootElement.GetProperty("items").GetArrayLength().ShouldBe(2);
        refunds.RootElement.GetProperty("items")[0].GetProperty("reason").GetString().ShouldBe("REF-10");
    }
}

// Parsing does not need a TestServer; avoid another full ABP host per scalar input.
public class ServiceMoneyParsingTests
{
    [Theory]
    [InlineData("1.500.000", true, "1500000")]
    [InlineData("1.500.000,50", true, "1500000.50")]
    [InlineData("1500000.50", true, "1500000.50")]
    [InlineData("1500000,50", true, "1500000.50")]
    [InlineData("-1", false, "0")]
    [InlineData("0", false, "0")]
    [InlineData("1.50.000", false, "0")]
    [InlineData("invalid", false, "0")]
    public void Money_parser_is_strict_and_culture_independent(string input, bool valid, string expected)
    {
        MoneyModalModel.TryParseMoney(input, out var actual).ShouldBe(valid);
        if (valid) actual.ShouldBe(decimal.Parse(expected, System.Globalization.CultureInfo.InvariantCulture));
    }
}
