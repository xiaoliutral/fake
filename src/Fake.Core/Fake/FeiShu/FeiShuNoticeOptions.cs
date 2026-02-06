namespace Fake.FeiShu;

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