using Microsoft.Data.SqlClient;
using Rsi.LocalTransactions;

namespace Rebus.SqlServer.Outbox.Sample.WebApi.DataAccess
{
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
}