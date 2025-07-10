namespace Fake.UnitOfWork;

public interface ISupportSaveChanges
{
    bool HasChanges { get; }
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}