namespace Fake.ExceptionHandling;

/// <summary>
/// 使用本地化错误消息，异常message将被尝试本地化
/// </summary>
public interface ILocalizeErrorMessage
{
    /// <summary>
    /// 本地化参数
    /// </summary>
    public object[]? Arguments { get; set; }
}