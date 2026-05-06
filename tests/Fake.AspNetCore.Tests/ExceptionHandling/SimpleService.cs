using Fake.Application;
using Fake.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Fake.AspNetCore.Tests.ExceptionHandling;


public class SimpleService : ApplicationService
{
    [Authorize]
    [HttpGet]
    public virtual void AuthorizationException()
    {
        throw new FakeAuthorizationException("授权失败");
    }

    [HttpGet("ex")]
    [HttpGet("throw-business-exception")]
    public virtual void ThrowBusinessException()
    {
        throw new BusinessException("Fake.AspNetCore.Tests:Hi", "xiaolipro");
    }
}