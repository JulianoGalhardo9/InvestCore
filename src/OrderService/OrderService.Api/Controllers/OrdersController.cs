using Microsoft.AspNetCore.Mvc;
using OrderService.Domain.Entities;
using OrderService.Application;

namespace OrderService.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private static readonly List<Order> _orders = new List<Order>();

    [HttpPost]
    public IActionResult Create([FromBody] CreateOrderRequest request)
    {
        var order = new Order(request.ClientId, request.AssetSymbol, request.Quantity, request.Price);
        
        if (order.Price > 1000)
            order.Reject();
        else
            order.Execute();

        _orders.Add(order);

        return Ok(order);
    }

    [HttpGet]
    public IActionResult GetAll()
    {
        return Ok(_orders);
    }
}