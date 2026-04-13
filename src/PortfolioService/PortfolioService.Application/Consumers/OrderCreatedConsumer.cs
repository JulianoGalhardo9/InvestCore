using Microsoft.Extensions.Logging;
using MassTransit;
using PortfolioService.Application.IntegrationEvents;

namespace PortfolioService.Application.Consumers;
public class OrderCreatedConsumer : IConsumer<OrderCreatedIntegrationEvent>
{
    private readonly ILogger<OrderCreatedConsumer> _logger;

    public OrderCreatedConsumer(ILogger<OrderCreatedConsumer> logger)
    {
        _logger = logger;
    }
    public Task Consume(ConsumeContext<OrderCreatedIntegrationEvent> context)
    {
        var message = context.Message;

        if (message.Status == 2)
        {
            _logger.LogInformation($"[CUSTÓDIA] Ordem {message.OrderId} recebida! Creditando {message.Quantity} de {message.AssetSymbol} para o cliente {message.ClientId}.");
        }
        else
        {
            _logger.LogWarning($"[CUSTÓDIA] Ordem {message.OrderId} ignorada. Status não é Executada (Status atual: {message.Status}).");
        }

        return Task.CompletedTask;
    }
}