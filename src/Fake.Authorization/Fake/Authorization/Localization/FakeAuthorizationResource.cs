using Fake.Localization;

namespace Fake.Authorization.Localization;

[LocalizationResourceName("FakeAuthorization")]
public class FakeAuthorizationResource
{
    public static readonly string NotPassedAuthentication = "Fake.Authorization:010001";
    public static readonly string GivenPolicyHasNotGranted = "Fake.Authorization:010002";
}