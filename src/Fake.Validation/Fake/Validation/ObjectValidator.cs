using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Fake.Validation;

public class ObjectValidator(IOptions<FakeValidationOptions> options, IServiceScopeFactory serviceScopeFactory)
    : IObjectValidator
{
    private readonly FakeValidationOptions _validationOptions = options.Value;

    public virtual async Task ValidateAsync(object validatingObject, string? name = null, bool allowNull = false)
    {
        var errors = await GetErrorsAsync(validatingObject, name, allowNull);

        if (errors.Count != 0)
        {
            throw new FakeValidationException(
                "Object state is not valid! See ValidationErrors for details.",
                errors
            );
        }
    }

    public virtual async Task<List<ValidationResult>> GetErrorsAsync(object validatingObject, string? name = null,
        bool allowNull = false)
    {
        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        if (validatingObject == null)
        {
            if (allowNull)
            {
                return [];
            }

            return
            [
                name == null
                    ? new ValidationResult("Given object is null!")
                    : new ValidationResult(name + " is null!", [name])
            ];
        }

        var context = new ObjectValidationContext(validatingObject);

        using (var scope = serviceScopeFactory.CreateScope())
        {
            foreach (var contributorType in _validationOptions.Contributors)
            {
                var contributor = scope.ServiceProvider.GetRequiredService(contributorType)
                    .To<IObjectValidationContributor>();
                await contributor.AddErrorsAsync(context);
            }
        }

        return context.Errors;
    }
}