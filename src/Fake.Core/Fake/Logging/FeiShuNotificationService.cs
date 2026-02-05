using System.Text;
using System.Text.Json;
using System.Threading.Channels;
using Microsoft.Extensions.Logging;

namespace Fake.Logging;

/// <summary>
/// 飞书通知服务（从 LogHelper 重构而来）
/// </summary>
internal sealed class FeiShuNotificationService : IDisposable
{
    private readonly FeiShuNoticeOptions _options;
    private readonly HttpClient _httpClient;
    private readonly Channel<NoticeMessage> _queue;
    private readonly CancellationTokenSource _cts;
    private readonly Task _consumerTask;

    public FeiShuNotificationService(FeiShuNoticeOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _options.Validate();

        _httpClient = new HttpClient(new SocketsHttpHandler
        {
            MaxConnectionsPerServer = 5,
            PooledConnectionLifetime = TimeSpan.FromMinutes(5),
            ConnectTimeout = TimeSpan.FromSeconds(5)
        })
        {
            Timeout = TimeSpan.FromSeconds(options.Timeout)
        };

        _queue = Channel.CreateBounded<NoticeMessage>(
            new BoundedChannelOptions(options.QueueCapacity)
            {
                FullMode = BoundedChannelFullMode.DropOldest,
                SingleReader = true,
                SingleWriter = false
            });

        _cts = new CancellationTokenSource();
        _consumerTask = Task.Run(() => ConsumeAsync(_cts.Token));
    }

    public void Enqueue(string content, string subTitle)
    {
        // 截断超长消息
        const int maxLength = 4000;
        if (content.Length > maxLength)
        {
            content = content[..maxLength] + "\n...(消息已截断)";
        }

        _queue.Writer.TryWrite(new NoticeMessage(content, subTitle, DateTime.Now));
    }

    private async Task ConsumeAsync(CancellationToken cancellationToken)
    {
        var batch = new List<NoticeMessage>();
        var batchTimer = new PeriodicTimer(TimeSpan.FromSeconds(_options.BatchIntervalSeconds));

        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                // 尝试读取消息直到达到批量大小
                while (batch.Count < _options.BatchSize)
                {
                    if (_queue.Reader.TryRead(out var msg))
                    {
                        batch.Add(msg);
                    }
                    else
                    {
                        break;
                    }
                }

                // 等待定时器或有新消息
                if (batch.Count == 0)
                {
                    try
                    {
                        await batchTimer.WaitForNextTickAsync(cancellationToken);
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                    continue;
                }

                // 发送批量消息
                if (batch.Count >= _options.BatchSize || await batchTimer.WaitForNextTickAsync(cancellationToken))
                {
                    if (batch.Count > 0)
                    {
                        await SendBatchWithRetryAsync(batch, cancellationToken);
                        batch.Clear();
                    }
                }
            }

            // 关闭时发送剩余消息
            if (batch.Count > 0)
            {
                await SendBatchWithRetryAsync(batch, CancellationToken.None);
            }
        }
        catch (OperationCanceledException)
        {
            // 正常关闭
            if (batch.Count > 0)
            {
                try
                {
                    await SendBatchWithRetryAsync(batch, CancellationToken.None);
                }
                catch
                {
                    // 忽略关闭时的错误
                }
            }
        }
        finally
        {
            batchTimer.Dispose();
        }
    }

    private async Task SendBatchWithRetryAsync(List<NoticeMessage> messages, CancellationToken cancellationToken)
    {
        if (messages.Count == 0) return;

        const int maxRetries = 3;
        var retryDelays = new[] { 1000, 2000, 5000 };

        for (int attempt = 0; attempt < maxRetries; attempt++)
        {
            try
            {
                await SendBatchToFeiShuAsync(messages, cancellationToken);
                return;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch
            {
                if (attempt == maxRetries - 1)
                    break; // 最后一次尝试失败，放弃

                try
                {
                    await Task.Delay(retryDelays[attempt], cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
            }
        }
    }

    private async Task SendBatchToFeiShuAsync(List<NoticeMessage> messages, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_options.Webhook)) return;
        if (messages.Count == 0) return;

        var grouped = messages.GroupBy(m => m.SubTitle).ToList();
        
        var title = messages.Count == 1 
            ? $"{_options.TitlePrefix}_{messages[0].SubTitle}"
            : $"{_options.TitlePrefix}_批量通知 ({messages.Count}条)";

        var contentLines = new List<object[]>();
        
        foreach (var group in grouped.OrderBy(g => GetLevelPriority(g.Key)))
        {
            var levelEmoji = GetLevelEmoji(group.Key);
            var count = group.Count();
            
            if (grouped.Count > 1)
            {
                contentLines.Add(new object[] 
                { 
                    new { tag = "text", text = $"\n{levelEmoji} {group.Key} ({count}条):\n" }
                });
            }
            
            foreach (var msg in group)
            {
                var timestamp = msg.CreatedAt.ToString("HH:mm:ss");
                var preview = msg.Content.Length > 200 
                    ? msg.Content[..200] + "..." 
                    : msg.Content;
                
                contentLines.Add(new object[] 
                { 
                    new { tag = "text", text = $"[{timestamp}] {preview}\n" }
                });
            }
        }

        var message = new
        {
            msg_type = "post",
            content = new
            {
                post = new
                {
                    zh_cn = new
                    {
                        title,
                        content = contentLines.ToArray()
                    }
                }
            }
        };

        using var httpContent = new StringContent(
            JsonSerializer.Serialize(message), Encoding.UTF8, "application/json");
        
        using var response = await _httpClient.PostAsync(_options.Webhook, httpContent, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    private static string GetLevelEmoji(string level) => level switch
    {
        "Critical" => "💀",
        "Error" => "🔴",
        "Warn" => "🟡",
        "Info" => "🔵",
        _ => "⚪"
    };

    private static int GetLevelPriority(string level) => level switch
    {
        "Critical" => 0,
        "Error" => 1,
        "Warn" => 2,
        "Info" => 3,
        _ => 4
    };

    public void Dispose()
    {
        _queue.Writer.Complete();
        _cts.Cancel();
        
        try
        {
            _consumerTask.Wait(TimeSpan.FromSeconds(10));
        }
        catch
        {
            // 忽略超时
        }
        
        _cts.Dispose();
        _httpClient.Dispose();
    }

    private record NoticeMessage(string Content, string SubTitle, DateTime CreatedAt);
}

public class FeiShuNoticeOptions
{
    public string Webhook { get; set; } = string.Empty;
    public string TitlePrefix { get; set; } = string.Empty;
    public int Timeout { get; set; } = 20;
    public int QueueCapacity { get; set; } = 500;
    public int BatchSize { get; set; } = 10;
    public int BatchIntervalSeconds { get; set; } = 5;
    
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Webhook))
            throw new ArgumentException("Webhook 不能为空", nameof(Webhook));
        
        if (Timeout <= 0)
            throw new ArgumentException("Timeout 必须大于 0", nameof(Timeout));
        
        if (QueueCapacity <= 0)
            throw new ArgumentException("QueueCapacity 必须大于 0", nameof(QueueCapacity));
        
        if (BatchSize <= 0)
            throw new ArgumentException("BatchSize 必须大于 0", nameof(BatchSize));
        
        if (BatchIntervalSeconds <= 0)
            throw new ArgumentException("BatchIntervalSeconds 必须大于 0", nameof(BatchIntervalSeconds));
    }
}
