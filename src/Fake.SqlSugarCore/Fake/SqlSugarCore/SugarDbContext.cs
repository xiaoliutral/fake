using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics;
using System.Reflection;
using System.Text;
using Fake.Data.Filtering;
using Fake.DependencyInjection;
using Fake.Domain.Entities.Auditing;
using Fake.Domain.Events;
using Fake.EventBus.Local;
using Fake.IdGenerators;
using Fake.SyncEx;
using Fake.Timing;

namespace Fake.SqlSugarCore;

public abstract class SugarDbContext<TDbContext> where TDbContext : SugarDbContext<TDbContext>
{
    public ILazyServiceProvider ServiceProvider { get; set; } = null!;
    public ISqlSugarClient SqlSugarClient { get; private set; }

    protected readonly SugarDbConnOptions<TDbContext> Options;
    protected IFakeClock FakeClock => ServiceProvider.GetRequiredService<IFakeClock>();
    protected GuidGeneratorBase GuidGenerator => ServiceProvider.GetRequiredService<GuidGeneratorBase>();
    protected LongIdGeneratorBase LongIdGenerator => ServiceProvider.GetRequiredService<LongIdGeneratorBase>();
    protected ILocalEventBus LocalEventBus => ServiceProvider.GetRequiredService<ILocalEventBus>();
    protected IAuditPropertySetter AuditPropertySetter => ServiceProvider.GetRequiredService<IAuditPropertySetter>();
    protected IDataFilter DataFilter => ServiceProvider.GetRequiredService<IDataFilter>();

    protected ILogger<SugarDbContext<TDbContext>> Logger =>
        ServiceProvider.GetRequiredService<ILogger<SugarDbContext<TDbContext>>>();

    public SugarDbContext(SugarDbConnOptions<TDbContext> options)
    {
        Options = options;
        var config = new ConnectionConfig
        {
            ConfigId = Options.ConfigId,
            DbType = Options.DbType,
            ConnectionString = Options.ConnectionString,
            SlaveConnectionConfigs = Options.ReadConnectionStrings
                .Select(str => new SlaveConnectionConfig { ConnectionString = str }).ToList(),
            IsAutoCloseConnection = Options.IsAutoCloseConnection,
            AopEvents = new AopEvents
            {
                OnDiffLogEvent = OnDiffLogEvent,
                OnError = OnError,
                OnLogExecuting = OnLogExecuting,
                OnLogExecuted = OnLogExecuted,
                OnExecutingChangeSql = null,
                DataExecuting = DataExecuting,
                DataChangesExecuted = DataChangesExecuted,
                DataExecuted = DataExecuted
            },
            ConfigureExternalServices = new ConfigureExternalServices
            {
                EntityService = ConfigureEntityService,
                EntityNameService = ConfigureEntityNameService,
            },
            IndexSuffix = null,
            SqlMiddle = null
        };

        ConfigureConnection(config);

        SqlSugarClient = new SqlSugarClient(config);

        SqlSugarClient.Ado.CommandTimeOut = options.Timeout;
    }

    public virtual void Initialize(int timeout)
    {
        // 设置超时时间
        SqlSugarClient.Ado.CommandTimeOut = timeout;
        ConfigureGlobalFilters();
    }

    /// <summary>
    /// 查询过滤器
    /// </summary>
    protected virtual void ConfigureGlobalFilters()
    {
        SqlSugarClient.QueryFilter.AddTableFilter<ISoftDelete>(u =>
            DataFilter.IsEnabled<ISoftDelete>() == false || !u.IsDeleted);
    }

    protected void ConfigureConnection(ConnectionConfig action)
    {
    }

    protected virtual void ConfigureEntityService(PropertyInfo property, EntityColumnInfo column)
    {
        if (property.Name == nameof(Entity<Any>.Id))
        {
            column.IsPrimarykey = true;
        }

        if (property.Name == nameof(Entity<Any>.IsTransient))
        {
            column.IsIgnore = true;
        }

        if (property.Name == nameof(AggregateRoot.ConcurrencyStamp))
        {
            column.IsEnableUpdateVersionValidation = true;
        }

        if (new NullabilityInfoContext().Create(property).WriteState is NullabilityState.Nullable)
        {
            column.IsNullable = true;
        }

        if (property.Name == nameof(IHasDomainEvent.DomainEvents))
        {
            column.IsIgnore = true;
        }

        if (property.Name == nameof(IHasExtraProperties.ExtraProperties))
        {
            column.IsIgnore = true;
        }
    }


    protected virtual void ConfigureEntityNameService(Type type, EntityInfo entity)
    {
        var attribute = type.GetCustomAttribute<TableAttribute>();
        entity.DbTableName = attribute?.Name ?? type.GetCustomAttribute<SugarTable>()?.TableName;
    }

    protected virtual async Task PublishDomainEventsAsync(IHasDomainEvent entityWithDomainEvent)
    {
        if (entityWithDomainEvent.DomainEvents == null) return;
        foreach (var @event in entityWithDomainEvent.DomainEvents)
        {
            await LocalEventBus.PublishAsync(@event);
        }
    }

    #region DataExecuting

    /// <summary>
    /// 增、删、改 前
    /// </summary>
    /// <param name="value"></param>
    /// <param name="entityInfo"></param>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    protected virtual void DataExecuting(object value, DataFilterModel entityInfo)
    {
        if (entityInfo.EntityValue is not IEntity entity) return;

        // tips：DataExecuting是字段级别的，加上此判断，才能做到类似EF的只进一次
        if (entityInfo.EntityColumnInfo.IsPrimarykey)
        {
            switch (entityInfo.OperationType)
            {
                case DataFilterType.UpdateByObject:
                    AuditPropertySetter.SetModificationProperties(entity);
                    break;
                case DataFilterType.InsertByObject:
                    AuditPropertySetter.SetCreationProperties(entity);
                    break;
                case DataFilterType.DeleteByObject:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }


    /// <summary>
    /// 增、删、改 后
    /// </summary>
    /// <param name="value"></param>
    /// <param name="entityInfo"></param>
    /// <exception cref="NotImplementedException"></exception>
    protected virtual void DataChangesExecuted(object value, DataFilterModel entityInfo)
    {
        if (entityInfo.EntityColumnInfo.IsPrimarykey)
        {
            if (entityInfo.EntityValue is IHasDomainEvent entity)
            {
                SyncContext.Run(() => PublishDomainEventsAsync(entity));
            }
        }
    }


    /// <summary>
    /// 查询完对数据进行加工事件
    /// </summary>
    /// <param name="value"></param>
    /// <param name="entityInfo"></param>
    protected virtual void DataExecuted(object value, DataAfterModel entityInfo)
    {
    }

    #endregion

    #region 日志

    /// <summary>
    /// 差异日志功能（审计）
    /// </summary>
    /// <param name="obj"></param>
    /// <exception cref="NotImplementedException"></exception>
    protected virtual void OnDiffLogEvent(DiffLogModel obj)
    {
    }

    /// <summary>
    /// SQL报错
    /// </summary>
    /// <param name="exception"></param>
    protected virtual void OnError(SqlSugarException exception)
    {
    }

    /// <summary>
    /// SQL执行前
    /// </summary>
    /// <param name="sql"></param>
    /// <param name="pars"></param>
    protected virtual void OnLogExecuting(string sql, SugarParameter[] pars)
    {
        LogSql(sql, pars);
    }

    private void LogSql(string sql, SugarParameter[] pars)
    {
        if (Options.EnabledSqlLog)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"SQL Sugar log [hash:{sql.GetHashCode()}]:");
            if (Debugger.IsAttached)
            {
                sb.AppendLine(UtilMethods.GetSqlString(Options.DbType, sql, pars));
            }
            else
            {
                sb.AppendLine(UtilMethods.GetNativeSql(sql, pars));
            }

            Logger.LogDebug(sb.ToString());
        }
    }

    protected virtual void OnLogExecuted(string sql, SugarParameter[] pars)
    {
        if (Options.EnabledSqlLog)
        {
            Logger.LogDebug($"- SQL sugar execution duration [hash:{sql.GetHashCode()}]: {{times}} ms",
                SqlSugarClient.Ado.SqlExecutionTime.TotalMilliseconds);
        }
    }

    #endregion
}