namespace System.Threading;

public static class TaskFactoryExtensions
{
    /// <summary>
    /// queues work to factory 并返回 task，如果工厂未指定scheduler 则使用默认的
    /// </summary>
    /// <param name="this"></param>
    /// <param name="action"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    public static Task Run(this TaskFactory @this, Action action)
    {
        if (@this == null)
            throw new ArgumentNullException(nameof(@this));
        if (action == null)
            throw new ArgumentNullException(nameof(action));

        return @this.StartNew(action, @this.CancellationToken,
            @this.CreationOptions | TaskCreationOptions.DenyChildAttach,
            @this.Scheduler ?? TaskScheduler.Default);
    }

    /// <summary>
    /// queues work to factory 并返回 task，如果工厂未指定scheduler 则使用默认的
    /// </summary>
    /// <param name="this"></param>
    /// <param name="action"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    public static Task<TResult> Run<TResult>(this TaskFactory @this, Func<TResult> action)
    {
        if (@this == null)
            throw new ArgumentNullException(nameof(@this));
        if (action == null)
            throw new ArgumentNullException(nameof(action));

        return @this.StartNew(action, @this.CancellationToken,
            @this.CreationOptions | TaskCreationOptions.DenyChildAttach, @this.Scheduler ?? TaskScheduler.Default);
    }

    /// <summary>
    /// queues work to factory 并返回 task，如果工厂未指定scheduler 则使用默认的
    /// </summary>
    /// <param name="this"></param>
    /// <param name="action"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    public static Task Run(this TaskFactory @this, Func<Task> action)
    {
        if (@this == null)
            throw new ArgumentNullException(nameof(@this));
        if (action == null)
            throw new ArgumentNullException(nameof(action));

        return @this.StartNew(action, @this.CancellationToken,
                @this.CreationOptions | TaskCreationOptions.DenyChildAttach, @this.Scheduler ?? TaskScheduler.Default)
            .Unwrap();
    }

    /// <summary>
    /// queues work to factory 并返回 task，如果工厂未指定scheduler 则使用默认的
    /// </summary>
    /// <param name="this"></param>
    /// <param name="action"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    public static Task<TResult> Run<TResult>(this TaskFactory @this, Func<Task<TResult>> action)
    {
        if (@this == null)
            throw new ArgumentNullException(nameof(@this));
        if (action == null)
            throw new ArgumentNullException(nameof(action));

        return @this.StartNew(action, @this.CancellationToken,
                @this.CreationOptions | TaskCreationOptions.DenyChildAttach, @this.Scheduler ?? TaskScheduler.Default)
            .Unwrap();
    }
}