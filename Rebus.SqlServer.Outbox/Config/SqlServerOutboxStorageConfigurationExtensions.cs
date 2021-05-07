using System;
using System.Data.Common;
using System.Threading.Tasks;
using Rebus.Config;
using Rebus.Outbox;

namespace Rebus.SqlServer.Outbox.Config
{
	/// <summary>
	/// Configuration extensions for configuring SQL persistence for outbox.
	/// </summary>
	public static class SqlServerOutboxStorageConfigurationExtensions
	{
		/// <summary>
		/// Configures Rebus to use SQL Server to store outbox messages.
		/// </summary>
		/// <param name="configurer"></param>
		/// <param name="connectionFactory">Connection factory which should return a shared connection</param>
		/// <param name="tableName">Outbox messages table name including schema</param>
		/// <param name="automaticallyCreateTables">Create outbox messages table automatically</param>
		/// <exception cref="ArgumentNullException"></exception>
		public static void UseSqlServer(this StandardConfigurer<IOutboxStorage> configurer,
			Func<Task<DbConnection>> connectionFactory, string tableName, bool automaticallyCreateTables = true)
		{
			if (configurer == null) 
				throw new ArgumentNullException(nameof(configurer));
			if (connectionFactory == null) 
				throw new ArgumentNullException(nameof(connectionFactory));
			if (tableName == null) 
				throw new ArgumentNullException(nameof(tableName));
			
			configurer.Register(c =>
			{
				var outboxStorage = new SqlServerOutboxStorage(connectionFactory, tableName);

				if (automaticallyCreateTables)
				{
					outboxStorage.EnsureTableIsCreated();
				}

				return outboxStorage;
			});			
		}
	}
}
