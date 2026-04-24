using System.ComponentModel.DataAnnotations;
using Fake.Validation;
using Microsoft.Extensions.Localization;

namespace Fake.AspNetCore.Mvc.Validation;

public class FakeMvcAttributeValidationResultProvider : DefaultAttributeValidationResultProvider
{
    private readonly AbpMvcDataAnnotationsLocalizationOptions _abpMvcDataAnnotationsLocalizationOptions;
    private readonly IStringLocalizerFactory _stringLocalizerFactory;

    public AbpMvcAttributeValidationResultProvider(
        IOptions<AbpMvcDataAnnotationsLocalizationOptions> abpMvcDataAnnotationsLocalizationOptions,
        IStringLocalizerFactory stringLocalizerFactory)
    {
        _abpMvcDataAnnotationsLocalizationOptions = abpMvcDataAnnotationsLocalizationOptions.Value;
        _stringLocalizerFactory = stringLocalizerFactory;
    }

    public override ValidationResult? GetOrDefault(ValidationAttribute validationAttribute, object? validatingObject, ValidationContext validationContext)
    {
        var resourceSource = _abpMvcDataAnnotationsLocalizationOptions.AssemblyResources.GetOrDefault(validationContext.ObjectType.Assembly);
        if (resourceSource == null)
        {
            return base.GetOrDefault(validationAttribute, validatingObject, validationContext);
        }

        if (validationAttribute.ErrorMessage == null)
        {
            ValidationAttributeHelper.SetDefaultErrorMessage(validationAttribute);
        }

        if (validationAttribute.ErrorMessage != null)
        {
            validationAttribute.ErrorMessage = _stringLocalizerFactory.Create(resourceSource)[validationAttribute.ErrorMessage];
        }

        return base.GetOrDefault(validationAttribute, validatingObject, validationContext);
    }
}