using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using IdentityService.Domain;
using BCrypt.Net;

var builder = WebApplication.CreateBuilder(args);

// 1. Configurando a Autenticação JWT
var jwtSecret = builder.Configuration["JwtSettings:Secret"]!;
var key = Encoding.ASCII.GetBytes(jwtSecret);

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(key),
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["JwtSettings:Issuer"],
            ValidateAudience = true,
            ValidAudience = builder.Configuration["JwtSettings:Audience"],
            ValidateLifetime = true
        };
    });
builder.Services.AddAuthorization();

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

var users = new List<User>();

app.MapPost("/api/auth/register", (RegisterRequest request) =>
{
    if (users.Any(u => u.Email == request.Email))
        return Results.BadRequest("Usuário já existe.");

    var passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);
    
    var newUser = new User(request.Email, passwordHash);
    
    if (request.Email.Contains("aprovado"))
        newUser.ApproveKyc();

    users.Add(newUser);
    return Results.Ok(new { newUser.Id, newUser.Email, newUser.IsKycApproved });
});

app.MapPost("/api/auth/login", (LoginRequest request) =>
{
    var user = users.FirstOrDefault(u => u.Email == request.Email);
    
    if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        return Results.Unauthorized();

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
        Issuer = builder.Configuration["JwtSettings:Issuer"],
        Audience = builder.Configuration["JwtSettings:Audience"],
        SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
    };

    var token = tokenHandler.CreateToken(tokenDescriptor);
    return Results.Ok(new { Token = tokenHandler.WriteToken(token) });
});

app.Run();

record RegisterRequest(string Email, string Password);
record LoginRequest(string Email, string Password);