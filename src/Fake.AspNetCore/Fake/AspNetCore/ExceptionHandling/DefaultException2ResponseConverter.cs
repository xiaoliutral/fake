using System.Text;
using Fake.Application.Dtos;
using Fake.AspNetCore.ExceptionHandling.Localization;
using Fake.Data;
using Fake.DependencyInjection;
using Fake.Domain.Exceptions;
using Fake.ExceptionHandling;
using Fake.Localization;
using Fake.Validation;
using Microsoft.Extensions.Localization;

namespace Fake.AspNetCore.ExceptionHandling;

public class DefaultException2ResponseConverter(
    IStringLocalizerFactory stringLocalizerFactory,
    IOptions<FakeLocalizationOptions> localizationOptions,
    IStringLocalizer<FakeAspNetCoreErrorResource> l) : IException2ResponseConverter, ITransientDependency
{
    protected FakeLocalizationOptions LocalizationOptions = localizationOptions.Value;

    public virtual ApplicationExceptionResponse Convert(Exception exception, FakeExceptionHandlingOptions options)
    {
        var errorInfo = new ApplicationExceptionResponse(exception.Message);

        LocalizeErrorMessage(exception, errorInfo);

        if (errorInfo.Message.IsNullOrEmpty())
        {
            errorInfo.Message = l[FakeAspNetCoreErrorResource.InternalServerError];
        }

        if (options.OutputStackTrace)
        {
            var stackTrace = new StringBuilder();
            AddExceptionToDetails(exception, stackTrace);
            errorInfo.Details = stackTrace.ToString();
        }

        return errorInfo;
    }

    private void LocalizeErrorMessage(Exception exception, ApplicationExceptionResponse errorInfo)
    {
        switch (exception)
        {
            case FakeDbConcurrencyException:
                errorInfo.Message = l[FakeAspNetCoreErrorResource.DbConcurrencyError];
                break;
            case EntityNotFoundException entityNotFoundException:
                errorInfo.Message = string.Format(l[FakeAspNetCoreErrorResource.EntityNotFoundError],
                    entityNotFoundException.EntityType?.Name, entityNotFoundException.Id);
                break;
            case FakeValidationException fakeValidationException:
                errorInfo.Message = fakeValidationException.ValidationErrors.Select(x => x.ErrorMessage)
                    .JoinAsString("\n");
                break;
            case IHasLocalization localizeErrorMessage:
            {
                if (!localizeErrorMessage.ErrorCode.Contains(':')) break;
                var errorNamespace = localizeErrorMessage.ErrorCode.Split(':').First();
                var resourceType = LocalizationOptions.ErrorCodeNamespaceMappings.GetOrDefault(errorNamespace);
                if (resourceType == null) break;
                var stringLocalizer = stringLocalizerFactory.Create(resourceType);
                errorInfo.Message = localizeErrorMessage.LocalizeArguments.IsNullOrEmpty()
                    ? stringLocalizer[localizeErrorMessage.ErrorCode]
                    : stringLocalizer[localizeErrorMessage.ErrorCode, localizeErrorMessage.LocalizeArguments!];
                break;
            }
            default:
                errorInfo.Message = exception.Message;
                break;
        }
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