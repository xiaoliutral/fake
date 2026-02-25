using System.Text;
using System.Text.Json;
using System.Threading.Channels;
using Fake.Timing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Fake.FeiShu;

/// <summary>
/// 飞书通知服务
/// </summary>
public sealed class FeiShuNotificationService : IFeiShuNotificationService
{
    private readonly ILogger<FeiShuNotificationService> _logger;
    private readonly IFakeClock _fakeClock;
    private readonly FeiShuNoticeOptions _options;
    private readonly HttpClient _httpClient;
    private readonly Channel<NoticeMessage> _queue;
    private readonly CancellationTokenSource _cts;
    private readonly Task _consumerTask;
    private readonly string _subTitle;

    public FeiShuNotificationService(IOptionsMonitor<FeiShuNoticeOptions> options, ILogger<FeiShuNotificationService> logger,
        IConfiguration config, IFakeClock fakeClock)
    {
        _logger = logger;
        _fakeClock = fakeClock;
        _options = options.CurrentValue ?? throw new ArgumentNullException(nameof(options));
        _options.Validate();

        _httpClient = new HttpClient(new SocketsHttpHandler
        {
            MaxConnectionsPerServer = 5,
            PooledConnectionLifetime = TimeSpan.FromMinutes(5),
            ConnectTimeout = TimeSpan.FromSeconds(5)
        })
        {
            Timeout = TimeSpan.FromSeconds(_options.Timeout)
        };

        _queue = Channel.CreateBounded<NoticeMessage>(
            new BoundedChannelOptions(_options.QueueCapacity)
            {
                FullMode = BoundedChannelFullMode.DropOldest,
                SingleReader = true,
                SingleWriter = false
            });

        _cts = new CancellationTokenSource();
        _consumerTask = Task.Run(() => ConsumeAsync(_cts.Token));
        _subTitle = config["ENVIRONMENT"] is null ? "" : $"_{config["ENVIRONMENT"]}";
    }

    public void Enqueue(string content, LogLevel logLevel = LogLevel.Information)
    {
        // 截断超长消息
        if (content.Length > _options.MaxLength)
        {
            content = content[.._options.MaxLength] + "...";
        }

        _queue.Writer.TryWrite(new NoticeMessage(content, logLevel, _fakeClock.Now));
    }

    public async Task SendAsync(string content, LogLevel logLevel, CancellationToken cancellationToken)
    {
        await SendBatchWithRetryAsync([new(content, logLevel, _fakeClock.Now)], cancellationToken);
    }

    public async Task SendAsync(List<NoticeMessage> messages, CancellationToken cancellationToken)
    {
        await SendBatchWithRetryAsync(messages, cancellationToken);
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

        var retryDelays = _options.RetryDelays;
        int maxRetries = retryDelays.Length;

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
            catch (Exception ex)
            {
                if (attempt == maxRetries - 1)
                {
                    _logger.LogWarning($"已达到最大重试次数{maxRetries}，仍然无法发送到飞书{_options} \n{ex}");
                    break; // 最后一次尝试失败，放弃
                }

                await Task.Delay(retryDelays[attempt], cancellationToken);
            }
        }
    }

    private async Task SendBatchToFeiShuAsync(List<NoticeMessage> messages, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_options.Webhook)) return;
        if (messages.Count == 0) return;

        var grouped = messages.GroupBy(m => m.LogLevel).ToList();

        var title = $"{_options.Title}{_subTitle} [{messages.Count}]";

        var contentLines = new List<object[]>();

        foreach (var group in grouped.OrderBy(g => GetLevelPriority(g.Key)))
        {
            var levelEmoji = GetLevelEmoji(group.Key);
            var count = group.Count();

            contentLines.Add([
                new { tag = "text", text = $"{levelEmoji}{group.Key} [{count}]:\n" }
            ]);

            foreach (var msg in group)
            {
                var timestamp = msg.CreatedAt.ToString("HH:mm:ss");
                var preview = msg.Content;

                contentLines.Add([
                    new { tag = "text", text = $"[{timestamp}] {preview}\n" }
                ]);
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

    private static string GetLevelEmoji(LogLevel level) => level switch
    {
        LogLevel.Critical => "💀",
        LogLevel.Error => "🔴",
        LogLevel.Warning => "🟡",
        LogLevel.Information => "🔵",
        _ => "⚪"
    };

    private static int GetLevelPriority(LogLevel level) => level switch
    {
        LogLevel.Critical => 0,
        LogLevel.Error => 1,
        LogLevel.Warning => 2,
        LogLevel.Information => 3,
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
}