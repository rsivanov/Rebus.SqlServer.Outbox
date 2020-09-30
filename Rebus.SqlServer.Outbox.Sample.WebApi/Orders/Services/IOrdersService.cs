using System.Threading.Tasks;
using Rebus.SqlServer.Outbox.Sample.WebApi.Orders.Commands;

namespace Rebus.SqlServer.Outbox.Sample.WebApi.Orders.Services
{
	public interface IOrdersService
	{
		Task<long> CreateOrder(CreateOrderRequest createOrderRequest);
	}
}