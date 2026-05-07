namespace Fake.ExceptionHandling;

/// <summary>
/// 使用本地化错误消息，异常message将被尝试本地化
/// </summary>
public interface IHasLocalization
{
    /// <summary>
    /// 错误码
    /// </summary>
    public string ErrorCode { get; set; }
    
    /// <summary>
    /// 本地化参数
    /// </summary>
    public object[]? LocalizeArguments { get; set; }
}