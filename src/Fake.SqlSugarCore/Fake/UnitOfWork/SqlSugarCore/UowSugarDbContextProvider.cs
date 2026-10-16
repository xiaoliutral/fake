using System.Data;
using System.Data.Common;
using System.Diagnostics;
using Fake.SqlSugarCore;
using Fake.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fake.UnitOfWork.SqlSugarCore;

/// <summary>
/// 基于工作单元的DbContext
/// </summary>
/// <typeparam name="TDbContext"></typeparam>
public class UowSugarDbContextProvider<TDbContext>(
    IUnitOfWorkManager unitOfWorkManager,
    ICancellationTokenProvider cancellationTokenProvider,
    IConnectionStringResolver connectionStringResolver)
    : ISugarDbContextProvider<TDbContext>
    where TDbContext : SugarDbContext<TDbContext>
{
    public readonly ILogger<UowSugarDbContextProvider<TDbContext>> Logger =
        NullLogger<UowSugarDbContextProvider<TDbContext>>
            .Instance;

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
            databaseApi = new SqlSugarDatabaseApi<TDbContext>(dbContext);

            unitOfWork.AddDatabaseApi(dbContextKey, databaseApi);
        }

        return ((SqlSugarDatabaseApi<TDbContext>)databaseApi).DbContext;
    }


    private async Task<TDbContext> CreateDbContextAsync(IUnitOfWork unitOfWork,
        string connectionString, CancellationToken cancellationToken = default)
    {
        var creationContext = new DbContextCreationContext(connectionString);
        using (DbContextCreationContext.Use(creationContext))
        {
            var dbContext = unitOfWork.Context.IsTransactional
                ? await CreateDbContextWithTransactionAsync(unitOfWork, cancellationToken)
                : unitOfWork.ServiceProvider.GetRequiredService<TDbContext>();

            dbContext.Initialize(unitOfWork.Context.Timeout);
            return dbContext;
        }
    }


    private async Task<TDbContext> CreateDbContextWithTransactionAsync(IUnitOfWork unitOfWork,
        CancellationToken cancellationToken = default)
    {
        Debug.Assert(DbContextCreationContext.Current != null, "DbContextCreationContext.Current != null");

        var connectionString = DbContextCreationContext.Current.ConnectionString;
        var transactionApiKey = $"SqlSugarCore_{connectionString}";

        var activeTransaction = unitOfWork.FindTransactionApi(transactionApiKey) as SqlSugarTransactionApi<TDbContext>;
        if (activeTransaction != null)
        {
            return (TDbContext)activeTransaction.GetDbContext();
        }

        var dbContext = unitOfWork.ServiceProvider.GetRequiredService<TDbContext>();

        // 同连接串已有 EF（或其他）共享事务 → 只 enlist，不 Begin / 不 Commit
        var sharedTx = UnitOfWorkSharedDb.GetDbTransaction(unitOfWork, connectionString);
        if (sharedTx != null)
        {
            await AttachToSharedTransactionAsync(dbContext, sharedTx, cancellationToken);
            unitOfWork.AddTransactionApi(
                transactionApiKey,
                new SqlSugarTransactionApi<TDbContext>(dbContext, ownsTransaction: false));
            return dbContext;
        }

        var transaction = new SqlSugarTransactionApi<TDbContext>(dbContext, ownsTransaction: true);
        unitOfWork.AddTransactionApi(transactionApiKey, transaction);

        await dbContext.SqlSugarClient.Ado.BeginTranAsync();
        UnitOfWorkSharedDb.Register(unitOfWork, connectionString, transaction);
        return dbContext;
    }

    private static async Task AttachToSharedTransactionAsync(
        TDbContext dbContext,
        DbTransaction dbTransaction,
        CancellationToken cancellationToken)
    {
        var connection = dbTransaction.Connection
                         ?? throw new InvalidOperationException(
                             "Shared DbTransaction has no Connection; cannot enlist SqlSugar.");

        // 共享连接由 owner 管理生命周期，禁止 SqlSugar 自动关闭
        if (dbContext.SqlSugarClient.CurrentConnectionConfig != null)
        {
            dbContext.SqlSugarClient.CurrentConnectionConfig.IsAutoCloseConnection = false;
        }

        if (connection.State != ConnectionState.Open)
        {
            if (connection is DbConnection dbConnection)
            {
                await dbConnection.OpenAsync(cancellationToken);
            }
            else
            {
                connection.Open();
            }
        }

        dbContext.SqlSugarClient.Ado.Connection = connection;
        dbContext.SqlSugarClient.Ado.Transaction = dbTransaction;
    }

    protected virtual CancellationToken GetCancellationToken(CancellationToken preferredValue = default)
    {
        return cancellationTokenProvider.FallbackToProvider(preferredValue);
    }
}
