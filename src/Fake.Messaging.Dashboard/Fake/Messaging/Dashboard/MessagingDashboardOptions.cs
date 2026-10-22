namespace Fake.Messaging.Dashboard;

/// <summary>
/// Messaging Dashboard 配置。
/// </summary>
public class MessagingDashboardOptions
{
    public const string SectionName = "Messaging:Dashboard";

    /// <summary>
    /// Dashboard 根路径，默认 <c>/messaging</c>。原 EventBus 路径可配置为 <c>/eventbus</c>。
    /// </summary>
    public string PathMatch { get; set; } = "/messaging";

    /// <summary>
    /// 显式允许匿名访问（开发环境）。生产环境建议配合鉴权中间件。
    /// </summary>
    public bool AllowAnonymousExplicit { get; set; } = true;

    /// <summary>
    /// 指标轮询间隔（毫秒），前端图表使用。
    /// </summary>
    public int MetricsPollIntervalMs { get; set; } = 2000;
}
