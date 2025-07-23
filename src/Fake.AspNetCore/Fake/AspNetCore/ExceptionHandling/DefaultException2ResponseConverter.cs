using System.Text;
using Fake.Application;
using Fake.Application.Dtos;
using Fake.DependencyInjection;
using Fake.ExceptionHandling;
using Fake.Localization;
using Microsoft.Extensions.Localization;

namespace Fake.AspNetCore.ExceptionHandling;

public class DefaultException2ResponseConverter : IException2ResponseConverter, ITransientDependency
{
    private readonly IStringLocalizerFactory _stringLocalizerFactory;
    private readonly IOptions<FakeLocalizationOptions> _localizationOptions;

    public DefaultException2ResponseConverter(IStringLocalizerFactory stringLocalizerFactory,
        IOptions<FakeLocalizationOptions> localizationOptions)
    {
        _stringLocalizerFactory = stringLocalizerFactory;
        _localizationOptions = localizationOptions;
    }

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

        var errorResourceType = _localizationOptions.Value.DefaultErrorResourceType;
        if (errorResourceType == null) return;

        var stringLocalizer = _stringLocalizerFactory.Create(errorResourceType);
        errorInfo.Message = localizeErrorMessage.Arguments.IsNullOrEmpty()
            ? stringLocalizer[exception.Message]
            : stringLocalizer[exception.Message, localizeErrorMessage.Arguments!];
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