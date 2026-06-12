using System.Reflection;
using Fake.Helpers;
using Fake.Timing;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Fake.AspNetCore.Newtonsoft;

public class FakeCamelCasePropertyNamesContractResolver: CamelCasePropertyNamesContractResolver
{
    private readonly FakeDateTimeConverter _dateTimeConverter;

    public FakeCamelCasePropertyNamesContractResolver(FakeDateTimeConverter dateTimeConverter)
    {
        _dateTimeConverter = dateTimeConverter;
        NamingStrategy = new CamelCaseNamingStrategy
        {
            ProcessDictionaryKeys = false
        };
    }
    
    protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
    {
        var property = base.CreateProperty(member, memberSerialization);
        
        if ((property.PropertyType == typeof(DateTime) || property.PropertyType == typeof(DateTime?)) &&
            ReflectionHelper.GetAttributeOrDefault<DisableClockNormalizationAttribute>(member) == null)
        {
            property.Converter = _dateTimeConverter;
        }
        
        return property;
    }
}