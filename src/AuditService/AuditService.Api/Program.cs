using Microsoft.EntityFrameworkCore;
using MassTransit;
using AuditService.Infrastructure.Data;
using AuditService.Api.Consumers;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AuditContext>(options =>
    options.UseSqlServer("Server=localhost,14333;Database=AuditDb;User Id=sa;Password=InvestCore2026!;TrustServerCertificate=True;"));

builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<OrderAuditConsumer>();

    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host("localhost", 5673, "/", h => { h.Username("guest"); h.Password("guest"); });

        cfg.ReceiveEndpoint("audit-order-created-queue", e =>
        {
            e.ConfigureConsumer<OrderAuditConsumer>(context);
        });
    });
});

var app = builder.Build();
app.Run();