using Clinica.Domain.Entities;
using Clinica.Domain.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Clinica.Infrastructure.Security;

/// <summary>Gera o JWT assinado com HMAC-SHA256 contendo e-mail (name) e perfil (role).</summary>
public class JwtTokenService : ITokenService
{
    private readonly IConfiguration _configuration;

    public JwtTokenService(IConfiguration configuration) => _configuration = configuration;

    public string GerarToken(UserEntity user)
    {
        var secret = _configuration["Secretkey"]
            ?? throw new InvalidOperationException("Configure 'Secretkey' no appsettings.");

        var key = Encoding.UTF8.GetBytes(secret);

        var descriptor = new SecurityTokenDescriptor
        {
            Expires = DateTime.UtcNow.AddHours(8),
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256),
            Subject = new ClaimsIdentity(new[]
            {
                new Claim("name", user.Email),
                new Claim("role", user.Role)
            })
        };

        var handler = new JwtSecurityTokenHandler();
        return handler.WriteToken(handler.CreateToken(descriptor));
    }
}
