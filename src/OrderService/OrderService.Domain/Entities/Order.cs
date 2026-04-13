using OrderService.Domain.Enums;
namespace OrderService.Domain.Entities;

public class Order
{
    public Guid Id { get; private set; }
    public Guid ClientId { get; private set; }
    public string AssetSymbol { get; private set; }
    public int Quantity { get; private set; }
    public decimal Price { get; private set; }
    public OrderStatus Status { get; private set; }
    public DateTime CreatedAt { get; private set; }

    public Order(Guid clientId, string assetSymbol, int quantity, decimal price)
    {
        Id = Guid.NewGuid();
        ClientId = clientId;
        AssetSymbol = assetSymbol;
        Quantity = quantity;
        Price = price;
        Status = OrderStatus.Pending;
        CreatedAt = DateTime.UtcNow;
    }

    public void Execute()
    {
        if (Status == OrderStatus.Pending)
            Status = OrderStatus.Executed;
    }

    public void Reject()
    {
        if (Status == OrderStatus.Pending)
            Status = OrderStatus.Rejected;
    }
}