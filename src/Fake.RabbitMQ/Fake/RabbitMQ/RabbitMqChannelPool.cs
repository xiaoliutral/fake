using System.Collections.Concurrent;
using System.Diagnostics;
using Fake.Timing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace Fake.RabbitMQ;

public class RabbitMqChannelPool(
    IRabbitMqConnectionPool rabbitMqConnectionPool,
    ILogger<RabbitMqChannelPool> logger,
    IFakeClock clock,
    IOptions<FakeRabbitMqOptions> options) : IRabbitMqChannelPool
{
    private volatile bool _isDisposed;
    protected ConcurrentDictionary<string, ChannelWrapper> Channels { get; } = new();

    public virtual IChannelAccessor Acquire(string channelName = "", string? connectionName = null)
    {
        if (_isDisposed)
        {
            throw new ObjectDisposedException(nameof(RabbitMqChannelPool));
        }

        var key = GetKey(channelName, connectionName);
        bool isNew = false;
        var wrapper = Channels.GetOrAdd(
            key,
            _ =>
            {
                isNew = true;
                return new ChannelWrapper(rabbitMqConnectionPool.Get(connectionName).CreateModel());
            }
        );

        // 与 Connection 不同，Channel 对象在 RabbitMQ 中不是线程安全的，因此需要加锁
        wrapper.Acquire();

        return new ChannelAccessor(channelName, wrapper, isNew);
    }

    public virtual bool Release(string channelName = "", string? connectionName = null)
    {
        var key = GetKey(channelName, connectionName);
        if (!Channels.TryGetValue(key, out var wrapper))
        {
            return false;
        }

        return wrapper.TryRelease();
    }

    public virtual void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        _isDisposed = true;
        var channelCount = Channels.Count;

        try
        {
            var cost = clock.MeasureExecutionTime(DoDispose);
            logger.LogInformation("Disposed Channel pool ({ChannelCount} channels) in {Elapsed}ms", channelCount, cost.TotalMilliseconds);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to dispose RabbitMQ channel pool cleanly.");
        }
        finally
        {
            Channels.Clear();
        }
    }

    private void DoDispose()
    {
        var channelWrappers = Channels.Values.ToArray();
        logger.LogInformation("Disposing Channel pool ({ChannelCount} channels)", channelWrappers.Length);

        var remainingTime = options.Value.ChannelPoolDisposeDuration;

        foreach (var channelWrapper in channelWrappers)
        {
            var timeout = remainingTime;
            var itemTime = clock.MeasureExecutionTime(() =>
            {
                try
                {
                    if (!channelWrapper.TryDispose(timeout))
                    {
                        logger.LogWarning(
                            "Timed out disposing RabbitMQ channel after {Timeout}ms. Disposal will be completed when the current lease is released.",
                            timeout.TotalMilliseconds
                        );
                    }
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Dispose channel error.");
                }
            });

            remainingTime = remainingTime > itemTime ? remainingTime - itemTime : TimeSpan.Zero;
        }
    }

    private static string GetKey(string channelName, string? connectionName)
    {
        return $"{connectionName ?? string.Empty}:{channelName}";
    }

    protected class ChannelAccessor(string name, ChannelWrapper channelWrapper, bool isNew) : IChannelAccessor
    {
        private bool _isDisposed;

        public string Name { get; } = name;
        public IModel Channel { get; } = channelWrapper.Channel;

        public bool IsNew { get; set; } = isNew;

        public void Dispose()
        {
            if (_isDisposed)
            {
                return;
            }

            _isDisposed = true;
            channelWrapper.TryRelease();
        }
    }


    protected class ChannelWrapper(IModel channel)
    {
        public IModel Channel { get; } = channel;

        private bool _isDisposeRequested;
        private bool _isDisposed;
        private volatile bool _isInUse;

        public void Acquire()
        {
            lock (this)
            {
                ThrowIfDisposed();

                while (_isInUse) // race, only one can use it
                {
                    // 释放锁，等待其他线程调用 Release 方法
                    Monitor.Wait(this);
                    ThrowIfDisposed();
                }

                _isInUse = true;
            }
        }

        public bool TryDispose(TimeSpan timeout)
        {
            bool shouldDisposeChannel = false;

            lock (this)
            {
                if (_isDisposed)
                {
                    return true;
                }

                _isDisposeRequested = true;
                Monitor.PulseAll(this);

                var stopwatch = Stopwatch.StartNew();
                while (_isInUse)
                {
                    var remaining = timeout - stopwatch.Elapsed;
                    if (remaining <= TimeSpan.Zero || !Monitor.Wait(this, remaining))
                    {
                        return false;
                    }
                }

                if (_isDisposed)
                {
                    return true;
                }

                _isDisposed = true;
                shouldDisposeChannel = true;
                Monitor.PulseAll(this);
            }

            if (shouldDisposeChannel)
            {
                Channel.Dispose();
            }

            return true;
        }

        public bool TryRelease()
        {
            bool shouldDisposeChannel = false;

            lock (this)
            {
                if (!_isInUse)
                {
                    return false;
                }

                _isInUse = false;
                if (_isDisposeRequested && !_isDisposed)
                {
                    _isDisposed = true;
                    shouldDisposeChannel = true;
                }

                Monitor.PulseAll(this); // 通知其他wait线程 release了
            }

            if (shouldDisposeChannel)
            {
                Channel.Dispose();
            }

            return true;
        }

        private void ThrowIfDisposed()
        {
            if (_isDisposeRequested || _isDisposed)
            {
                throw new ObjectDisposedException(nameof(IModel));
            }
        }
    }
}
