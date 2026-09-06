using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Shouldly;
using Xunit;

namespace VPureLux.Pages;

public class WarrantyAntiforgeryTests : VPureLuxWebTestBase
{
    public static TheoryData<string> MutationPages => new()
    {
        $"/Warranty/Install/{Guid.NewGuid()}",
        $"/Warranty/ReminderActionModal?Id={Guid.NewGuid()}&Action=Complete",
        $"/Warranty/SyncFailures?handler=Retry&id={Guid.NewGuid()}",
        "/Warranty/Assets/CreateExternal"
    };

    [Theory]
    [MemberData(nameof(MutationPages))]
    public async Task Warranty_mutation_page_should_reject_post_without_antiforgery_token(string url)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = new FormUrlEncodedContent(new Dictionary<string, string>())
        };
        request.Headers.Add("X-W008-Test-Auth", "true");
        var response = await Client.SendAsync(request);

        response.StatusCode.ShouldBe(HttpStatusCode.Found);
        response.Headers.Location.ShouldNotBeNull();
        response.Headers.Location!.ToString().ShouldBe("/Error?httpStatusCode=400");
    }
}
