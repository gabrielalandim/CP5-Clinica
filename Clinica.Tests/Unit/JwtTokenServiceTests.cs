using Clinica.Domain.Entities;
using Clinica.Infrastructure.Security;
using Microsoft.Extensions.Configuration;
using System.IdentityModel.Tokens.Jwt;

namespace Clinica.Tests.Unit;

public class JwtTokenServiceTests
{
    private static JwtTokenService Criar() => new(new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Secretkey"] = "chave-de-teste-com-mais-de-trinta-e-dois-caracteres-123456"
        })
        .Build());

    [Fact]
    public void GerarToken_ContemEmailERole_EExpiraNoFuturo()
    {
        var token = Criar().GerarToken(new UserEntity { Email = "admin@clinica.com", Role = "admin" });

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);

        Assert.Contains(jwt.Claims, c => c.Type == "name" && c.Value == "admin@clinica.com");
        Assert.Contains(jwt.Claims, c => c.Type == "role" && c.Value == "admin");
        Assert.True(jwt.ValidTo > DateTime.UtcNow);
    }

    [Fact]
    public void GerarToken_SemSecretkey_LancaInvalidOperation()
    {
        var service = new JwtTokenService(new ConfigurationBuilder().Build());

        Assert.Throws<InvalidOperationException>(() => service.GerarToken(new UserEntity { Email = "x@x.com", Role = "user" }));
    }
}
