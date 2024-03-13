using Newtonsoft.Json;
using NServiceBus;
using Events;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Serialization;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;


var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddOpenTelemetry().WithTracing(x =>
{
    x.SetResourceBuilder(ResourceBuilder.CreateDefault()
        .AddService(serviceName: "Console.Producer.EnableOT", serviceVersion: "1.0.0"));

    x.AddSource("NServiceBus.*")
        .AddAspNetCoreInstrumentation()
        .AddConsoleExporter();
});

builder.Host.UseNServiceBus(context =>
{
    var connectionString = context.Configuration.GetValue<string>("RabbitMq:Url");

    var endpointConfiguration = new EndpointConfiguration("WebApp.Producer.DisableOT");

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

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.MapPost("Producer/EnableOt/publish", async (IMessageSession context) =>
{
    await context.Publish(new TestEvent()
    {
        Source = "WebApp.Producer.EnableOT with asp instrumentation"
    }).ConfigureAwait(false);


    return Results.Accepted();
});

app.Run();