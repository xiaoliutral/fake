using System.Net;

namespace Fake.AspNetCore.ExceptionHandling;

public interface IHttpExceptionStatusCodeFinder
{
    HttpStatusCode Find(HttpContext httpContext, Exception exception);
}