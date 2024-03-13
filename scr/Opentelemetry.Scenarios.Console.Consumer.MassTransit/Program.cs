using MassTransit;
using MassTransit.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenTelemetry;
using OpenTelemetry.Resources;
using Opentelemetry.Scenarios.Console.Consumer.MassTransit;
using OpenTelemetry.Trace;

public class Program
{
    public static async Task Main(string[]? args)
    {
        var builder = Host.CreateDefaultBuilder(args);
        
        builder.ConfigureAppConfiguration(x =>
        {
            x.AddJsonFile("appsettings.json", optional: false);
        });
        
        builder.ConfigureServices((builderContext, services) =>
        {
            
            // services.AddOpenTelemetry().WithTracing(x =>
            // {
            //     x.SetResourceBuilder(ResourceBuilder.CreateDefault()
            //         .AddService(serviceName: "Console.Consumer.MassTransit", serviceVersion: "1.0.0"));
            //
            //     x.AddSource("MassTransit.*")
            //         .AddConsoleExporter();
            //
            // });

            Sdk.CreateTracerProviderBuilder()
                .SetResourceBuilder(ResourceBuilder.CreateDefault()
                    .AddService(serviceName: "Console.Consumer.MassTransit", serviceVersion: "1.0.0"))
                .AddSource(DiagnosticHeaders.DefaultListenerName) // MassTransit ActivitySource
                .AddConsoleExporter() //
                .Build();
            
            services.AddMassTransit( service =>
            {
                service.AddConsumer<EventConsumer>();
                service.UsingRabbitMq((context, configurator) =>
                {
                    configurator.Host(new Uri(builderContext.Configuration.GetValue<string>("RabbitMq:Url")));
                    
                    configurator.UseInstrumentation(serviceName: "MassTransit");
                    configurator.ClearSerialization();
                    configurator.AddRawJsonSerializer(RawSerializerOptions.All);
                    configurator.SupportNServiceBusJsonDeserializer();
                    
                    configurator.ReceiveEndpoint("Console.Consumer.MassTransit", e =>
                    {
                        e.ConfigureConsumer<EventConsumer>(context);
                        e.SetQuorumQueue();

                        e.UseMessageRetry(r =>
                        {
                            r.Intervals(100, 500, 1000, 2000);
                            r.Ignore(typeof(ArgumentNullException), typeof(ArgumentNullException));
                        });
                    });

                });
            });
            
        });

        var app = builder.Build();

        await app.RunAsync().ConfigureAwait(false);
        
    }
}