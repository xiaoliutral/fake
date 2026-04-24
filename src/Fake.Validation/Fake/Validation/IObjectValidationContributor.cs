namespace Fake.Validation;

public interface IObjectValidationContributor
{
    Task AddErrorsAsync(ObjectValidationContext context);
}