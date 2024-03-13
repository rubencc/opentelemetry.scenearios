namespace Opentelemetry.Scenarios.Console.Consumer;

using Events;
using NServiceBus;
using Console = System.Console;

public class Handler : IHandleMessages<TestEvent>
{
    public Task Handle(TestEvent message, IMessageHandlerContext context)
    {
        Console.WriteLine("############");
        Console.WriteLine($"############ Message received from {message.Source}");
        Console.WriteLine("############");
        Console.WriteLine();
        
        return Task.CompletedTask;
    }
}