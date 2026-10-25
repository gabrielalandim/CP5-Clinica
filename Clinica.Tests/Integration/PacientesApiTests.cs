using Clinica.Tests.Support;
using System.Net;
using System.Net.Http.Json;

namespace Clinica.Tests.Integration;

[Collection(ApiCollection.Name)]
public class PacientesApiTests
{
    private readonly ApiFactory _factory;

    public PacientesApiTests(ApiFactory factory) => _factory = factory;

    [Fact]
    public async Task Criar_Retorna201_E_GetPorId_RetornaLinksHateoas()
    {
        var client = await _factory.ClientAutenticadoAsync(TestHelpers.UserEmail, TestHelpers.UserSenha);

        var id = await TestHelpers.CriarPacienteAsync(client);
        var response = await client.GetAsync($"/api/pacientes/{id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var doc = await response.LerJsonAsync();
        Assert.Equal("São Paulo", doc.RootElement.GetProperty("cidade").GetString());

        var rels = doc.RootElement.GetProperty("links").EnumerateArray()
            .Select(l => l.GetProperty("rel").GetString()).ToList();
        Assert.Contains("self", rels);
        Assert.Contains("update_paciente", rels);
        Assert.Contains("delete_paciente", rels);
    }

    [Fact]
    public async Task GetPorId_Inexistente_Retorna404()
    {
        var client = await _factory.ClientAutenticadoAsync(TestHelpers.UserEmail, TestHelpers.UserSenha);

        var response = await client.GetAsync("/api/pacientes/999999999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Criar_EmailDuplicado_Retorna409()
    {
        var client = await _factory.ClientAutenticadoAsync(TestHelpers.UserEmail, TestHelpers.UserSenha);
        var email = TestHelpers.EmailUnico();

        await TestHelpers.CriarPacienteAsync(client, email);
        var response = await client.PostAsJsonAsync("/api/pacientes", new { nome = "Outro", email, cep = "01001000" });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Criar_CepInexistente_Retorna404()
    {
        var client = await _factory.ClientAutenticadoAsync(TestHelpers.UserEmail, TestHelpers.UserSenha);

        var response = await client.PostAsJsonAsync("/api/pacientes",
            new { nome = "Teste", email = TestHelpers.EmailUnico(), cep = FakeEnderecoService.CepInexistente });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Criar_ServicoDeCepForaDoAr_Retorna503()
    {
        var client = await _factory.ClientAutenticadoAsync(TestHelpers.UserEmail, TestHelpers.UserSenha);

        var response = await client.PostAsJsonAsync("/api/pacientes",
            new { nome = "Teste", email = TestHelpers.EmailUnico(), cep = FakeEnderecoService.CepServicoForaDoAr });

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
    }

    [Fact]
    public async Task Criar_ComEmailInvalido_Retorna400()
    {
        var client = await _factory.ClientAutenticadoAsync(TestHelpers.UserEmail, TestHelpers.UserSenha);

        var response = await client.PostAsJsonAsync("/api/pacientes", new { nome = "Teste", email = "nao-e-email", cep = "01001000" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Listar_RetornaPageResultComMetadadosELinks()
    {
        var client = await _factory.ClientAutenticadoAsync(TestHelpers.UserEmail, TestHelpers.UserSenha);
        await TestHelpers.CriarPacienteAsync(client);
        await TestHelpers.CriarPacienteAsync(client);

        var response = await client.GetAsync("/api/pacientes?page=1&pageSize=1");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var doc = await response.LerJsonAsync();
        var root = doc.RootElement;

        Assert.Equal(1, root.GetProperty("page").GetInt32());
        Assert.Equal(1, root.GetProperty("pageSize").GetInt32());
        Assert.Equal(1, root.GetProperty("items").GetArrayLength());
        Assert.True(root.GetProperty("totalItems").GetInt32() >= 2);
        Assert.True(root.GetProperty("totalPages").GetInt32() >= 2);
        Assert.True(root.GetProperty("hasNext").GetBoolean());

        var rels = root.GetProperty("links").EnumerateArray().Select(l => l.GetProperty("rel").GetString()).ToList();
        Assert.Contains("self", rels);
        Assert.Contains("next", rels);
        Assert.Contains("create_paciente", rels);
    }

    [Fact]
    public async Task Deletar_ComoUsuarioComum_Retorna403()
    {
        var client = await _factory.ClientAutenticadoAsync(TestHelpers.UserEmail, TestHelpers.UserSenha);
        var id = await TestHelpers.CriarPacienteAsync(client);

        var response = await client.DeleteAsync($"/api/pacientes/{id}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Deletar_ComoAdmin_Retorna204_E_DepoisGetRetorna404()
    {
        var client = await _factory.ClientAutenticadoAsync(TestHelpers.AdminEmail, TestHelpers.AdminSenha);
        var id = await TestHelpers.CriarPacienteAsync(client);

        var delete = await client.DeleteAsync($"/api/pacientes/{id}");
        var get = await client.GetAsync($"/api/pacientes/{id}");

        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, get.StatusCode);
    }

    [Fact]
    public async Task Atualizar_Existente_Retorna200ComDadosNovos()
    {
        var client = await _factory.ClientAutenticadoAsync(TestHelpers.UserEmail, TestHelpers.UserSenha);
        var id = await TestHelpers.CriarPacienteAsync(client);
        var novoEmail = TestHelpers.EmailUnico();

        var response = await client.PutAsJsonAsync($"/api/pacientes/{id}", new { nome = "Nome Atualizado", email = novoEmail });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var doc = await response.LerJsonAsync();
        Assert.Equal("Nome Atualizado", doc.RootElement.GetProperty("nome").GetString());
        Assert.Equal(novoEmail, doc.RootElement.GetProperty("email").GetString());
    }

    [Fact]
    public async Task Atualizar_Inexistente_Retorna404ComCodigoDoErroCustomizado()
    {
        var client = await _factory.ClientAutenticadoAsync(TestHelpers.UserEmail, TestHelpers.UserSenha);

        var response = await client.PutAsJsonAsync("/api/pacientes/999999999", new { nome = "X", email = TestHelpers.EmailUnico() });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        using var doc = await response.LerJsonAsync();
        Assert.Equal("PACIENTE_NAO_ENCONTRADO", doc.RootElement.GetProperty("code").GetString());
    }
}
