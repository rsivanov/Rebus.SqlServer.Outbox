using System.Data.Common;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using Rebus.Bus;
using Rebus.Config;
using Rebus.Handlers;
using Rebus.Outbox.Config;
using Rebus.Routing.TypeBased;
using Rebus.ServiceProvider;
using Rebus.SqlServer.Outbox.Config;
using Rebus.SqlServer.Outbox.Sample.WebApi.DataAccess;
using Rebus.SqlServer.Outbox.Sample.WebApi.DataAccess.Options;
using Rebus.SqlServer.Outbox.Sample.WebApi.Orders.Events;
using Rebus.SqlServer.Outbox.Sample.WebApi.Orders.Handlers;
using Rebus.SqlServer.Outbox.Sample.WebApi.Orders.Services;
using Rebus.SqlServer.Outbox.Sample.WebApi.RabbitMq.Options;
using Rsi.LocalTransactions;
using Swashbuckle.AspNetCore.SwaggerUI;

namespace Rebus.SqlServer.Outbox.Sample.WebApi
{
	public class Startup
	{
		private IConfiguration Configuration { get; }

		public Startup(IConfiguration configuration)
		{
			Configuration = configuration;
		}
		
		public void ConfigureServices(IServiceCollection services)
		{
			var dataAccessOptionsSection = Configuration.GetSection(nameof(DataAccessOptions));
			var dataAccessOptions = dataAccessOptionsSection.Get<DataAccessOptions>();
			services.Configure<DataAccessOptions>(dataAccessOptionsSection);

			var rabbitMqOptionsSection = Configuration.GetSection(nameof(RabbitMqOptions));
			var rabbitMqOptions = rabbitMqOptionsSection.Get<RabbitMqOptions>();
			services.Configure<RabbitMqOptions>(rabbitMqOptionsSection);
            
			var rabbitMqConnectionString =
				$"amqp://{rabbitMqOptions.User}:{rabbitMqOptions.Password}@{rabbitMqOptions.Host}:{rabbitMqOptions.Port.ToString()}";

			services.AddRebus(configure => configure
				.Transport(x =>
				{
					x.UseRabbitMq(rabbitMqConnectionString, rabbitMqOptions.InputQueueName);
				})
				.Outbox(c => c.UseSqlServer(() =>
				{
					var dbConnection = (DbConnection) SqlConnectionFactory.GetOpenConnection(dataAccessOptions.ConnectionString);
					return Task.FromResult(dbConnection);
				}, "dbo.OutboxMessages"), options =>
				{
					options.RunMessagesProcessor = true;
					options.MaxMessagesToRetrieve = 5;
				})
				.Routing(r => r.TypeBased()));

			services.AddMvc();
			services.AddOptions();

			services.AddSwaggerGen(c =>
			{
				c.SwaggerDoc("v1", new OpenApiInfo
				{
					Version = "v1",
					Title = "My API",
					Description = ""
				});
			});

			services.AddSingleton<IOrdersService, OrdersService>();
			services.AddTransient<IHandleMessages<OrderCreated>, OrderCreatedMessageHandler>();
		}

		public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
		{
			if (env.IsDevelopment())
			{
				app.UseDeveloperExceptionPage();
			}

			app.ApplicationServices.UseRebus(bus => bus.Advanced.SyncBus.Subscribe<OrderCreated>());

			app.UseStaticFiles();
			app.UseRouting();

			app.UseSwagger();
			app.UseSwaggerUI(
				c =>
				{
					c.SwaggerEndpoint("./v1/swagger.json", "My API V1");
					c.DocExpansion(DocExpansion.None);
				});
			app.UseEndpoints(endpoints => endpoints.MapControllers());
		}
	}
}