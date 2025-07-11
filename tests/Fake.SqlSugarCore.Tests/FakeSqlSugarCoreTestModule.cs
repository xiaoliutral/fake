using Domain.Aggregates.BuyerAggregate;
using Domain.Aggregates.OrderAggregate;
using Fake.Domain.Entities;
using Fake.Domain.Repositories;
using Fake.DomainDrivenDesign.Repositories.SqlSugarCore;
using Fake.Modularity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SqlSugar;

namespace Fake.SqlSugarCore.Tests;

[DependsOn(typeof(FakeAppTestModule))]
[DependsOn(typeof(FakeSqlSugarCoreModule))]
public class FakeSqlSugarCoreTestModule : FakeModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.AddSugarDbContext<OrderingContext>(options =>
        {
            // tips：sugar client 仅在内存测试场景下使用单例,生产环境勿用
            options.ConnectionString = $"Filename=:memory:";
            options.DbType = DbType.Sqlite;
            options.IsAutoCloseConnection = false;
        }, ServiceLifetime.Singleton);

        context.Services.AddTransient(typeof(IRepository<Order>),
            typeof(SqlSugarRepository<OrderingContext, Order>));
        context.Services.AddTransient(typeof(IRepository<Buyer>),
            typeof(SqlSugarRepository<OrderingContext, Buyer>));
    }

    public override void PreConfigureApplication(ApplicationConfigureContext context)
    {
        var orderingContext = context.ServiceProvider.GetRequiredService<OrderingContext>();

        var client = orderingContext.SqlSugarClient;

        var entities = GetEntitiesAndClearnDb(client);
        client.CodeFirst.SetStringDefaultLength(200).InitTables(entities);
    }

    private Type[] GetEntitiesAndClearnDb(ISqlSugarClient client)
    {
        var types = typeof(FakeSqlSugarCoreTestModule).Assembly.GetTypes()
            .Where(x => x.IsAssignableTo<IEntity>())
            .ToList();

        if (types.Count > 0)
        {
            var tables = client.Ado.SqlQuery<string>("SELECT name FROM sqlite_master WHERE type='table'");
            foreach (var table in tables)
            {
                client.Ado.ExecuteCommand($"DROP TABLE IF EXISTS {table}");
            }
        }

        return types.ToArray();
    }
}