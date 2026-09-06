using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Text.Json;
using HtmlAgilityPack;
using Shouldly;
using VPureLux.Permissions;
using VPureLux.Service;
using Xunit;

namespace VPureLux.Pages;

public partial class ServiceOrderWebTests
{
    [Theory]
    [InlineData("1", "2026-09-07T12:45", true)]
    [InlineData("1", "2026-09-07T01:30", true)]
    [InlineData("0", "2026-09-07T12:45", true)]
    [InlineData("1,5", "2026-09-07T12:45", false)]
    [InlineData("2", "2026-09-07T12:45", false)]
    [InlineData("1", "07/09/2026", false)]
    public async Task Completion_modal_validates_integer_actual_date_and_antiforgery(string quantity, string date, bool success)
    {
        Enable();
        var f = await CreateFixtureAsync("WEB-COMPLETE", "<b>Service work</b>");
        var service = GetRequiredService<IServiceOrderAppService>();
        var order = await service.CreateAsync(new()
        {
            CustomerAssetId = f.AssetId, WarehouseId = f.WarehouseId,
            Lines = [new() { LineType = ServiceOrderLineType.Labor, CatalogItemId = f.WorkId, Quantity = 1, UnitPrice = 100 }]
        });
        order = await service.ConfirmAsync(order.Id, new() { ConcurrencyStamp = order.ConcurrencyStamp });
        order = await service.StartAsync(order.Id, new() { ConcurrencyStamp = order.ConcurrencyStamp });
        var url = $"/Service/CompleteModal?Id={order.Id}";
        using (var denied = new HttpRequestMessage(HttpMethod.Get, url))
        {
            denied.Headers.Add("X-S001-Deny", VPureLuxPermissions.Service.Complete);
            (await Client.SendAsync(denied)).IsSuccessStatusCode.ShouldBeFalse();
        }
        var modal = await Client.GetAsync(url);
        var markup = await modal.Content.ReadAsStringAsync();
        modal.StatusCode.ShouldBe(HttpStatusCode.OK, markup);
        markup.ShouldContain("&lt;b&gt;Service work&lt;/b&gt;");
        markup.ShouldContain("type=\"datetime-local\"");
        markup.ShouldContain("min=\"0\"");
        var html = new HtmlDocument();
        html.LoadHtml(markup);
        var input = new Dictionary<string, string>
        {
            ["Id"] = order.Id.ToString(),
            ["Input.ConcurrencyStamp"] = order.ConcurrencyStamp,
            ["Input.IdempotencyKey"] = html.DocumentNode.SelectSingleNode("//input[@name='Input.IdempotencyKey']").GetAttributeValue("value", ""),
            ["Input.CompletedAt"] = date,
            ["Input.Lines[0].LineId"] = order.Lines.Single().Id.ToString(),
            ["Input.Lines[0].ActualQuantity"] = quantity
        };
        AddAntiforgery(modal, input);
        using var post = new HttpRequestMessage(HttpMethod.Post, url) { Content = new FormUrlEncodedContent(input) };
        AddCookies(modal, post);
        var result = await Client.SendAsync(post);
        result.StatusCode.ShouldBe(success ? HttpStatusCode.NoContent : HttpStatusCode.OK, await result.Content.ReadAsStringAsync());
        var saved = await service.GetAsync(order.Id);
        saved.Status.ShouldBe(success ? ServiceOrderStatus.Completed : ServiceOrderStatus.InProgress);
        if (success)
        {
            var localCompletedAt = DateTime.ParseExact(date, "yyyy-MM-ddTHH:mm", CultureInfo.InvariantCulture);
            saved.CompletedAt.ShouldBe(localCompletedAt.AddHours(-7));
            saved.Lines.Single().ActualQuantity.ShouldBe(int.Parse(quantity));
            var history = await Client.GetAsync($"/Warranty/Assets/Details/{f.AssetId}?handler=History");
            history.StatusCode.ShouldBe(HttpStatusCode.OK, await history.Content.ReadAsStringAsync());
            using var historyJson = JsonDocument.Parse(await history.Content.ReadAsStringAsync());
            historyJson.RootElement.GetProperty("items")[0].GetProperty("occurredAt").GetString()
                .ShouldBe(localCompletedAt.ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture));
        }
    }
}
