namespace Opentelemetry.Scenarios.Console.Consumer.MassTransit;

using Events;
using global::MassTransit;
using Console = System.Console;

public class EventConsumer : IConsumer<TestEvent>
{

    public Task Consume(ConsumeContext<TestEvent> context)
    {
        Console.WriteLine("############");
        Console.WriteLine($"############ Message received from {context.Message.Source}");
        Console.WriteLine("############");
        Console.WriteLine();
        
        return Task.CompletedTask;
    }
}