using Fake.DependencyInjection;
using Fake.Validation.FluentValidation.Tests.Dtos;

namespace Fake.Validation.FluentValidation.Tests.Services;

public class BookService : IBookService, ITransientDependency, IValidationEnabled
{
    public Task CreateBook(CreateBookInput input)
    {
        return Task.CompletedTask;
    }
}