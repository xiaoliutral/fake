using Fake.DependencyInjection;
using Fake.Validation.Tests.Dtos;

namespace Fake.Validation.Tests.Services;

public class BookService : IBookService, ITransientDependency, IValidationEnabled
{
    public Task CreateBook(CreateBookInput input)
    {
        return Task.CompletedTask;
    }
}