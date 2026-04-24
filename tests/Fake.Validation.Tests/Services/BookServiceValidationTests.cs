using Fake.Testing;
using Fake.Validation.Tests.Dtos;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Fake.Validation.Tests.Services;

public class BookServiceValidationTests : ApplicationTest<FakeValidationTestModule>
{
    private readonly IBookService _bookService;
    
    public BookServiceValidationTests()
    {
        _bookService = ServiceProvider.GetRequiredService<IBookService>();
    }

    [Fact]
    public async Task 可以校验出来()
    {
        var input = new CreateBookInput
        {
            Name = null,
            Numbers = 0
        };
        var error = await Assert.ThrowsAsync<FakeValidationException>(async () => await _bookService.CreateBook(input));
        
        Assert.True(error.ValidationErrors.Count == 2);
    }
    
    [Fact]
    public async Task 正常通过()
    {
        var input = new CreateBookInput
        {
            Name = "西游记",
            Numbers = 711
        };
        await _bookService.CreateBook(input);
    }
}