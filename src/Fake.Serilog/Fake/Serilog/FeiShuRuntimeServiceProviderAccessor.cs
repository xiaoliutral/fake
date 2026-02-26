using Fake.DependencyInjection;

namespace Fake.Serilog;

/// <summary>
/// 桥接DI，支持纯配置文件下的依赖注入
/// </summary>
public static class FeiShuRuntimeServiceProviderAccessor
{
    public static ObjectAccessor<IServiceProvider>? Accessor { get; private set; }

    internal static void SetAccessor(ObjectAccessor<IServiceProvider> accessor)
    {
        Accessor = accessor;
    }
}
