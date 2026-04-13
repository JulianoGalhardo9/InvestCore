using System.Text.Json;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using MassTransit;
using OrderService.Infrastructure.Data;
using InvestCore.Messages;

namespace OrderService.Infrastructure.BackgroundJobs;

public class OutboxProcessorBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;

    public OutboxProcessorBackgroundService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            using var scope = _serviceProvider.CreateScope();
            
            var dbContext = scope.ServiceProvider.GetRequiredService<OrderContext>();
            
            var publishEndpoint = scope.ServiceProvider.GetRequiredService<IPublishEndpoint>();

            var messages = dbContext.OutboxMessages.Where(m => m.ProcessedDate == null).ToList();

            foreach (var message in messages)
            {
                if (message.Type == "OrderCreatedEvent")
                {
                    var eventPayload = JsonSerializer.Deserialize<OrderCreatedIntegrationEvent>(message.Content);
                    
                    if (eventPayload != null)
                    {
                        await publishEndpoint.Publish(eventPayload, stoppingToken);
                        
                        message.MarkAsProcessed();
                    }
                }
            }

            await dbContext.SaveChangesAsync(stoppingToken);

            await Task.Delay(5000, stoppingToken);
        }
    }
}