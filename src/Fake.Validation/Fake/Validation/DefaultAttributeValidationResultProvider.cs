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
        var resourceSource = localizationOptions.Value
            .AssemblyResources.GetOrDefault(validationContext.ObjectType.Assembly);
        if (resourceSource == null)
        {
            return validationAttribute.GetValidationResult(validatingObject, validationContext);
        }

        if (validationAttribute.ErrorMessage == null)
        {
            ValidationAttributeHelper.SetDefaultErrorMessage(validationAttribute);
        }

        if (validationAttribute.ErrorMessage != null)
        {
            validationAttribute.ErrorMessage = stringLocalizerFactory.Create(resourceSource)[validationAttribute.ErrorMessage];
        }

        return validationAttribute.GetValidationResult(validatingObject, validationContext);
    }
}