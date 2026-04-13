namespace OrderService.Application;

public record OrderCreatedIntegrationEvent(
    Guid OrderId, 
    Guid ClientId, 
    string AssetSymbol, 
    int Quantity, 
    decimal Price, 
    int Status
);