namespace Rebus.SqlServer.Outbox.Sample.WebApi.RabbitMq.Options
{
	/// <summary>
	/// RabbitMq configuration options
	/// </summary>
	public class RabbitMqOptions
	{
		public string Host { get; set; }

		public int Port { get; set; }

		public string User { get; set; }
		
		public string Password { get; set; }
		
		public string InputQueueName { get; set; }
	}
}