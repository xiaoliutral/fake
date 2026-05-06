namespace Fake.Application.Dtos;

/// <summary>
/// 应用服务异常响应结果
/// </summary>
[Serializable]
public class ApplicationExceptionResponse
{
    /// <summary>
    /// 异常信息
    /// </summary>
    public string? Message { get; set; }

    /// <summary>
    /// 异常明细
    /// </summary>
    public string? Details { get; set; }

    public ApplicationExceptionResponse()
    {
    }

    public ApplicationExceptionResponse(string? message = null)
    {
        Message = message;
    }
}