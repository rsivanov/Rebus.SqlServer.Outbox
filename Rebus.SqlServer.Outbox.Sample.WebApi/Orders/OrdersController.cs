using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Rebus.SqlServer.Outbox.Sample.WebApi.Orders.Commands;
using Rebus.SqlServer.Outbox.Sample.WebApi.Orders.Services;

namespace Rebus.SqlServer.Outbox.Sample.WebApi.Orders
{
	[Route("[controller]")]
	public class OrdersController : Controller
	{
		private readonly IOrdersService _ordersService;

		public OrdersController(IOrdersService ordersService)
		{
			_ordersService = ordersService;
		}
		
		[HttpPost]
		public Task<long> CreateOrder([FromBody] CreateOrderRequest createOrderRequest)
		{
			return _ordersService.CreateOrder(createOrderRequest);
		}
	}
}