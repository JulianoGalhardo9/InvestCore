using Microsoft.EntityFrameworkCore;
using MassTransit;
using OrderService.Infrastructure.Data;
using OrderService.Infrastructure.BackgroundJobs;
// A PEÇA QUE FALTAVA (O namespace correto)
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// CONFIGURA O SWAGGER
builder.Services.AddSwaggerGen(c =>
{
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "Insira o token JWT desta maneira: Bearer {seu_token}",
        Name = "Authorization",
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// CONFIGURA O BANCO DE DADOS
builder.Services.AddDbContext<OrderContext>(options =>
    options.UseSqlServer("Server=host.docker.internal,14333;Database=OrderDb;User Id=sa;Password=InvestCore2026!;TrustServerCertificate=True;"));

// CONFIGURA A MENSAGERIA (RABBITMQ)
builder.Services.AddMassTransit(x =>
{
    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host("host.docker.internal", 5673, "/", h =>
        {
            h.Username("guest");
            h.Password("guest");
        });
    });
});

// CONFIGURA O CARTEIRO
builder.Services.AddHostedService<OutboxProcessorBackgroundService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();

app.Run();