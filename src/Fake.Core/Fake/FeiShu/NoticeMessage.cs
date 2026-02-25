using Microsoft.Extensions.Logging;

namespace Fake.FeiShu;

public record NoticeMessage(string Content, LogLevel LogLevel, DateTime CreatedAt);
