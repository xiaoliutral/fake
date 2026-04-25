using Fake.DependencyInjection;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace Fake.Validation.FluentValidation.Fake.Validation.FluentValidation;

public class FluentValidationConventionalRegistrar : DefaultServiceRegistrar
{
    protected override bool IsSkipServiceRegistration(Type type)
    {
        return GetValidatorInterfaceOrNull(type) == null || base.IsSkipServiceRegistration(type);
    }

    protected override ServiceLifetime? GetLifeTimeOrNull(Type type, DependencyAttribute? attribute)
    {
        return ServiceLifetime.Transient;
    }

    protected override List<ServiceIdentifier> GetExposedServices(Type type)
    {
        var validatorInterface = GetValidatorInterfaceOrNull(type)!;
        return
        [
            new ServiceIdentifier(type),
            new ServiceIdentifier(validatorInterface)
        ];
    }
    
    private static Type? GetValidatorInterfaceOrNull(Type type)
    {
        return type.GetInterfaces()
            .FirstOrDefault(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(IValidator<>));
    }
}
