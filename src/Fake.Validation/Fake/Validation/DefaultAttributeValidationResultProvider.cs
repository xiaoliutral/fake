using System.ComponentModel.DataAnnotations;
using Fake.Localization;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;

namespace Fake.Validation;

public class DefaultAttributeValidationResultProvider(
    IOptions<FakeLocalizationOptions> localizationOptions,
    IStringLocalizerFactory stringLocalizerFactory) : IAttributeValidationResultProvider
{
    public virtual ValidationResult? GetOrDefault(ValidationAttribute validationAttribute, object? validatingObject,
        ValidationContext validationContext)
    {
        var stringLocalizer = stringLocalizerFactory.Create(validationContext.ObjectType);

        if (validationAttribute.ErrorMessage == null)
        {
            ValidationAttributeHelper.SetDefaultErrorMessage(validationAttribute);
        }

        if (validationAttribute.ErrorMessage != null)
        {
            validationAttribute.ErrorMessage = stringLocalizer[validationAttribute.ErrorMessage];
        }

        return validationAttribute.GetValidationResult(validatingObject, validationContext);
    }
}