using System.Data;
using System.Data.Common;
using System.Diagnostics;
using Fake.EntityFrameworkCore;
using Fake.Threading;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fake.UnitOfWork.EntityFrameWorkCore;

/// <summary>
/// 基于工作单元的DbContext
/// </summary>
/// <typeparam name="TDbContext"></typeparam>
public class UowEfDbContextProvider<TDbContext>(
    IUnitOfWorkManager unitOfWorkManager,
    ICancellationTokenProvider cancellationTokenProvider,
    IConnectionStringResolver connectionStringResolver)
    : IEfDbContextProvider<TDbContext>
    where TDbContext : EfCoreDbContext<TDbContext>
{
    public readonly ILogger<UowEfDbContextProvider<TDbContext>> Logger =
        NullLogger<UowEfDbContextProvider<TDbContext>>
            .Instance;

    private const string TransactionsNotSupportedErrorMessage = "当前数据库不支持事务！";

    public async Task<TDbContext> GetDbContextAsync(CancellationToken cancellationToken = default)
    {
        var unitOfWork = unitOfWorkManager.Current;

        if (unitOfWork == null)
        {
            throw new FakeException($"{typeof(TDbContext).Name}必须在工作单元内创建！");
        }

        var targetDbContextType = typeof(TDbContext);
        var connectionStringName = ConnectionStringNameAttribute.GetConnStringName<TDbContext>();
        var connectionString = await connectionStringResolver.ResolveAsync(connectionStringName);

        var dbContextKey = $"{targetDbContextType.FullName}_{connectionString}";
        var databaseApi = unitOfWork.FindDatabaseApi(dbContextKey);

        if (databaseApi == null)
        {
            var dbContext = await CreateDbContextAsync(unitOfWork, connectionString, cancellationToken);
            databaseApi = new EfCoreDatabaseApi<TDbContext>(dbContext);

            unitOfWork.AddDatabaseApi(dbContextKey, databaseApi);
        }

        return ((EfCoreDatabaseApi<TDbContext>)databaseApi).DbContext;
    }


    private async Task<TDbContext> CreateDbContextAsync(IUnitOfWork unitOfWork,
        string connectionString, CancellationToken cancellationToken = default)
    {
        var creationContext = new DbContextCreationContext(connectionString);
        using (DbContextCreationContext.Use(creationContext))
        {
            var dbContext = unitOfWork.Context.IsTransactional
                ? await CreateDbContextWithTransactionAsync(unitOfWork, cancellationToken)
                // 这里必须直接注入FakeDbContext<TDbContext>，不能用IDbContextFactory<>，否则会报错
                : unitOfWork.ServiceProvider.GetRequiredService<TDbContext>();

            dbContext.Initialize(unitOfWork);
            return dbContext;
        }
    }


    private async Task<TDbContext> CreateDbContextWithTransactionAsync(IUnitOfWork unitOfWork,
        CancellationToken cancellationToken = default)
    {
        Debug.Assert(DbContextCreationContext.Current != null, "DbContextCreationContext.Current != null");

        var connectionString = DbContextCreationContext.Current.ConnectionString;
        var transactionApiKey = $"EntityFrameworkCore_{connectionString}";
        var activeTransaction = unitOfWork.FindTransactionApi(transactionApiKey) as EfCoreTransactionApi;

        var dbContext = unitOfWork.ServiceProvider.GetRequiredService<TDbContext>();
        var token = GetCancellationToken(cancellationToken);

        // 该 EF 连接已开启事务 → 复用 / attend
        if (activeTransaction != null)
        {
            await AttendExistingEfTransactionAsync(dbContext, activeTransaction, token);
            return dbContext;
        }

        // 同连接串已有 SqlSugar（或其他）共享事务 → 只 UseTransaction，不登记新的 Commit owner
        var sharedTx = UnitOfWorkSharedDb.GetDbTransaction(unitOfWork, connectionString);
        if (sharedTx != null)
        {
            if (!await TryEnlistSharedTransactionAsync(dbContext, sharedTx, token))
            {
                Logger.LogError(
                    "Failed to enlist EF into shared DbTransaction for {ConnectionString}; falling back to own transaction",
                    connectionString);
            }
            else
            {
                return dbContext;
            }
        }

        try
        {
            var dbTransaction = await dbContext.Database.BeginTransactionAsync(
                unitOfWork.Context.IsolationLevel,
                token
            );

            var api = new EfCoreTransactionApi(
                dbTransaction,
                dbContext,
                cancellationTokenProvider
            );
            unitOfWork.AddTransactionApi(transactionApiKey, api);
            UnitOfWorkSharedDb.Register(unitOfWork, connectionString, api);
        }
        catch (Exception e) when (e is InvalidOperationException || e is NotSupportedException)
        {
            Logger.LogError(TransactionsNotSupportedErrorMessage);
            Logger.LogException(e);
        }

        return dbContext;
    }

    private async Task AttendExistingEfTransactionAsync(
        TDbContext dbContext,
        EfCoreTransactionApi activeTransaction,
        CancellationToken token)
    {
        DbContextCreationContext.Current!.ExistingConnection =
            activeTransaction.DbContextTransaction.GetDbTransaction().Connection;

        // 如果是关系型数据库
        if (dbContext.Database.GetService<IDbContextTransactionManager>() is IRelationalTransactionManager)
        {
            // 同一个连接复用一个事务
            if (dbContext.Database.GetDbConnection() == DbContextCreationContext.Current.ExistingConnection)
            {
                await dbContext.Database.UseTransactionAsync(
                    activeTransaction.DbContextTransaction.GetDbTransaction(),
                    token
                );
            }
            else
            {
                try
                {
                    /* 用户没有使用已存在的连接，我们将启动一个新的事务
                     * EfCoreTransactionApi将检查连接字符串匹配并通过DbContext实例提交/回滚
                     * */
                    await dbContext.Database.BeginTransactionAsync(
                        unitOfWorkManager.Current!.Context.IsolationLevel,
                        token
                    );
                }
                catch (Exception e) when (e is InvalidOperationException || e is NotSupportedException)
                {
                    Logger.LogError(TransactionsNotSupportedErrorMessage);
                    Logger.LogException(e);

                    return;
                }
            }
        }
        else
        {
            try
            {
                await dbContext.Database.BeginTransactionAsync(token);
            }
            catch (Exception e) when (e is InvalidOperationException || e is NotSupportedException)
            {
                Logger.LogError(TransactionsNotSupportedErrorMessage);
                Logger.LogException(e);

                return;
            }
        }

        activeTransaction.AttendedDbContexts.Add(dbContext);
    }

    private async Task<bool> TryEnlistSharedTransactionAsync(
        TDbContext dbContext,
        DbTransaction sharedTx,
        CancellationToken token)
    {
        try
        {
            var sharedConnection = sharedTx.Connection
                                   ?? throw new InvalidOperationException("Shared DbTransaction has no Connection.");

            if (dbContext.Database.GetDbConnection() != sharedConnection)
            {
                var current = dbContext.Database.GetDbConnection();
                if (current.State == ConnectionState.Open)
                {
                    current.Close();
                }

                dbContext.Database.SetDbConnection(sharedConnection, contextOwnsConnection: false);
            }

            if (dbContext.Database.GetDbConnection().State != ConnectionState.Open)
            {
                await dbContext.Database.OpenConnectionAsync(token);
            }

            await dbContext.Database.UseTransactionAsync(sharedTx, token);
            return true;
        }
        catch (Exception e) when (e is InvalidOperationException || e is NotSupportedException)
        {
            Logger.LogError(TransactionsNotSupportedErrorMessage);
            Logger.LogException(e);
            return false;
        }
    }

    protected virtual CancellationToken GetCancellationToken(CancellationToken preferredValue = default)
    {
        return cancellationTokenProvider.FallbackToProvider(preferredValue);
    }
}
