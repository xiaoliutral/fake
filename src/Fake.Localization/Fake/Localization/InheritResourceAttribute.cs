namespace Fake.Localization;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class InheritResourceAttribute(params Type[] resourceTypes) : Attribute
{
    public Type[] ResourceTypes { get; } = resourceTypes;
}