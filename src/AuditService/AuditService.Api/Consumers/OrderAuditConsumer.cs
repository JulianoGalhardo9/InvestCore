using MassTransit;
using System.Text.Json;
using AuditService.Infrastructure.Data;
using AuditService.Domain.Entities;
using InvestCore.Messages;

namespace AuditService.Api.Consumers;
public class OrderAuditConsumer : IConsumer<OrderCreatedIntegrationEvent>
{
    private readonly AuditContext _dbContext;

    public OrderAuditConsumer(AuditContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task Consume(ConsumeContext<OrderCreatedIntegrationEvent> context)
    {
        var eventData = JsonSerializer.Serialize(context.Message);
        
        var auditLog = new AuditLog(nameof(OrderCreatedIntegrationEvent), eventData);
        
        _dbContext.AuditLogs.Add(auditLog);
        await _dbContext.SaveChangesAsync();
    }
}