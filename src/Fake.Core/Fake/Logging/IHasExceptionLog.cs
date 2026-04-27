using Microsoft.Extensions.Logging;

namespace Fake.Logging;

public interface IHasExceptionLog
{
    void Log(ILogger logger);
}