# Rebus.SqlServer.Outbox
Provides an implementation of [Rebus.Outbox](https://github.com/rsivanov/Rebus.Outbox) abstraction using MS SQL Server as an outbox storage.

![Build](https://github.com/rsivanov/Rebus.SqlServer.Outbox/workflows/Build%20&%20test%20&%20publish%20Nuget/badge.svg?branch=master)
[![NuGet](https://img.shields.io/nuget/dt/Rebus.SqlServer.Outbox)](https://www.nuget.org/packages/Rebus.SqlServer.Outbox) 
[![NuGet](https://img.shields.io/nuget/v/Rebus.SqlServer.Outbox)](https://www.nuget.org/packages/Rebus.SqlServer.Outbox)

How to use
===
Here's a code fragment from the sample application:
```csharp
services.AddRebus(configure => configure
    .Transport(x =>
    {
        x.UseRabbitMq(rabbitMqConnectionString, rabbitMqOptions.InputQueueName);
    })
    .Outbox(c => c.UseSqlServer(() =>
    {
        var sqlConnection = SqlConnectionFactory.GetOpenConnection( dataAccessOptions.ConnectionString);
        IDbConnection rebusDbConnection = new DbConnectionWrapper(sqlConnection, null, DbConnectionScope.Current != null);
        return Task.FromResult(rebusDbConnection);
    }, "dbo.OutboxMessages"), options =>
    {
        options.RunMessagesProcessor = true;
        options.MaxMessagesToRetrieve = 5;
    })
    .Routing(r => r.TypeBased()));
```
The key point is to have some method of sharing database connections between Outbox and business services. I use [Rsi.LocalTransactions](https://github.com/rsivanov/Rsi.LocalTransactions) for that:
```csharp
public static class SqlConnectionFactory
{
    public static SqlConnection GetOpenConnection(string connectionString)
    {
        var currentScope = DbConnectionScope.Current;
        if (currentScope != null)
        {
            return (SqlConnection) DbConnectionScope.Current.GetOpenConnection(SqlClientFactory.Instance, connectionString);
        }

        var sqlConnection = (SqlConnection) SqlClientFactory.Instance.CreateConnection();
        sqlConnection.ConnectionString = connectionString;
        sqlConnection.Open();
        return sqlConnection;
    }
}
```