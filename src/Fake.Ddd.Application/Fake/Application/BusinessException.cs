using Fake.ExceptionHandling;
using Fake.Logging;
using Microsoft.Extensions.Logging;

namespace Fake.Application;

public class BusinessException(string errorCode = "", params object[] localizeArguments)
    : FakeException(errorCode), IHasLogLevel, IHasLocalization
{
    public LogLevel LogLevel { get; set; } = LogLevel.Warning;

    public string ErrorCode { get; set; } = errorCode;
    public object[]? LocalizeArguments { get; set; } = localizeArguments;
}