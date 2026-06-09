using System.Net;
using KiwiCart.Infrastructure.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Xunit;

namespace KiwiCart.Tests;

public class HealthCheckTests : IClassFixture<HealthCheckTests.TestFactory>
{
    private readonly HttpClient _client;

    public HealthCheckTests(TestFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task HealthCheck_ReturnsOk()
    {
        var response = await _client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    public class TestFactory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureAppConfiguration(config =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Database=test;Username=test;Password=test",
                    ["Auth0:Domain"] = "https://test.au.auth0.com",
                    ["Auth0:Audience"] = "https://api.kiwicart.local"
                });
            });

            builder.ConfigureServices(services =>
            {
                // Replace DbContext with InMemory
                services.RemoveAll(typeof(DbContextOptions<AppDbContext>));
                services.AddDbContext<AppDbContext>(o =>
                    o.UseInMemoryDatabase("HealthCheckTest"));

                // Replace the NpgSql health check with a no-op
                services.Configure<HealthCheckServiceOptions>(opts =>
                {
                    opts.Registrations.Clear();
                });
            });
        }
    }
}
