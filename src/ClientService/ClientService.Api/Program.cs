using ClientService.Domain;
using ClientService.Application;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();
var clients = new List<Client>();

app.MapPost("/api/clients", (CreateClientRequest request) =>
{
    var suitability = (SuitabilityProfile)request.ProfileId;

    if (!Enum.IsDefined(typeof(SuitabilityProfile), suitability))
    {
        return Results.BadRequest("Perfil de suitability inválido.");
    }

    var newClient = new Client(request.UserId, suitability);

    clients.Add(newClient);

    return Results.Ok(new { newClient.Id, newClient.UserId, newClient.Profile });
});

app.MapGet("/api/clients/{userId:guid}", (Guid userId) =>
{
    var client = clients.FirstOrDefault(c => c.UserId == userId);

    if (client == null)
        return Results.NotFound("Cliente não encontrado.");

    return Results.Ok(new { client.Id, client.UserId, client.Profile });
});

app.Run();