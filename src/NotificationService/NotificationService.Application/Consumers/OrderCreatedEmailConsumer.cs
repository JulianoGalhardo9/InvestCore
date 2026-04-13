using Microsoft.Extensions.Logging;
using MassTransit;
using InvestCore.Messages;

namespace NotificationService.Application.Consumers;

public class OrderCreatedEmailConsumer : IConsumer<OrderCreatedIntegrationEvent>
{
    private readonly ILogger<OrderCreatedEmailConsumer> _logger;

    public OrderCreatedEmailConsumer(ILogger<OrderCreatedEmailConsumer> logger)
    {
        _logger = logger;
    }

    public Task Consume(ConsumeContext<OrderCreatedIntegrationEvent> context)
    {
        var message = context.Message;

        if (message.Status == 2)
        {
            _logger.LogInformation($"[E-MAIL ENVIADO] Assunto: Ordem Executada! Corpo: Sua compra de {message.Quantity}x {message.AssetSymbol} foi concluída com sucesso.");
        }
        else if (message.Status == 3)
        {
            _logger.LogWarning($"[E-MAIL ENVIADO] Assunto: Ordem Rejeitada. Corpo: Não foi possível comprar {message.AssetSymbol}. Verifique seu saldo ou perfil de risco.");
        }

        return Task.CompletedTask;
    }
}