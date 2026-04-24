using Fake.ExceptionHandling;
using Fake.Logging;
using Microsoft.Extensions.Logging;

namespace Fake.Application;

public class BusinessException : FakeException, IHasLogLevel, ILocalizeErrorMessage
{
    public BusinessException(string? message = null, Exception? innerException = null): base(message, innerException)
    {
        
    }
    public BusinessException(string? message = null, params object[] localizeArguments) : base(message)
    {
        LocalizeLocalizeArguments = localizeArguments;
    }
    
    public LogLevel LogLevel { get; set; } = LogLevel.Warning;
    public object[]? LocalizeLocalizeArguments { get; set; }
}