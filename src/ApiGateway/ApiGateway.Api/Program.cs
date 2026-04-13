var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger-identity/v1/swagger.json", "Identity API (Portaria)");
        c.SwaggerEndpoint("/swagger-order/v1/swagger.json", "Order API (Motor)");
    });
}

// Ativa o roteador
app.MapReverseProxy();

app.Run();