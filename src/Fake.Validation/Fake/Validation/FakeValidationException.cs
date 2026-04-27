using System.ComponentModel.DataAnnotations;
using System.Text;
using Fake.Logging;
using Microsoft.Extensions.Logging;

namespace Fake.Validation;

public class FakeValidationException: FakeException, IHasLogLevel, IHasExceptionLog
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

    public void Log(ILogger logger)
    {
        if (ValidationErrors.IsNullOrEmpty()) return;

        var validationErrors = new StringBuilder();
        validationErrors.AppendLine("There are " + ValidationErrors.Count + " validation errors:");
        foreach (var validationResult in ValidationErrors)
        {
            var memberNames = "";
            if (validationResult.MemberNames.Any())
            {
                memberNames = $" ({string.Join(", ", validationResult.MemberNames)})";
            }

            validationErrors.AppendLine(validationResult.ErrorMessage + memberNames);
        }

        logger.LogWithLevel(LogLevel, validationErrors.ToString());
    }
}