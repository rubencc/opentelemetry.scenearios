using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using NServiceBus;
using Opentelemetry.Scenarios.Console.Producer.DisableOT;

public class Program
{
    public static async Task Main(string[]? args)
    {
        var builder = Host.CreateDefaultBuilder(args);

        builder.ConfigureAppConfiguration(x =>
        {
            x.AddJsonFile("appsettings.json", optional: false);
        });

        builder.UseNServiceBus(context =>
        {
            var connectionString = context.Configuration.GetValue<string>("RabbitMq:Url");

            var endpointConfiguration = new EndpointConfiguration("Console.Producer.DisableOT");

            var transport = endpointConfiguration.UseTransport<RabbitMQTransport>();

            var settings = new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver(),
                NullValueHandling = NullValueHandling.Ignore,
                DateParseHandling = DateParseHandling.DateTime,
                DateTimeZoneHandling = DateTimeZoneHandling.Utc,
                TypeNameHandling = TypeNameHandling.Auto
            };
            var serialization = endpointConfiguration.UseSerialization<NewtonsoftJsonSerializer>();
            serialization.Settings(settings);

            endpointConfiguration.EnableInstallers();
            endpointConfiguration.EnableOpenTelemetry();

            endpointConfiguration.AssemblyScanner();

            var conventionsBuilder = endpointConfiguration.Conventions();
            conventionsBuilder.DefiningEventsAs(t => t.FullName != null && t.FullName.Contains("Test"));

            transport.ConnectionString(connectionString);
            transport.UseConventionalRoutingTopology(QueueType.Quorum);

            var scanner = endpointConfiguration.AssemblyScanner();
            scanner.ScanAppDomainAssemblies = true;
            scanner.ScanAssembliesInNestedDirectories = true;

            endpointConfiguration.SendOnly();
            return endpointConfiguration;
        });
        
        builder.ConfigureServices(services =>
        {
            services.AddHostedService<SenderWorker>();
        });

        var app = builder.Build();

        await app.RunAsync().ConfigureAwait(false);
        
    }
}