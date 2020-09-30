using System;
using System.Threading.Tasks;
using Rebus.Handlers;
using Rebus.SqlServer.Outbox.Sample.WebApi.Orders.Events;

namespace Rebus.SqlServer.Outbox.Sample.WebApi.Orders.Handlers
{
	public class OrderCreatedMessageHandler : IHandleMessages<OrderCreated>
	{
		public Task Handle(OrderCreated message)
		{
			Console.WriteLine($"Order arrived: {message.Id}");
			return Task.CompletedTask;
		}
	}
}