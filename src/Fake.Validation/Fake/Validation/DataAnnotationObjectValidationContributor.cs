using System.Collections;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Fake.DependencyInjection;
using Fake.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Fake.Validation;

public class DataAnnotationObjectValidationContributor(
    IOptions<FakeValidationOptions> options,
    IServiceProvider serviceProvider) : IObjectValidationContributor, ITransientDependency
{
    private readonly FakeValidationOptions _validationOptions = options.Value;

    public virtual Task AddErrorsAsync(ObjectValidationContext context)
    {
        ValidateObjectRecursively(context.Errors, context.ValidatingObject, currentDepth: 1);

        return Task.CompletedTask;
    }

    protected virtual void ValidateObjectRecursively(List<ValidationResult> errors, object? validatingObject,
        int currentDepth)
    {
        if (currentDepth > _validationOptions.MaxRecursiveParameterValidationDepth) return;
        if (validatingObject is null) return;
        
        AddErrors(errors, validatingObject);
        
        if (TypeHelper.IsPrimitiveExtended(validatingObject.GetType())) return;
        if (_validationOptions.IgnoredTypes.Any(t => t.IsInstanceOfType(validatingObject))) return;


        // Validate items of enumerable
        if (validatingObject is IEnumerable enumerable)
        {
            if (enumerable is IQueryable) return;

            foreach (var item in enumerable)
            {
                ValidateObjectRecursively(errors, item, currentDepth + 1);
            }

            return;
        }

        var properties = TypeDescriptor.GetProperties(validatingObject).Cast<PropertyDescriptor>();
        foreach (var property in properties)
        {
            if (property.Attributes.OfType<DisableValidationAttribute>().Any()) continue;

            ValidateObjectRecursively(errors, property.GetValue(validatingObject), currentDepth + 1);
        }
    }

    public void AddErrors(List<ValidationResult> errors, object validatingObject)
    {
        var properties = TypeDescriptor.GetProperties(validatingObject).Cast<PropertyDescriptor>();

        // ValidationAttribute
        foreach (var property in properties)
        {
            AddPropertyErrors(validatingObject, property, errors);
        }

        // IValidatableObject
        if (validatingObject is IValidatableObject validatableObject)
        {
            errors.AddRange(
                validatableObject.Validate(new ValidationContext(validatableObject, serviceProvider, null))
            );
        }
    }

    protected virtual void AddPropertyErrors(object validatingObject, PropertyDescriptor property,
        List<ValidationResult> errors)
    {
        var validationAttributes = property.Attributes.OfType<ValidationAttribute>().ToArray();
        if (validationAttributes.IsNullOrEmpty())
        {
            return;
        }

        var validationContext = new ValidationContext(validatingObject, serviceProvider, null)
        {
            DisplayName = property.DisplayName,
            MemberName = property.Name
        };

        var attributeValidationResultProvider =
            serviceProvider.GetRequiredService<IAttributeValidationResultProvider>();
        foreach (var attribute in validationAttributes)
        {
            var result = attributeValidationResultProvider.GetOrDefault(attribute, property.GetValue(validatingObject),
                validationContext);
            if (result != null)
            {
                errors.Add(result);
            }
        }
    }
}