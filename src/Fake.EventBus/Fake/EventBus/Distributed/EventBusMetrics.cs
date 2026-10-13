using System.Collections.Concurrent;
using System.Diagnostics.Metrics;

namespace Fake.EventBus.Distributed;

/// <summary>
/// 事件总线指标：System.Diagnostics.Metrics + 内存时间线（供 Dashboard 实时图表）。
/// </summary>
public sealed class EventBusMetrics
{
    public const string MeterName = "Fake.EventBus";

    private readonly Meter _meter;
    private readonly Counter<long> _outboxEnqueued;
    private readonly Counter<long> _outboxDispatched;
    private readonly Counter<long> _outboxDispatchFailed;
    private readonly Counter<long> _outboxScanRetry;
    private readonly Counter<long> _inboxProcessed;
    private readonly Counter<long> _inboxDuplicate;
    private readonly Counter<long> _inboxFailed;

    private long _enqueuedTotal;
    private long _dispatchedImmediateTotal;
    private long _dispatchedScanTotal;
    private long _dispatchFailedTotal;
    private long _scanRetryTotal;
    private long _inboxProcessedTotal;
    private long _inboxDuplicateTotal;
    private long _inboxFailedTotal;

    private readonly ConcurrentQueue<MetricSample> _timeline = new();
    private readonly int _timelineCapacity;

    public EventBusMetrics(int timelineCapacity = 512)
    {
        _timelineCapacity = timelineCapacity;
        _meter = new Meter(MeterName);
        _outboxEnqueued = _meter.CreateCounter<long>("outbox_enqueued");
        _outboxDispatched = _meter.CreateCounter<long>("outbox_dispatched");
        _outboxDispatchFailed = _meter.CreateCounter<long>("outbox_dispatch_failed");
        _outboxScanRetry = _meter.CreateCounter<long>("outbox_scan_retry");
        _inboxProcessed = _meter.CreateCounter<long>("inbox_processed");
        _inboxDuplicate = _meter.CreateCounter<long>("inbox_duplicate");
        _inboxFailed = _meter.CreateCounter<long>("inbox_failed");
    }

    public void RecordOutboxEnqueued(string eventType)
    {
        Interlocked.Increment(ref _enqueuedTotal);
        _outboxEnqueued.Add(1, new KeyValuePair<string, object?>("event_type", eventType));
        Push("outbox_enqueued", eventType);
    }

    public void RecordOutboxDispatched(string eventType, string source)
    {
        if (source == "scan")
            Interlocked.Increment(ref _dispatchedScanTotal);
        else
            Interlocked.Increment(ref _dispatchedImmediateTotal);

        _outboxDispatched.Add(1,
            new KeyValuePair<string, object?>("event_type", eventType),
            new KeyValuePair<string, object?>("dispatch_source", source));
        Push("outbox_dispatched", eventType, source);
    }

    public void RecordOutboxDispatchFailed(string eventType, string source)
    {
        Interlocked.Increment(ref _dispatchFailedTotal);
        _outboxDispatchFailed.Add(1,
            new KeyValuePair<string, object?>("event_type", eventType),
            new KeyValuePair<string, object?>("dispatch_source", source));
        Push("outbox_dispatch_failed", eventType, source);
    }

    public void RecordOutboxScanRetry(string eventType)
    {
        Interlocked.Increment(ref _scanRetryTotal);
        _outboxScanRetry.Add(1, new KeyValuePair<string, object?>("event_type", eventType));
        Push("outbox_scan_retry", eventType);
    }

    public void RecordInboxProcessed(string eventType)
    {
        Interlocked.Increment(ref _inboxProcessedTotal);
        _inboxProcessed.Add(1, new KeyValuePair<string, object?>("event_type", eventType));
        Push("inbox_processed", eventType);
    }

    public void RecordInboxDuplicate(string eventType)
    {
        Interlocked.Increment(ref _inboxDuplicateTotal);
        _inboxDuplicate.Add(1, new KeyValuePair<string, object?>("event_type", eventType));
        Push("inbox_duplicate", eventType);
    }

    public void RecordInboxFailed(string eventType)
    {
        Interlocked.Increment(ref _inboxFailedTotal);
        _inboxFailed.Add(1, new KeyValuePair<string, object?>("event_type", eventType));
        Push("inbox_failed", eventType);
    }

    public EventBusMetricSnapshot GetSnapshot() => new(
        Interlocked.Read(ref _enqueuedTotal),
        Interlocked.Read(ref _dispatchedImmediateTotal),
        Interlocked.Read(ref _dispatchedScanTotal),
        Interlocked.Read(ref _dispatchFailedTotal),
        Interlocked.Read(ref _scanRetryTotal),
        Interlocked.Read(ref _inboxProcessedTotal),
        Interlocked.Read(ref _inboxDuplicateTotal),
        Interlocked.Read(ref _inboxFailedTotal),
        _timeline.ToArray());

    private void Push(string name, string eventType, string? source = null)
    {
        _timeline.Enqueue(new MetricSample(DateTime.UtcNow, name, eventType, source));
        while (_timeline.Count > _timelineCapacity && _timeline.TryDequeue(out _))
        {
        }
    }
}

public sealed record MetricSample(DateTime UtcTimestamp, string Name, string EventType, string? Source);

public sealed record EventBusMetricSnapshot(
    long OutboxEnqueued,
    long OutboxDispatchedImmediate,
    long OutboxDispatchedScan,
    long OutboxDispatchFailed,
    long OutboxScanRetry,
    long InboxProcessed,
    long InboxDuplicate,
    long InboxFailed,
    IReadOnlyList<MetricSample> Timeline);
