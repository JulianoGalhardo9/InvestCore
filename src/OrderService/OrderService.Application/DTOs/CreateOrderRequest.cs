namespace OrderService.Application;
public record CreateOrderRequest(Guid ClientId, string AssetSymbol, int Quantity, decimal Price);