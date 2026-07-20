using System.Reflection;
using Fake.Helpers;
using Fake.Timing;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Fake.AspNetCore.Newtonsoft;

public class FakeDefaultContractResolver(FakeDateTimeConverter dateTimeConverter) : DefaultContractResolver
{
    protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
    {
        var property = base.CreateProperty(member, memberSerialization);

        if (property.Converter == null &&
            (property.PropertyType == typeof(DateTime) || property.PropertyType == typeof(DateTime?)) &&
            ReflectionHelper.GetAttributeOrDefault<DisableClockNormalizationAttribute>(member) == null)
        {
            property.Converter = dateTimeConverter;
        }

        return property;
    }
}