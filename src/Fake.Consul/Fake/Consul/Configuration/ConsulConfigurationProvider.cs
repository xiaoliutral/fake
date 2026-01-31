using System.Net;
using Consul;
using Fake.Consul.Internal;
using Fake.SyncEx;
using Microsoft.Extensions.Configuration;

namespace Fake.Consul.Configuration;

public class ConsulConfigurationProvider(IConsulClient consulClient, ConsulConfigurationSource source)
    : ConfigurationProvider, IDisposable
{
    private readonly CancellationTokenSource _cancellationTokenSource = new();
    private ulong _lastIndex;
    private Task? _pollTask;
    private bool _disposed;

    public override void Load()
    {
        // 轮询任务已经启动不需要再load了
        if (_pollTask != null)
        {
            return;
        }

        var cancellationToken = _cancellationTokenSource.Token;

        SyncContext.Run(() => DoLoadAsync(cancellationToken));

        // Polling starts after the initial load to ensure no concurrent access to the key from this instance
        if (source.ReloadOnChange)
        {
            _pollTask = Task.Run(() => PollingLoop(cancellationToken), cancellationToken);
        }
    }

    private async Task DoLoadAsync(CancellationToken cancellationToken)
    {
        try
        {
            var result = await GetKvPairs(false, cancellationToken);

            if (result is { StatusCode: HttpStatusCode.OK, Response: not null })
            {
                Data = result.Response
                    .ConvertToConfig(source.Key, source.Parser)
                    .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
            }

            SetLastIndex(result);
        }
        catch (Exception exception)
        {
            await source.OnLoadException.Invoke(exception);
        }
    }

    private async Task<QueryResult<KVPair>> GetKvPairs(bool waitForChange, CancellationToken cancellationToken)
    {
        /*
         * wait_index通常与wait_time配合使用。客户端发送GET请求（例如读取KV存储），携带上一次接收到的X-Consul-Index值。
            1) 被请求资源发生变化, 请求直接返回新的X-Consul-Index和新的body体
            2) 被请求资源未发生变化，则请求会一直阻塞，直到wait指定的时间耗尽，请求最终会返回。只是此时X-Consul-Index不发生变化，body体不变。
            3) 客户端应当在收到响应后，更新其保存的 X-Consul-Index，并将新的值在下一次请求中作为 wait_index 参数传入。
            长轮训减少了频繁轮训的所造成的不必要的带宽和服务器资源开销，用在服务发现上，即时性也能有所保证，还是很合适的
         */
        var queryOptions = new QueryOptions
        {
            WaitTime = source.WaitTime, // consul会尝试等待的时间
            WaitIndex = waitForChange ? _lastIndex : 0 // 版本号X-Consul-Index
        };

        var result =
            await consulClient
                .KV
                .Get(source.Key, queryOptions, cancellationToken);

        return result.StatusCode switch
        {
            HttpStatusCode.OK => result,
            HttpStatusCode.NotFound => result,
            _ => throw new FakeException($"Error loading configuration from consul. Status code: {result.StatusCode}.")
        };
    }

    private async Task PollingLoop(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var result = await GetKvPairs(true, cancellationToken).ConfigureAwait(false);

                if (result.LastIndex > _lastIndex)
                {
                    Data = result.Response
                        .ConvertToConfig(source.Key, source.Parser)
                        .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

                    // 通知ConfigurationManager重新加载
                    OnReload();
                }

                SetLastIndex(result);
            }
            catch (Exception exception) when (!cancellationToken.IsCancellationRequested)
            {
                await source.OnWatchException.Invoke(exception);
            }
        }
    }

    private void SetLastIndex(QueryResult result)
    {
        _lastIndex = result.LastIndex == 0
            ? 1
            : result.LastIndex < _lastIndex
                ? 0
                : result.LastIndex;
    }
    
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _cancellationTokenSource.Cancel();
        _cancellationTokenSource.Dispose();
        _disposed = true;
    }
}