namespace Rebus.SqlServer.Outbox.Sample.WebApi.Orders.Commands
{
	public class CreateOrderRequest
	{
		public string ProductId { get; set; }
		
		public int Quantity { get; set; }
	}
}