namespace Fake;

public class DisableFakeFeaturesAttribute : Attribute
{
    /// <summary>
    /// 禁用所有Fake拦截器
    /// </summary>
    public bool DisableInterceptors { get; set; } = true;

    /// <summary>
    /// 禁用所有Fake中间件
    /// </summary>
    public bool DisableMiddleware { get; set; } = true;

    /// <summary>
    /// 禁用所有Fake过滤器
    /// </summary>
    public bool DisableMvcFilters { get; set; } = true;
}