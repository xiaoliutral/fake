using Fake.ExceptionHandling;
using Fake.Logging;
using Microsoft.Extensions.Logging;

namespace Fake.Authorization;

public class FakeAuthorizationException(string errorCode = "", params object[] localizeArguments)
    : FakeException(errorCode), IHasLogLevel, IHasLocalization
{
    public string ErrorCode { get; set; } = errorCode;
    public object[]? LocalizeArguments { get; set; } = localizeArguments;
    public LogLevel LogLevel { get; set; } = LogLevel.Warning;
}