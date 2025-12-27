using System.Text;
using System.Text.Json;
using System.Threading.Channels;
using Microsoft.Extensions.Logging;

namespace Fake.Helpers;

public static class LogHelper
{
    private static FeiShuNoticeOptions? _options;
    private static ILogger? _logger;
    
    private static readonly HttpClient HttpClient = new(new SocketsHttpHandler
    {
        MaxConnectionsPerServer = 5,
        PooledConnectionLifetime = TimeSpan.FromMinutes(5)
    });
    
    private static readonly Channel<NoticeMessage> Queue = Channel.CreateBounded<NoticeMessage>(
        new BoundedChannelOptions(500)
        {
            FullMode = BoundedChannelFullMode.DropOldest,
            SingleReader = true,
            SingleWriter = false
        });
    
    private static CancellationTokenSource? _cts;
    private static Task? _consumerTask;

    public static void Init(FeiShuNoticeOptions options, ILogger? logger = null)
    {
        _options = options;
        _logger = logger;
        HttpClient.Timeout = TimeSpan.FromSeconds(options.Timeout);
        
        _cts = new CancellationTokenSource();
        _consumerTask = Task.Run(() => ConsumeAsync(_cts.Token));
    }

    public static async Task ShutdownAsync()
    {
        Queue.Writer.Complete();
        if (_cts != null)
        {
            await _cts.CancelAsync();
            _cts.Dispose();
        }
        if (_consumerTask != null)
        {
            await _consumerTask;
        }
    }

    public static void Info(string msg, bool isSend = false)
    {
        _logger?.LogInformation(msg);
        if (isSend) Enqueue(msg, "Info");
    }

    public static void Warn(string msg, bool isSend = false)
    {
        _logger?.LogWarning(msg);
        if (isSend) Enqueue(msg, "Warn");
    }

    public static void Error(string msg, bool isSend = true)
    {
        _logger?.LogError(msg);
        if (isSend) Enqueue(msg, "Error");
    }

    public static void Error(Exception error, bool isSend = true)
    {
        _logger?.LogError(error, error.Message);
        if (isSend) Enqueue(error.ToString(), "Error");
    }

    private static void Enqueue(string content, string subTitle)
    {
#if DEBUG
        return;
#endif
        if (!Queue.Writer.TryWrite(new NoticeMessage(content, subTitle, DateTime.Now)))
        {
            _logger?.LogWarning("飞书通知队列已满，消息被丢弃");
        }
    }

    private static async Task ConsumeAsync(CancellationToken cancellationToken)
    {
        try
        {
            await foreach (var msg in Queue.Reader.ReadAllAsync(cancellationToken))
            {
                try
                {
                    await SendToFeiShuAsync(msg.Content, msg.SubTitle);
                    await Task.Delay(100, cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger?.LogWarning(ex, "飞书通知发送失败: {Content}", 
                        msg.Content[..Math.Min(100, msg.Content.Length)]);
                }
            }
        }
        catch (OperationCanceledException)
        {
            // 正常关闭
        }
    }

    private static async Task SendToFeiShuAsync(string content, string subTitle)
    {
        if (_options?.Webhook is not { Length: > 0 }) return;

        var title = string.IsNullOrWhiteSpace(subTitle)
            ? _options.TitlePrefix
            : $"{_options.TitlePrefix}_{subTitle}";

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
                        content = new object[]
                        {
                            new object[] { new { tag = "text", text = content + Environment.NewLine } }
                        }
                    }
                }
            }
        };

        using var httpContent = new StringContent(
            JsonSerializer.Serialize(message), Encoding.UTF8, "application/json");
        await HttpClient.PostAsync(_options.Webhook, httpContent);
    }

    private record NoticeMessage(string Content, string SubTitle, DateTime CreatedAt);
}

public class FeiShuNoticeOptions
{
    public string Webhook { get; set; } = string.Empty;
    public string TitlePrefix { get; set; } = string.Empty;
    public int Timeout { get; set; } = 20;
}
