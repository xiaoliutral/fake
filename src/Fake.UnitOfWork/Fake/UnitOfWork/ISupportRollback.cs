namespace Fake.UnitOfWork;

public interface ISupportRollback
{
    Task RollbackAsync(CancellationToken cancellationToken = default);
}