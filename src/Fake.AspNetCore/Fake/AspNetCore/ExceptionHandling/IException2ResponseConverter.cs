using Fake.Application;
using Fake.Application.Dtos;

namespace Fake.AspNetCore.ExceptionHandling;

public interface IException2ResponseConverter
{
    ApplicationExceptionResponse Convert(Exception exception, FakeExceptionHandlingOptions options);
}