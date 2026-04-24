using Fake.Collections;

namespace Fake.Validation;

public class FakeValidationOptions
{
    public List<Type> IgnoredTypes { get; } = [];
    
    public ITypeList<IObjectValidationContributor> Contributors { get; set; } = new TypeList<IObjectValidationContributor>();
}