using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using IdentityService.Domain;
using BCrypt.Net;

namespace IdentityService.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private static readonly List<User> _users = new List<User>();
    private readonly IConfiguration _configuration;
    public AuthController(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    [HttpPost("register")]
    public IActionResult Register([FromBody] RegisterRequest request)
    {
        if (_users.Any(u => u.Email == request.Email))
            return BadRequest("Usuário já existe.");

        var passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);
        
        var newUser = new User(request.Email, passwordHash);
        
        if (request.Email.Contains("aprovado"))
            newUser.ApproveKyc();

        _users.Add(newUser);
        
        return Ok(new { newUser.Id, newUser.Email, newUser.IsKycApproved });
    }

    [HttpPost("login")]
    public IActionResult Login([FromBody] LoginRequest request)
    {
        var user = _users.FirstOrDefault(u => u.Email == request.Email);
        
        if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            return Unauthorized();

        var jwtSecret = _configuration["JwtSettings:Secret"]!;
        var key = Encoding.ASCII.GetBytes(jwtSecret);

        var tokenHandler = new JwtSecurityTokenHandler();
        
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim("KycApproved", user.IsKycApproved.ToString())
            }),
            Expires = DateTime.UtcNow.AddHours(2),
            Issuer = _configuration["JwtSettings:Issuer"],
            Audience = _configuration["JwtSettings:Audience"],
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return Ok(new { Token = tokenHandler.WriteToken(token) });
    }
}

public record RegisterRequest(string Email, string Password);
public record LoginRequest(string Email, string Password);