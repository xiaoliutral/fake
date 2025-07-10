using Fake.Threading;

namespace Fake.AspNetCore.Http;

public class HttpContextCancellationTokenProvider(
    IAmbientScopeProvider<CancellationToken> ambientScopeProvider,
    IHttpContextAccessor httpContextAccessor)
    : CancellationTokenProviderBase(ambientScopeProvider)
{
    public override CancellationToken Token
    {
        get
        {
            if (CurrentToken == default) return CurrentToken;
            return httpContextAccessor.HttpContext?.RequestAborted ?? CancellationToken.None;
        }
    }
}