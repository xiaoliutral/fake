namespace Fake.Application.Dtos;

/// <summary>
/// 应用服务异常响应模型
/// </summary>
[Serializable]
public class ApplicationExceptionResult
{
    /// <summary>
    /// 异常信息
    /// </summary>
    public string? Message { get; set; }

    /// <summary>
    /// 异常明细
    /// </summary>
    public string? Details { get; set; }

    public ApplicationExceptionResult()
    {
    }

    public ApplicationExceptionResult(string? message = null)
    {
        Message = message;
    }
}