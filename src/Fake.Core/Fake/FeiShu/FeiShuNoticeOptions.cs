namespace Fake.FeiShu;

public class FeiShuNoticeOptions
{
    public string Webhook { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public int Timeout { get; set; } = 20;
    public int QueueCapacity { get; set; } = 500;
    public int BatchSize { get; set; } = 10;
    public int BatchIntervalSeconds { get; set; } = 5;

    /// <summary>
    /// 消息最大长度
    /// </summary>
    public int MaxLength { get; set; } = 200;

    /// <summary>
    /// 发送失败重试ms
    /// </summary>
    public int[] RetryDelays { get; set; } = [1000, 2000, 5000];


    #region 异常订阅

    public bool EnableFeiShuExceptionSubscribe { get; set; }
    public bool WriteBody { get; set; }
    public bool WriteHeader { get; set; }
    public bool WriteStack { get; set; }

    #endregion


    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Webhook))
            throw new ArgumentException("Webhook 不能为空", nameof(Webhook));
        
        if (string.IsNullOrWhiteSpace(Title))
            throw new ArgumentException("Title 不能为空", nameof(Title));
        
        if (Timeout <= 0)
            throw new ArgumentException("Timeout 必须大于 0", nameof(Timeout));
        
        if (QueueCapacity <= 0)
            throw new ArgumentException("QueueCapacity 必须大于 0", nameof(QueueCapacity));
        
        if (BatchSize <= 0)
            throw new ArgumentException("BatchSize 必须大于 0", nameof(BatchSize));
        
        if (BatchIntervalSeconds <= 0)
            throw new ArgumentException("BatchIntervalSeconds 必须大于 0", nameof(BatchIntervalSeconds));
    }

    public override string ToString()
    {
        return $"Webhook:{Webhook}";
    }
}