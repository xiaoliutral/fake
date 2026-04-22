namespace Fake.AspNetCore.ExceptionHandling;

public class FakeExceptionHandlingOptions
{
    /// <summary>
    /// 输出栈追踪
    /// </summary>
    public bool OutputStackTrace { get; set; }

    /// <summary>
    /// 不记录这些异常的日志
    /// </summary>
    public List<Type> ExcludeExceptionLogs { get; } = [];
    
    public bool ShouldLogException(Exception exception)
    {
        return ExcludeExceptionLogs.All(x => x != exception.GetType());
    }
}