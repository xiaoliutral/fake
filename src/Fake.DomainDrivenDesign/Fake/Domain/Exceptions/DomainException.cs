using Fake.ExceptionHandling;
using Fake.Logging;
using Microsoft.Extensions.Logging;

namespace Fake.Domain.Exceptions;

[Serializable]
public class DomainException
    : FakeException, IHasLogLevel, ILocalizeErrorMessage
{
    public DomainException(string? message = null, Exception? innerException = null): base(message, innerException)
    {
        
    }
    public DomainException(string? message = null, params object[] localizeArguments) : base(message)
    {
        LocalizeLocalizeArguments = localizeArguments;
    }
    public LogLevel LogLevel { get; set; } = LogLevel.Warning;
    public object[]? LocalizeLocalizeArguments { get; set; }
}