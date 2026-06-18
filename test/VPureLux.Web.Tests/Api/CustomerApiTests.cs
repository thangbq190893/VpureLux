using System;
using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Shouldly;
using VPureLux.Customers;
using VPureLux.Customers.CustomerGroups;
using Xunit;

namespace VPureLux.Api;

[Collection(VPureLuxTestConsts.CollectionDefinitionName)]
public class CustomerApiTests : VPureLuxWebTestBase
{
    [Fact]
    public async Task Should_Execute_CustomerGroup_Routes()
    {
        var created = await CreateGroupAsync();
        (await GetResponseAsObjectAsync<CustomerGroupDto>($"/api/customer-groups/{created.Id}")).Code.ShouldBe(created.Code);

        var update = await Client.PutAsJsonAsync($"/api/customer-groups/{created.Id}", new UpdateCustomerGroupDto { Name = "Updated", SortOrder = 2 });
        update.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await Client.PostAsync($"/api/customer-groups/{created.Id}/deactivate", null)).StatusCode.ShouldBe(HttpStatusCode.NoContent);
        (await Client.PostAsync($"/api/customer-groups/{created.Id}/activate", null)).StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Should_Execute_Customer_Routes()
    {
        var group = await CreateGroupAsync();
        var createResponse = await Client.PostAsJsonAsync("/api/customers", new CreateCustomerDto
        {
            Code = Unique("API-C"),
            Name = "API Customer",
            CustomerGroupId = group.Id
        });
        createResponse.StatusCode.ShouldBe(HttpStatusCode.OK, await createResponse.Content.ReadAsStringAsync());
        var customer = (await createResponse.Content.ReadFromJsonAsync<CustomerDto>())!;
        customer.CustomerGroupName.ShouldBe(group.Name);

        (await GetResponseAsObjectAsync<CustomerDto>($"/api/customers/{customer.Id}")).Id.ShouldBe(customer.Id);
        (await Client.PutAsJsonAsync($"/api/customers/{customer.Id}", new UpdateCustomerDto { Name = "Updated", CustomerGroupId = group.Id })).StatusCode.ShouldBe(HttpStatusCode.OK);
        (await Client.PostAsync($"/api/customers/{customer.Id}/deactivate", null)).StatusCode.ShouldBe(HttpStatusCode.NoContent);
        (await Client.PostAsync($"/api/customers/{customer.Id}/activate", null)).StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Should_Reject_Invalid_Customer_Request()
    {
        var response = await Client.PostAsJsonAsync("/api/customers", new CreateCustomerDto());
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    private async Task<CustomerGroupDto> CreateGroupAsync()
    {
        var response = await Client.PostAsJsonAsync("/api/customer-groups", new CreateCustomerGroupDto
        {
            Code = Unique("API-G"),
            Name = "API Group",
            SortOrder = 1
        });
        response.StatusCode.ShouldBe(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        return (await response.Content.ReadFromJsonAsync<CustomerGroupDto>())!;
    }

    private static string Unique(string prefix) => prefix + Guid.NewGuid().ToString("N")[..8];
}
