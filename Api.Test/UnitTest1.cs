// api/tests/AuthTests.cs
using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.VisualStudio.TestPlatform.TestHost;
using FluentAssertions;

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

public class ProductsTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _f;
    public ProductsTests(WebApplicationFactory<Program> f) => _f = f;

    [Fact]
    public async Task List_Returns_Ok_And_Paged()
    {
        var c = _f.CreateClient();
        var res = await c.GetAsync("/products?take=2");
        res.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await res.Content.ReadAsStringAsync();
        body.Should().Contain("items").And.Contain("nextAfter");
    }
}
