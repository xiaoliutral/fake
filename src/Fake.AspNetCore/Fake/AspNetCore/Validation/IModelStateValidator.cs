using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Fake.AspNetCore.Validation;

public interface IModelStateValidator
{
    void Validate(ModelStateDictionary modelState);
}