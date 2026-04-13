using Microsoft.AspNetCore.Mvc;
using ClientService.Domain;
using ClientService.Application;

namespace ClientService.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ClientsController : ControllerBase
{
    private static readonly List<Client> _clients = new List<Client>();

    [HttpPost]
    public IActionResult Create([FromBody] CreateClientRequest request)
    {
        var suitability = (SuitabilityProfile)request.ProfileId;

        if (!Enum.IsDefined(typeof(SuitabilityProfile), suitability))
        {
            return BadRequest("Perfil de suitability inválido.");
        }

        var newClient = new Client(request.UserId, suitability);

        _clients.Add(newClient);

        return Ok(new { newClient.Id, newClient.UserId, newClient.Profile });
    }

    [HttpGet("{userId:guid}")]
    public IActionResult GetByUserId(Guid userId)
    {
        var client = _clients.FirstOrDefault(c => c.UserId == userId);

        if (client == null)
        {
            return NotFound("Cliente não encontrado.");
        }

        return Ok(new { client.Id, client.UserId, client.Profile });
    }
}