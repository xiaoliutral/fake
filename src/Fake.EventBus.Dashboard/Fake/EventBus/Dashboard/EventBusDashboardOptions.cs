namespace Fake.EventBus.Dashboard;

/// <summary>
/// 独立 EventBus Dashboard 配置（对齐 CAP UseDashboard 风格）。
/// </summary>
public class EventBusDashboardOptions
{
    /// <summary>
    /// Dashboard 根路径，默认 <c>/eventbus</c>。
    /// </summary>
    public string PathMatch { get; set; } = "/eventbus";

    /// <summary>
    /// 显式允许匿名访问（开发环境）。生产环境建议配合鉴权中间件。
    /// </summary>
    public bool AllowAnonymousExplicit { get; set; } = true;

    /// <summary>
    /// 指标轮询间隔（毫秒），前端图表使用。
    /// </summary>
    public int MetricsPollIntervalMs { get; set; } = 2000;
}
