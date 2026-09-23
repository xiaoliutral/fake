namespace Fake.FileManagement.Domain.FileAggregate;

public enum StoredFileStatus
{
    /// <summary>已签发直传凭证，尚未确认。</summary>
    Pending = 0,

    /// <summary>已可用（服务端上传完成或直传已确认）。</summary>
    Available = 1
}
