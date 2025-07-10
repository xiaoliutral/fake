using Fake.Authorization;

namespace Fake.AspNetCore.ExceptionHandling;

public interface IAuthorizationExceptionHandler
{
    Task HandleAsync(FakeAuthorizationException exception, HttpContext httpContext);
}