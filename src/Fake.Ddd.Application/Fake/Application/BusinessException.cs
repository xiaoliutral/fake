using Fake.ExceptionHandling;
using Fake.Logging;
using Microsoft.Extensions.Logging;

namespace Fake.Application;

public class BusinessException : FakeException, IHasLogLevel, ILocalizeErrorMessage
{
    public BusinessException(string errorCode, params object[] localizeArguments) : base(errorCode)
    {
        ErrorCode = errorCode;
        LocalizeArguments = localizeArguments;
    }
    
    public LogLevel LogLevel { get; set; } = LogLevel.Warning;

    public string ErrorCode { get; set; }
    public object[]? LocalizeArguments { get; set; }
}