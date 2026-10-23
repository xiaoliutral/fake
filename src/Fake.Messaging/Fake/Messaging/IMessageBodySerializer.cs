namespace Fake.Messaging;

/// <summary>
/// Messaging 消息体编解码。默认 <see cref="SystemTextJsonMessageBodySerializer"/>；由框架注入，业务侧一般无需直接使用。
/// </summary>
public interface IMessageBodySerializer
{
    byte[] Serialize(object value, Type type);

    byte[] Serialize<T>(T value) => Serialize(value!, typeof(T));

    object? Deserialize(ReadOnlySpan<byte> utf8Json, Type type);

    T? Deserialize<T>(ReadOnlySpan<byte> utf8Json)
        => (T?)Deserialize(utf8Json, typeof(T));
}
