using Fake.ExceptionHandling;
using Fake.Logging;
using Microsoft.Extensions.Logging;

namespace Fake.Application;

public class BusinessException(string message = "", params object[] localizeArguments)
    : FakeException(message), IHasLogLevel, IHasLocalization
{
    public LogLevel LogLevel { get; set; } = LogLevel.Warning;

    public string ErrorCode { get; set; } = message;
    public object[]? LocalizeArguments { get; set; } = localizeArguments;
}