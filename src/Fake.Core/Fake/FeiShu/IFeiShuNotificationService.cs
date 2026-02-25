using Fake.Logging;
using Microsoft.Extensions.Logging;

namespace Fake.FeiShu;

public interface IFeiShuNotificationService : IDisposable
{
    public void Enqueue(string content, LogLevel logLevel = LogLevel.Information);
    
    public Task SendAsync(string content, LogLevel logLevel, CancellationToken cancellationToken);
    
    public Task SendAsync(List<NoticeMessage> messages, CancellationToken cancellationToken);
}