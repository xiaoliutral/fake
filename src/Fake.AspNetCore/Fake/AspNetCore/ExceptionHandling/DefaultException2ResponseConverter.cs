using System.Text;
using Fake.Application;
using Fake.Application.Dtos;
using Fake.DependencyInjection;
using Fake.ExceptionHandling;
using Fake.Localization;
using Microsoft.Extensions.Localization;

namespace Fake.AspNetCore.ExceptionHandling;

public class DefaultException2ResponseConverter(
    IStringLocalizerFactory stringLocalizerFactory,
    IOptions<FakeLocalizationOptions> localizationOptions) : IException2ResponseConverter, ITransientDependency
{
    private readonly FakeLocalizationOptions _localizationOptions = localizationOptions.Value;

    public virtual ApplicationExceptionResult Convert(Exception exception, FakeExceptionHandlingOptions options)
    {
        var errorInfo = new ApplicationExceptionResult(exception.Message);

        LocalizeErrorMessage(exception, errorInfo);

        if (options.OutputStackTrace)
        {
            var stackTrace = new StringBuilder();
            AddExceptionToDetails(exception, stackTrace);
            errorInfo.Details = stackTrace.ToString();
        }

        return errorInfo;
    }

    private void LocalizeErrorMessage(Exception exception, ApplicationExceptionResult errorInfo)
    {
        if (exception is not ILocalizeErrorMessage localizeErrorMessage) return;

        var errorResourceType =
            _localizationOptions.DefaultErrorResourceType ?? _localizationOptions.DefaultResourceType;
        if (errorResourceType == null) return;

        var stringLocalizer = stringLocalizerFactory.Create(errorResourceType);
        errorInfo.Message = localizeErrorMessage.LocalizeLocalizeArguments.IsNullOrEmpty()
            ? stringLocalizer[exception.Message]
            : stringLocalizer[exception.Message, localizeErrorMessage.LocalizeLocalizeArguments!];
    }

    protected virtual void AddExceptionToDetails(Exception exception, StringBuilder stackTrace)
    {
        // message
        stackTrace.AppendLine(exception.GetType().Name + ": " + exception.Message);

        // stack trace
        stackTrace.AppendLine($"Stack Trace: {exception.StackTrace}");

        // inner exception
        if (exception.InnerException != null)
        {
            AddExceptionToDetails(exception.InnerException, stackTrace);
        }

        // aggregate exception
        if (exception is AggregateException aggregateException)
        {
            foreach (var innerException in aggregateException.InnerExceptions)
            {
                AddExceptionToDetails(innerException, stackTrace);
            }
        }
    }
}