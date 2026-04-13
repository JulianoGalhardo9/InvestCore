using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using OrderService.Domain.Entities;
using OrderService.Infrastructure.Data;
using OrderService.Application;

namespace OrderService.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly OrderContext _context;
    public OrdersController(OrderContext context)
    {
        _context = context;
    }

    [HttpPost]
    public IActionResult Create([FromBody] CreateOrderRequest request)
    {
        var order = new Order(request.ClientId, request.AssetSymbol, request.Quantity, request.Price);

        if (order.Price > 1000) order.Reject();
        else order.Execute();

        _context.Orders.Add(order);

        var eventContent = JsonSerializer.Serialize(new { order.Id, order.ClientId, order.AssetSymbol, order.Quantity, order.Price, order.Status });

        var outboxMessage = new OutboxMessage("OrderCreatedEvent", eventContent);
        
        _context.OutboxMessages.Add(outboxMessage);

        _context.SaveChanges();

        return Ok(order);
    }
}