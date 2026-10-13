using Fake.EventBus.Distributed;
using Shouldly;

namespace Fake.EventBus.Tests;

public class EventBusMetricsTests
{
    [Fact]
    public void 记录后快照与时间线应更新()
    {
        var metrics = new EventBusMetrics(timelineCapacity: 8);

        metrics.RecordOutboxEnqueued("A");
        metrics.RecordOutboxDispatched("A", "immediate");
        metrics.RecordOutboxDispatched("A", "scan");
        metrics.RecordOutboxDispatchFailed("A", "scan");
        metrics.RecordOutboxScanRetry("A");
        metrics.RecordInboxProcessed("A");
        metrics.RecordInboxDuplicate("A");
        metrics.RecordInboxFailed("A");

        var snap = metrics.GetSnapshot();
        snap.OutboxEnqueued.ShouldBe(1);
        snap.OutboxDispatchedImmediate.ShouldBe(1);
        snap.OutboxDispatchedScan.ShouldBe(1);
        snap.OutboxDispatchFailed.ShouldBe(1);
        snap.OutboxScanRetry.ShouldBe(1);
        snap.InboxProcessed.ShouldBe(1);
        snap.InboxDuplicate.ShouldBe(1);
        snap.InboxFailed.ShouldBe(1);
        snap.Timeline.Count.ShouldBeGreaterThanOrEqualTo(8);
    }
}
