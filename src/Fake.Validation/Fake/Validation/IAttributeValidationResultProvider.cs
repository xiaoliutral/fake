using System.ComponentModel.DataAnnotations;

namespace Fake.Validation;

public interface IAttributeValidationResultProvider
{
    ValidationResult? GetOrDefault(ValidationAttribute validationAttribute, object? validatingObject, ValidationContext validationContext);
}