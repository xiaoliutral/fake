using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace Fake.Validation;

public class ObjectValidationContext([NotNull] object validatingObject)
{
    public object ValidatingObject { get; } = ThrowHelper.ThrowIfNull(validatingObject, nameof(validatingObject));

    public List<ValidationResult> Errors { get; } = new();
}