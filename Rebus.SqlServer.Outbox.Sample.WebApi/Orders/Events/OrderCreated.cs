namespace Rebus.SqlServer.Outbox.Sample.WebApi.Orders.Events
{
	public class OrderCreated
	{
		public long Id { get; set; }
		
		public string ProductId { get; set; }
		
		public int Quantity { get; set; }
	}
}