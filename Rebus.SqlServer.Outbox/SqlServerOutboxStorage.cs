using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using Rebus.Messages;
using Rebus.Outbox;
using Rebus.Serialization;
using Rebus.SqlServer.Outbox.Internal;

namespace Rebus.SqlServer.Outbox
{
	/// <summary>
	/// SQL Server implementation of the outbox storage abstraction
	/// </summary>
	public class SqlServerOutboxStorage : IOutboxStorage
	{
		private readonly Func<Task<DbConnection>> _connectionFactory;
		private static readonly HeaderSerializer _headerSerializer = new HeaderSerializer();
		private readonly TableName _tableName;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="connectionFactory"></param>
		/// <param name="tableName"></param>
		public SqlServerOutboxStorage(Func<Task<DbConnection>> connectionFactory, string tableName)
		{
			_connectionFactory = connectionFactory;
			_tableName = TableName.Parse(tableName);
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
			using (var connection = await _connectionFactory().ConfigureAwait(false))
			{
				if (connection.State != ConnectionState.Open)
					await connection.OpenAsync().ConfigureAwait(false);
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
			}
		}
		
		/// <summary>
		/// <inheritdoc />
		/// </summary>
		public async Task Store(TransportMessage message)
		{
			using (var connection = await _connectionFactory().ConfigureAwait(false))
			{
				if (connection.State != ConnectionState.Open)
					await connection.OpenAsync().ConfigureAwait(false);
				using (var command = connection.CreateCommand())
				{
					command.CommandText =
						$@"INSERT INTO {_tableName} ([headers], [body]) VALUES (@headers, @body)";

					var headersString = _headerSerializer.SerializeToString(message.Headers);
					var headersParam = command.CreateParameter();
					headersParam.ParameterName = "headers";
					headersParam.DbType = DbType.String;
					headersParam.Size = -1;
					headersParam.Value = headersString;
					command.Parameters.Add(headersParam);

					var bodyParam = command.CreateParameter();
					bodyParam.ParameterName = "body";
					bodyParam.DbType = DbType.Binary;
					bodyParam.Size = -1;
					bodyParam.Value = message.Body;
					command.Parameters.Add(bodyParam);

					await command.ExecuteNonQueryAsync().ConfigureAwait(false);
				}
			}
		}

		/// <summary>
		/// <inheritdoc />
		/// </summary>
		public async Task<TransportMessage[]> Retrieve(CancellationToken cancellationToken, int topMessages)
		{
			var messages = new List<TransportMessage>();
			using (var connection = await _connectionFactory().ConfigureAwait(false))
			{
				if (connection.State != ConnectionState.Open)
					await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
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
			}

			return messages.ToArray();
		}
	}
}