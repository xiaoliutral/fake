using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Shouldly;

namespace Fake.Messaging.Dashboard.Tests;

public class MessagingDashboardPathBaseTests
{
    [Fact]
    public async Task With_PathBase_admin_messaging_should_return_ui()
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            EnvironmentName = Environments.Development
        });
        builder.WebHost.UseTestServer();
        builder.Services.AddRouting();
        builder.Services.AddFakeMessagingDashboard(o => o.PathMatch = "/messaging");

        await using var app = builder.Build();
        app.UsePathBase("/admin");
        app.UseRouting();
        app.UseFakeMessagingDashboard();
        await app.StartAsync();

        var client = app.GetTestClient();
        var res = await client.GetAsync("/admin/messaging/");
        res.StatusCode.ShouldBe(System.Net.HttpStatusCode.OK);

        var html = await res.Content.ReadAsStringAsync();
        html.ShouldContain("/admin/messaging/api");

        var stats = await client.GetAsync("/admin/messaging/api/stats");
        stats.StatusCode.ShouldBe(System.Net.HttpStatusCode.OK);
    }
}
