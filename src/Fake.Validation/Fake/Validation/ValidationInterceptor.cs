using System.ComponentModel.DataAnnotations;
using Fake.DynamicProxy;
using Fake.Helpers;
using Microsoft.Extensions.Options;

namespace Fake.Validation;

public class ValidationInterceptor(IOptions<FakeValidationOptions> options,IObjectValidator objectValidator) : IFakeInterceptor
{
    private readonly FakeValidationOptions _validationOptions = options.Value;
    
    public virtual async Task InterceptAsync(IFakeMethodInvocation invocation)
    {
        await ValidateAsync(invocation);
        await invocation.ProcessAsync();
    }

    public virtual async Task ValidateAsync(IFakeMethodInvocation invocation)
    {
        if (!invocation.Method.IsPublic) return;
        if (invocation.Arguments.IsNullOrEmpty()) return;
        if (ReflectionHelper.GetAttributeOrDefault<DisableValidationAttribute>(invocation.Method) != null) return;

        var errors = new List<ValidationResult>();
        var parameters = invocation.Method.GetParameters();
        for (var i = 0; i < parameters.Length; i++)
        {
            var argument = parameters[i];
            var parameterValue = invocation.Arguments[i];
            
            if (_validationOptions.IgnoredTypes.Contains(parameterValue.GetType())) continue;
            
            var allowNulls = argument.IsOptional ||
                             argument.IsOut ||
                             TypeHelper.IsPrimitiveExtended(argument.ParameterType, includeEnums: true);

            var err = await objectValidator.GetErrorsAsync(parameterValue, argument.Name, allowNulls);
            errors.AddRange(err);
        }

        if (errors.Any())
        {
            throw new FakeValidationException(
                "Method arguments are not valid! See ValidationErrors for details.",
                errors
            );
        }
    }
}