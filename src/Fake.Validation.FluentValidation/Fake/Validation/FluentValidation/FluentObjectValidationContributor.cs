using System.ComponentModel.DataAnnotations;
using Fake.Helpers;
using FluentValidation;

namespace Fake.Validation.FluentValidation.Fake.Validation.FluentValidation;

public class FluentObjectValidationContributor(IServiceProvider serviceProvider): IObjectValidationContributor
{
    public async Task AddErrorsAsync(ObjectValidationContext context)
    {
        var serviceType = typeof(IValidator<>).MakeGenericType(context.ValidatingObject.GetType());
        var validator = serviceProvider.GetService(serviceType).As<IValidator>();

        if (validator == null) return;
        
        var result = await validator.ValidateAsync((IValidationContext)ReflectionHelper.CreateInstance(
            typeof(ValidationContext<>).MakeGenericType(context.ValidatingObject.GetType()),
            context.ValidatingObject)!);
        
        if (!result.IsValid)
        {
            context.Errors.AddRange(
                result.Errors.Select(
                    error =>
                        new ValidationResult(error.ErrorMessage, [error.PropertyName])
                )
            );
        }
    }
}