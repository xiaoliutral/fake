// See https://aka.ms/new-console-template for more information

using System.Reflection;
using SqlSugar;

Console.WriteLine("Hello, World!");

//创建数据库对象 (用法和EF Dappper一样通过new保证线程安全)
var db = new SqlSugarClient(new ConnectionConfig()
    {
        ConnectionString = "",
        DbType = DbType.MySql,
        IsAutoCloseConnection = true
    },
    client =>
    {
        client.Aop.OnLogExecuting = (sql, pars) =>
        {
            Console.WriteLine(UtilMethods.GetSqlString(DbType.SqlServer, sql, pars));
        };
    });

var path = Path.Combine(Directory.GetCurrentDirectory(), "Entities");
db.DbFirst.IsCreateAttribute().StringNullable().CreateClassFile(path, "Models");