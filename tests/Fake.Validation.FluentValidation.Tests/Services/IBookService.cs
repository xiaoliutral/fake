using Fake.Validation.FluentValidation.Tests.Dtos;

namespace Fake.Validation.FluentValidation.Tests.Services;

public interface IBookService
{
    Task CreateBook(CreateBookInput input);
}