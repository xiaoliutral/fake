using Fake.ExceptionHandling;
using Fake.Logging;
using Microsoft.Extensions.Logging;

namespace Fake.Application;

public class BusinessException : FakeException, IHasLogLevel, ILocalizeErrorMessage
{
    public BusinessException(string? message = null, Exception? innerException = null): base(message, innerException)
    {
        
    }
    public BusinessException(string? message = null, params object[] arguments) : base(message)
    {
        Arguments = arguments;
    }
    
    public LogLevel LogLevel { get; set; } = LogLevel.Information;
    public object[]? Arguments { get; set; }
}