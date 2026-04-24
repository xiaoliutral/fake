using System.ComponentModel.DataAnnotations;

namespace Fake.Validation;

public class DefaultAttributeValidationResultProvider : IAttributeValidationResultProvider
{
    public virtual ValidationResult? GetOrDefault(ValidationAttribute validationAttribute, object? validatingObject,
        ValidationContext validationContext)
    {
        return validationAttribute.GetValidationResult(validatingObject, validationContext);
    }
}