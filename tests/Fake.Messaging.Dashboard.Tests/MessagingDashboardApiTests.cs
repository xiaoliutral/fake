using System.Net;
using System.Text.Json;
using Shouldly;

namespace Fake.Messaging.Dashboard.Tests;

public class MessagingDashboardApiTests : IClassFixture<DashboardHostFixture>
{
    private readonly DashboardHostFixture _fixture;

    public MessagingDashboardApiTests(DashboardHostFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task 首页应返回HTML()
    {
        var res = await _fixture.Client.GetAsync("/messaging/");
        res.StatusCode.ShouldBe(HttpStatusCode.OK);
        var html = await res.Content.ReadAsStringAsync();
        html.ShouldContain("Fake Messaging Dashboard");
        html.ShouldContain("/messaging/api");
    }

    [Fact]
    public async Task dashboard_html应返回同一UI()
    {
        var res = await _fixture.Client.GetAsync("/messaging/dashboard.html");
        res.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await res.Content.ReadAsStringAsync()).ShouldContain("Fake Messaging Dashboard");
    }

    [Fact]
    public async Task 无EventBus适配时Metrics为404()
    {
        var res = await _fixture.Client.GetAsync("/messaging/api/metrics");
        res.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Stats接口应可用且features关闭()
    {
        var res = await _fixture.Client.GetAsync("/messaging/api/stats");
        res.StatusCode.ShouldBe(HttpStatusCode.OK);
        using var doc = JsonDocument.Parse(await res.Content.ReadAsStringAsync());
        doc.RootElement.GetProperty("dlxAvailable").GetBoolean().ShouldBeFalse();
        var features = doc.RootElement.GetProperty("features");
        features.GetProperty("metrics").GetBoolean().ShouldBeFalse();
        features.GetProperty("subscriptions").GetBoolean().ShouldBeFalse();
        features.GetProperty("dlx").GetBoolean().ShouldBeFalse();
        features.GetProperty("inboxReprocess").GetBoolean().ShouldBeFalse();
    }

    [Fact]
    public async Task Outbox列表在空存储下应返回空页()
    {
        var res = await _fixture.Client.GetAsync("/messaging/api/outbox?pageIndex=1&pageSize=10");
        res.StatusCode.ShouldBe(HttpStatusCode.OK);
        using var doc = JsonDocument.Parse(await res.Content.ReadAsStringAsync());
        doc.RootElement.GetProperty("total").GetInt32().ShouldBe(0);
        doc.RootElement.GetProperty("items").GetArrayLength().ShouldBe(0);
    }

    [Fact]
    public async Task Dlx在未接入时available为false()
    {
        var res = await _fixture.Client.GetAsync("/messaging/api/dlx/messages");
        res.StatusCode.ShouldBe(HttpStatusCode.OK);
        using var doc = JsonDocument.Parse(await res.Content.ReadAsStringAsync());
        doc.RootElement.GetProperty("available").GetBoolean().ShouldBeFalse();
    }

    [Fact]
    public async Task Subscriptions接口在无适配时available为false()
    {
        var res = await _fixture.Client.GetAsync("/messaging/api/subscriptions");
        res.StatusCode.ShouldBe(HttpStatusCode.OK);
        using var doc = JsonDocument.Parse(await res.Content.ReadAsStringAsync());
        doc.RootElement.GetProperty("available").GetBoolean().ShouldBeFalse();
        doc.RootElement.GetProperty("items").ValueKind.ShouldBe(JsonValueKind.Array);
    }

    [Fact]
    public async Task UI应包含Kind筛选与核心控件()
    {
        var html = await _fixture.Client.GetStringAsync("/messaging/");
        html.ShouldContain("outboxKind");
        html.ShouldContain("inboxKind");
        html.ShouldContain("btnCopyJson");
        html.ShouldContain("Retry &amp; dispatch");
        html.ShouldContain("clearInbox");
        html.ShouldContain("reprocessNow");
        html.ShouldNotContain("cdn.jsdelivr.net");
        html.ShouldContain("drawSparkline");
        html.ShouldContain("tzSelect");
        html.ShouldContain("fmtTime");
        html.ShouldContain("resolvedOptions().timeZone");
    }
}
