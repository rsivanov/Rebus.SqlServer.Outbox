using System.Data;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Rebus.Bus;
using Rebus.SqlServer.Outbox.Sample.WebApi.DataAccess;
using Rebus.SqlServer.Outbox.Sample.WebApi.DataAccess.Options;
using Rebus.SqlServer.Outbox.Sample.WebApi.Orders.Commands;
using Rebus.SqlServer.Outbox.Sample.WebApi.Orders.Events;
using Rsi.LocalTransactions;

namespace Rebus.SqlServer.Outbox.Sample.WebApi.Orders.Services
{
	public class OrdersService : IOrdersService
	{
		private readonly IOptions<DataAccessOptions> _dataAccessOptions;
		private readonly IBus _bus;

		public OrdersService(IOptions<DataAccessOptions> dataAccessOptions, IBus bus)
		{
			_dataAccessOptions = dataAccessOptions;
			_bus = bus;
		}
	
		public async Task<long> CreateOrder(CreateOrderRequest createOrderRequest)
		{
			using var transactionScope = new LocalTransactionScope();
			using var connection =
				SqlConnectionFactory.GetOpenConnection(_dataAccessOptions.Value.ConnectionString);
			using var command = connection.CreateCommand();
			command.CommandText =
				$@"INSERT INTO Orders (ProductId, Quantity) output INSERTED.Id VALUES (@productId, @quantity)";

			command.Parameters.Add("productId", SqlDbType.NVarChar, 100).Value = createOrderRequest.ProductId;
			command.Parameters.Add("quantity", SqlDbType.Int).Value = createOrderRequest.Quantity;

			var id = (long) await command.ExecuteScalarAsync();
			await _bus.Publish(new OrderCreated {Id = id, ProductId = createOrderRequest.ProductId, Quantity = createOrderRequest.Quantity});
			transactionScope.Complete();
			return id;
		}
	}
}