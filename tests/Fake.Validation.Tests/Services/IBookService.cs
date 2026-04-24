using Fake.Validation.Tests.Dtos;

namespace Fake.Validation.Tests.Services;

public interface IBookService
{
    Task CreateBook(CreateBookInput input);
}