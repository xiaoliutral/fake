namespace Fake.UnitOfWork;

public interface ITransactionApi : IDisposable
{
    Task CommitAsync(CancellationToken cancellationToken = default);
}