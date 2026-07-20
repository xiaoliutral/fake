namespace Fake.UnitOfWork;

public static class UnitOfWorkExtensions
{
    public static TValue GetOrAddItem<TValue>(this IUnitOfWork unitOfWork, string key, Func<string, TValue> factory)
        where TValue : class
    {
        ThrowHelper.ThrowIfNull(unitOfWork, nameof(unitOfWork));
        ThrowHelper.ThrowIfNull(key, nameof(key));
        ThrowHelper.ThrowIfNull(factory, nameof(factory));

        return (TValue)unitOfWork.Items.GetOrAdd(key, k => factory(k));
    }
}
