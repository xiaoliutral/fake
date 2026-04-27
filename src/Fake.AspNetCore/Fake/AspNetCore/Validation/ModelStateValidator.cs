using System.ComponentModel.DataAnnotations;
using Fake.Validation;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Fake.AspNetCore.Validation;

public class ModelStateValidator : IModelStateValidator
{
    public virtual void Validate(ModelStateDictionary modelState)
    {
        var errors = GetErrors(modelState);

        if (errors.Count != 0)
        {
            throw new FakeValidationException(
                "ModelState is not valid! See ValidationErrors for details.",
                errors
            );
        }
    }

    public virtual List<ValidationResult> GetErrors(ModelStateDictionary modelState)
    {
        var errors = new List<ValidationResult>();
        
        if (modelState.IsValid)
        {
            return [];
        }

        foreach (var state in modelState)
        {
            foreach (var error in state.Value.Errors)
            {
                errors.Add(new ValidationResult(error.ErrorMessage, [state.Key]));
            }
        }

        return errors;
    }
}