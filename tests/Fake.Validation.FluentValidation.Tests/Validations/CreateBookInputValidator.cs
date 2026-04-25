using Fake.Validation.FluentValidation.Tests.Dtos;
using FluentValidation;

namespace Fake.Validation.FluentValidation.Tests.Validations;

public class CreateBookInputValidator: AbstractValidator<CreateBookInput>
{
    public CreateBookInputValidator()
    {
        RuleFor(x => x.Name).NotEmpty().WithMessage("Name is required");
        RuleFor(x => x.Numbers)
            .Must(x => x is >= 10 and <= 1000).WithMessage("Numbers must be between 10 and 1000");
    }
}