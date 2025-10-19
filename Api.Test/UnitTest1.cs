// api/tests/AuthTests.cs
using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.VisualStudio.TestPlatform.TestHost;
using Xunit;

public class AuthTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    public AuthTests(WebApplicationFactory<Program> factory) => _factory = factory;

    [Fact]
    public async Task Orders_Unauthorized_Without_Token()
    {
        var client = _factory.CreateClient();
        var res = await client.GetAsync("/orders");
        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
    }
}
