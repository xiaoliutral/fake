using System.ComponentModel.DataAnnotations;
using Fake.Logging;
using Microsoft.Extensions.Logging;

namespace Fake.Validation;

public class FakeValidationException: FakeException, IHasLogLevel
{
    public LogLevel LogLevel { get; set; } = LogLevel.Warning;

    /// <summary>
    /// Detailed list of validation errors for this exception.
    /// </summary>
    public IList<ValidationResult> ValidationErrors { get; } = [];

    public FakeValidationException()
    {
    }

    public FakeValidationException(string message)
        : base(message)
    {
    }
    
    public FakeValidationException(string message, IList<ValidationResult> validationErrors)
        : base(message)
    {
        ValidationErrors = validationErrors;
    }

    public FakeValidationException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}