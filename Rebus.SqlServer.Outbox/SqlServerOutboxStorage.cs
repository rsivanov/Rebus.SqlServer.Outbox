using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Rebus.Logging;
using Rebus.Messages;
using Rebus.Outbox;
using Rebus.Serialization;

namespace Rebus.SqlServer.Outbox
{
	/// <summary>
	/// SQL Server implementation of the outbox storage abstraction
	/// </summary>
	public class SqlServerOutboxStorage : IOutboxStorage
	{
		private readonly IDbConnectionProvider _connectionProvider;
		private static readonly HeaderSerializer _headerSerializer = new HeaderSerializer();
		private readonly TableName _tableName;
		private readonly ILog _log;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="connectionProvider"></param>
		/// <param name="tableName"></param>
		/// <param name="rebusLoggerFactory"></param>
		public SqlServerOutboxStorage(IDbConnectionProvider connectionProvider, string tableName, IRebusLoggerFactory rebusLoggerFactory)
		{
			_connectionProvider = connectionProvider;
			_tableName = TableName.Parse(tableName);
			_log = rebusLoggerFactory.GetLogger<SqlServerOutboxStorage>();
		}
		
		/// <summary>
		/// Creates the outbox table if necessary
		/// </summary>
		public void EnsureTableIsCreated()
		{
			try
			{
				EnsureTableIsCreatedAsync().GetAwaiter().GetResult();
			}
			catch
			{
				EnsureTableIsCreatedAsync().GetAwaiter().GetResult();
			}
		}

		private async Task EnsureTableIsCreatedAsync()
		{
			using (var connection = await _connectionProvider.GetConnection().ConfigureAwait(false))
			{
				var tableNames = connection.GetTableNames();

				if (tableNames.Contains(_tableName))
				{
					return;
				}

				_log.Info("Table {tableName} does not exist - it will be created now", _tableName.ToString());

				using (var command = connection.CreateCommand())
				{
					command.CommandText = $@"
IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = '{_tableName.Schema}')
	EXEC('CREATE SCHEMA {_tableName.Schema}')
----
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = '{_tableName.Schema}' AND TABLE_NAME = '{
							_tableName.Name
						}')
    CREATE TABLE {_tableName} (
        [id] [bigint] IDENTITY(1,1) NOT NULL,
	    [headers] [nvarchar](MAX) NOT NULL,
	    [body] [varbinary](MAX) NOT NULL,
        CONSTRAINT [PK_{_tableName.Schema}_{_tableName.Name}] PRIMARY KEY NONCLUSTERED 
        (
	        [id] ASC
        )
    )
";
					await command.ExecuteNonQueryAsync().ConfigureAwait(false);
				}

				await connection.Complete().ConfigureAwait(false);
			}
		}
		
		/// <summary>
		/// <inheritdoc />
		/// </summary>
		public async Task Store(TransportMessage message)
		{
			using (var connection = await _connectionProvider.GetConnection().ConfigureAwait(false))
			{
				using (var command = connection.CreateCommand())
				{
					command.CommandText =
						$@"INSERT INTO {_tableName} ([headers], [body]) VALUES (@headers, @body)";

					var headersString = _headerSerializer.SerializeToString(message.Headers);
					command.Parameters.Add("headers", SqlDbType.NVarChar, -1).Value = headersString;
					command.Parameters.Add("body", SqlDbType.VarBinary, -1).Value = message.Body;

					await command.ExecuteNonQueryAsync().ConfigureAwait(false);
				}

				await connection.Complete().ConfigureAwait(false);
			}
		}

		/// <summary>
		/// <inheritdoc />
		/// </summary>
		public async Task<TransportMessage[]> Retrieve(CancellationToken cancellationToken, int topMessages)
		{
			var messages = new List<TransportMessage>();
			using (var connection = await _connectionProvider.GetConnection().ConfigureAwait(false))
			{
				using (var command = connection.CreateCommand())
				{
					command.CommandText =
						$@"
DELETE TOP({topMessages}) 
FROM {_tableName} WITH (READPAST, ROWLOCK, READCOMMITTEDLOCK)
OUTPUT deleted.headers, deleted.body
";

					using (var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false))
					{
						while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
						{
							var headers = _headerSerializer.DeserializeFromString((string) reader["headers"]);
							var body = (byte[]) reader["body"];

							messages.Add(new TransportMessage(headers, body));
						}
					}
				}

				await connection.Complete().ConfigureAwait(false);
			}

			return messages.ToArray();
		}
	}
}