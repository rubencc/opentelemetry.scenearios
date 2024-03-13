namespace Opentelemetry.Scenarios.Console.Producer.EnableOT;

using Events;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NServiceBus;

public class SenderWorker : BackgroundService
{
    private readonly IMessageSession messageSession;

    public SenderWorker(IMessageSession messageSession)
    {
        this.messageSession = messageSession;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            var round = 0;
            while (!stoppingToken.IsCancellationRequested)
            {
                await messageSession.Publish(new TestEvent(){Source = "Console.Producer.EnableOT" })
                    .ConfigureAwait(false);

                System.Console.WriteLine($"Message ###{round}###");

                await Task.Delay(1_000, stoppingToken)
                    .ConfigureAwait(false);
                round++;
            }
        }
        catch (OperationCanceledException)
        {
            // graceful shutdown
        }
    }
}