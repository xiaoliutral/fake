using Fake.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Fake.Messaging.EntityFrameworkCore;

/// <summary>
/// 注册 <see cref="MessagingDbContext"/>：解析连接串 + 由宿主配置数据库提供方（MySQL/SQL Server…）。
/// </summary>
public static class MessagingDbContextServiceCollectionExtensions
{
    /// <summary>首选连接串名（<c>ConnectionStrings:Messaging</c>）。</summary>
    public const string DefaultConnectionStringName = "Messaging";

    /// <summary>
    /// 注册 Outbox/Inbox DbContext。连接串按 <paramref name="connectionStringNames"/> 依次回退；
    /// 未传时默认 <c>Messaging</c> → <c>Default</c>。
    /// <paramref name="configureProvider"/> 由宿主绑定具体数据库（如 Pomelo <c>UseMySql</c>）。
    /// </summary>
    public static IServiceCollection AddMessagingDbContext(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<DbContextOptionsBuilder, string> configureProvider,
        params string[] connectionStringNames)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(configureProvider);

        var names = connectionStringNames is { Length: > 0 }
            ? connectionStringNames
            : [DefaultConnectionStringName, ConnectionStrings.DefaultConnectionStringName];

        var connectionString = ResolveConnectionString(configuration, names);
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                $"MessagingDbContext connection string not found. Tried: {string.Join(" → ", names)}.");
        }

        services.AddDbContext<MessagingDbContext>(options => configureProvider(options, connectionString));
        return services;
    }

    private static string? ResolveConnectionString(IConfiguration configuration, IReadOnlyList<string> names)
    {
        foreach (var name in names)
        {
            var cs = configuration.GetConnectionString(name);
            if (!string.IsNullOrWhiteSpace(cs))
                return cs;
        }

        return configuration[$"ConnectionStrings:{ConnectionStrings.DefaultConnectionStringName}"];
    }
}
