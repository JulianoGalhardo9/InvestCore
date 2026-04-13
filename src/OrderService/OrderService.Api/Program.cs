using OrderService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<OrderContext>(options =>
    options.UseSqlServer("Server=localhost,14333;Database=OrderDb;User Id=sa;Password=InvestCore2026!;TrustServerCertificate=True;"));

var app = builder.Build();
app.MapGet("/", () => "Hello World!");

app.Run();
