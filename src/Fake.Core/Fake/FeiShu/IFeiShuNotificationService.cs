using Fake.Logging;
using Microsoft.Extensions.Logging;

namespace Fake.FeiShu;

public interface IFeiShuNotificationService : IDisposable
{
    public void Enqueue(string content, LogLevel logLevel = LogLevel.Information);
}