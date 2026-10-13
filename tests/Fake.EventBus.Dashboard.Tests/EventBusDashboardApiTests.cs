using System.Net;
using System.Text.Json;
using Fake.EventBus.Distributed;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace Fake.EventBus.Dashboard.Tests;

public class EventBusDashboardApiTests : IClassFixture<DashboardHostFixture>
{
    private readonly DashboardHostFixture _fixture;

    public EventBusDashboardApiTests(DashboardHostFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task 首页应返回HTML()
    {
        var res = await _fixture.Client.GetAsync("/eventbus/");
        res.StatusCode.ShouldBe(HttpStatusCode.OK);
        var html = await res.Content.ReadAsStringAsync();
        html.ShouldContain("Fake EventBus Dashboard");
        html.ShouldContain("/eventbus/api");
    }

    [Fact]
    public async Task dashboard_html应返回同一UI()
    {
        var res = await _fixture.Client.GetAsync("/eventbus/dashboard.html");
        res.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await res.Content.ReadAsStringAsync()).ShouldContain("Fake EventBus Dashboard");
    }

    [Fact]
    public async Task Metrics接口应返回快照()
    {
        var metrics = _fixture.Services.GetRequiredService<EventBusMetrics>();
        metrics.RecordOutboxEnqueued("Demo");

        var res = await _fixture.Client.GetAsync("/eventbus/api/metrics");
        res.StatusCode.ShouldBe(HttpStatusCode.OK);
        using var doc = JsonDocument.Parse(await res.Content.ReadAsStringAsync());
        doc.RootElement.GetProperty("outboxEnqueued").GetInt64().ShouldBeGreaterThanOrEqualTo(1);
    }

    [Fact]
    public async Task Stats接口应可用()
    {
        var res = await _fixture.Client.GetAsync("/eventbus/api/stats");
        res.StatusCode.ShouldBe(HttpStatusCode.OK);
        using var doc = JsonDocument.Parse(await res.Content.ReadAsStringAsync());
        doc.RootElement.GetProperty("dlxAvailable").GetBoolean().ShouldBeFalse();
        doc.RootElement.GetProperty("metrics").ValueKind.ShouldBe(JsonValueKind.Object);
    }

    [Fact]
    public async Task Outbox列表在空存储下应返回空页()
    {
        var res = await _fixture.Client.GetAsync("/eventbus/api/outbox?pageIndex=1&pageSize=10");
        res.StatusCode.ShouldBe(HttpStatusCode.OK);
        using var doc = JsonDocument.Parse(await res.Content.ReadAsStringAsync());
        doc.RootElement.GetProperty("total").GetInt32().ShouldBe(0);
        doc.RootElement.GetProperty("items").GetArrayLength().ShouldBe(0);
    }

    [Fact]
    public async Task Dlx在未接入时available为false()
    {
        var res = await _fixture.Client.GetAsync("/eventbus/api/dlx/messages");
        res.StatusCode.ShouldBe(HttpStatusCode.OK);
        using var doc = JsonDocument.Parse(await res.Content.ReadAsStringAsync());
        doc.RootElement.GetProperty("available").GetBoolean().ShouldBeFalse();
    }
}
