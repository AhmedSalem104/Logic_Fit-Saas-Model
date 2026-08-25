using System.Net;
using System.Net.Http.Json;
using LogicFit.Shared;
using Microsoft.AspNetCore.Mvc.Testing;

namespace LogicFit.ApiTests;

public sealed class FoundationApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient client;

    public FoundationApiTests(WebApplicationFactory<Program> factory)
    {
        client = factory.CreateClient();
    }

    [Fact]
    public async Task HealthReturnsTheApprovedEnvelopeAndRequestId()
    {
        using var response = await client.GetAsync("/api/v1/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(response.Headers.Contains("X-Request-Id"));
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<HealthData>>();
        Assert.NotNull(body);
        Assert.Equal("healthy", body.Data.Status);
        Assert.Equal("api", body.Data.Service);
    }

    [Fact]
    public async Task VersionReturnsTheOfficialApiVersion()
    {
        using var response = await client.GetAsync("/api/v1/version");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<VersionData>>();
        Assert.NotNull(body);
        Assert.Equal("v1", body.Data.ApiVersion);
    }
}
