using Fake.DependencyInjection;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace Fake.Validation.FluentValidation.Fake.Validation.FluentValidation;

public class FluentValidationConventionalRegistrar: DefaultServiceRegistrar
{
    protected override bool IsSkipServiceRegistration(Type type)
    {
        return !type.GetInterfaces().Any(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(IValidator<>)) ||
               base.IsSkipServiceRegistration(type);
    }

    protected override ServiceLifetime? GetLifeTimeOrNull(Type type, DependencyAttribute? attribute)
    {
        return ServiceLifetime.Transient;
    }

    protected override List<Type> GetExposedServices(Type type)
    {
        return
        [
            type,
            typeof(IValidator<>).MakeGenericType(GetFirstGenericArgumentOrNull(type, 1)!)
        ];
    }
    
    private static Type? GetFirstGenericArgumentOrNull(Type type, int depth)
    {
        const int maxFindDepth = 5;

        if (depth >= maxFindDepth)
        {
            return null;
        }

        if (type.IsGenericType && type.GetGenericArguments().Length >= 1)
        {
            return type.GetGenericArguments()[0];
        }

        return GetFirstGenericArgumentOrNull(type.BaseType!, depth + 1);
    }
}