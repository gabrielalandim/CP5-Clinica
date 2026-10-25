using Clinica.Tests.Support;
using System.Net;
using System.Net.Http.Json;

namespace Clinica.Tests.Integration;

[Collection(ApiCollection.Name)]
public class AuthApiTests
{
    private readonly ApiFactory _factory;

    public AuthApiTests(ApiFactory factory) => _factory = factory;

    [Fact]
    public async Task Endpoint_Protegido_SemToken_Retorna401()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/pacientes");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        using var doc = await response.LerJsonAsync();
        Assert.Equal("NAO_AUTENTICADO", doc.RootElement.GetProperty("code").GetString());
    }

    [Fact]
    public async Task Endpoint_Protegido_ComTokenInvalido_Retorna401()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "token.invalido.123");

        var response = await client.GetAsync("/api/consultas");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_CredenciaisInvalidas_Retorna401()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth", new { email = TestHelpers.AdminEmail, senha = "senha-errada" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_Admin_RetornaJwtEAcessaEndpointProtegido()
    {
        var client = await _factory.ClientAutenticadoAsync(TestHelpers.AdminEmail, TestHelpers.AdminSenha);

        var response = await client.GetAsync("/api/pacientes");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Registro_EmailNovo_Retorna201ComPerfilUser()
    {
        var client = _factory.CreateClient();
        var email = $"novo-{Guid.NewGuid():N}@teste.com";

        var response = await client.PostAsJsonAsync("/api/user", new
        {
            nome = "Novo Usuário",
            email,
            password = "Senha@123",
            confirmPassword = "Senha@123"
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        using var doc = await response.LerJsonAsync();
        Assert.Equal("user", doc.RootElement.GetProperty("role").GetString());
    }
}
