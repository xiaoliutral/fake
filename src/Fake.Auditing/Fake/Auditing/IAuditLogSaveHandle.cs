namespace Fake.Auditing;

public interface IAuditLogSaveHandle : IDisposable
{
    Task SaveAsync();
}