using System.ComponentModel.DataAnnotations;
using System.Net;
using Fake.Application;
using Fake.Authorization;
using Fake.Data;
using Fake.DependencyInjection;
using Fake.Domain.Exceptions;
using Fake.Validation;

namespace Fake.AspNetCore.ExceptionHandling;

/// <summary>
/// default exception http codes: 400 401 403 500
/// </summary>
public class DefaultHttpExceptionStatusCodeFinder : IHttpExceptionStatusCodeFinder, ITransientDependency
{
    public virtual HttpStatusCode Find(HttpContext httpContext, Exception exception)
    {
        if (exception is FakeAuthorizationException)
        {
            var isAuthenticated = httpContext.User.Identity?.IsAuthenticated ?? false;
            return isAuthenticated
                ? HttpStatusCode.Forbidden
                : HttpStatusCode.Unauthorized;
        }

        if (exception is FakeDbConcurrencyException) return HttpStatusCode.Conflict;

        if (exception is DomainException or FakeValidationException or BusinessException) return HttpStatusCode.BadRequest;

        return HttpStatusCode.InternalServerError;
    }
}