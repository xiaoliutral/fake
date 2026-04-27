using Microsoft.Extensions.Logging;

namespace Fake.Logging;

/// <summary>
/// 仅服务于FakeApplication初始化
/// </summary>
/// <typeparam name="T"></typeparam>
public interface IInitLogger<out T> : ILogger<T>
{
    public List<InitLoggerEntry> Entries { get; }
}