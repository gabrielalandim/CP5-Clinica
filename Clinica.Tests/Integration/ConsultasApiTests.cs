using Clinica.Tests.Support;
using System.Net;
using System.Net.Http.Json;

namespace Clinica.Tests.Integration;

[Collection(ApiCollection.Name)]
public class ConsultasApiTests
{
    private readonly ApiFactory _factory;

    public ConsultasApiTests(ApiFactory factory) => _factory = factory;

    // Data única por execução para não colidir com dados que já estejam no banco
    private static string DataUnica()
    {
        var dias = 365 + Random.Shared.Next(1, 3000);
        return DateTime.Today.AddDays(dias).AddHours(Random.Shared.Next(7, 19)).AddMinutes(Random.Shared.Next(0, 59))
            .ToString("yyyy-MM-ddTHH:mm:ss");
    }

    [Fact]
    public async Task Agendar_Retorna201_E_DepoisCancelar_Retorna200_E_CancelarDeNovo_Retorna409()
    {
        var client = await _factory.ClientAutenticadoAsync(TestHelpers.UserEmail, TestHelpers.UserSenha);
        var pacienteId = await TestHelpers.CriarPacienteAsync(client);

        var agendar = await client.PostAsJsonAsync("/api/consultas",
            new { pacienteId, dataHora = DataUnica(), especialidade = "Cardiologia", valor = 300 });

        Assert.Equal(HttpStatusCode.Created, agendar.StatusCode);
        using var doc = await agendar.LerJsonAsync();
        var id = doc.RootElement.GetProperty("id").GetInt32();
        Assert.Equal("Agendada", doc.RootElement.GetProperty("status").GetString());

        var cancelar = await client.DeleteAsync($"/api/consultas/{id}");
        Assert.Equal(HttpStatusCode.OK, cancelar.StatusCode);

        var cancelarDeNovo = await client.DeleteAsync($"/api/consultas/{id}");
        Assert.Equal(HttpStatusCode.Conflict, cancelarDeNovo.StatusCode);
    }

    [Fact]
    public async Task Agendar_HorarioJaOcupado_Retorna409()
    {
        var client = await _factory.ClientAutenticadoAsync(TestHelpers.UserEmail, TestHelpers.UserSenha);
        var pacienteId = await TestHelpers.CriarPacienteAsync(client);
        var data = DataUnica();
        var body = new { pacienteId, dataHora = data, especialidade = "Dermatologia", valor = 200 };

        var primeira = await client.PostAsJsonAsync("/api/consultas", body);
        var segunda = await client.PostAsJsonAsync("/api/consultas", body);

        Assert.Equal(HttpStatusCode.Created, primeira.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, segunda.StatusCode);
    }

    [Fact]
    public async Task Agendar_PacienteInexistente_Retorna404()
    {
        var client = await _factory.ClientAutenticadoAsync(TestHelpers.UserEmail, TestHelpers.UserSenha);

        var response = await client.PostAsJsonAsync("/api/consultas",
            new { pacienteId = 999999999, dataHora = DataUnica(), especialidade = "Ortopedia", valor = 150 });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Agendar_DataNoPassado_Retorna400()
    {
        var client = await _factory.ClientAutenticadoAsync(TestHelpers.UserEmail, TestHelpers.UserSenha);
        var pacienteId = await TestHelpers.CriarPacienteAsync(client);

        var response = await client.PostAsJsonAsync("/api/consultas",
            new { pacienteId, dataHora = "2020-01-01T10:00:00", especialidade = "Clínico", valor = 100 });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Listar_PorPaciente_RetornaPageResult()
    {
        var client = await _factory.ClientAutenticadoAsync(TestHelpers.UserEmail, TestHelpers.UserSenha);
        var pacienteId = await TestHelpers.CriarPacienteAsync(client);

        await client.PostAsJsonAsync("/api/consultas", new { pacienteId, dataHora = DataUnica(), especialidade = "Pediatria", valor = 120 });

        var response = await client.GetAsync($"/api/consultas?pacienteId={pacienteId}&page=1&pageSize=10");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var doc = await response.LerJsonAsync();
        Assert.Equal(1, doc.RootElement.GetProperty("totalItems").GetInt32());
        Assert.Equal(1, doc.RootElement.GetProperty("totalPages").GetInt32());
    }

    [Fact]
    public async Task ObterPorId_RetornaLinksHateoasDeCancelamentoEPaciente()
    {
        var client = await _factory.ClientAutenticadoAsync(TestHelpers.UserEmail, TestHelpers.UserSenha);
        var pacienteId = await TestHelpers.CriarPacienteAsync(client);

        var agendar = await client.PostAsJsonAsync("/api/consultas",
            new { pacienteId, dataHora = DataUnica(), especialidade = "Neurologia", valor = 400 });
        agendar.EnsureSuccessStatusCode();
        using var criada = await agendar.LerJsonAsync();
        var id = criada.RootElement.GetProperty("id").GetInt32();

        var response = await client.GetAsync($"/api/consultas/{id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var doc = await response.LerJsonAsync();
        var rels = doc.RootElement.GetProperty("links").EnumerateArray()
            .Select(l => l.GetProperty("rel").GetString()).ToList();
        Assert.Contains("self", rels);
        Assert.Contains("paciente", rels);
        Assert.Contains("cancelar_consulta", rels);
    }

    [Fact]
    public async Task ObterPorId_Inexistente_Retorna404ComCodigoDevolvidoPeloRepository()
    {
        var client = await _factory.ClientAutenticadoAsync(TestHelpers.UserEmail, TestHelpers.UserSenha);

        var response = await client.GetAsync("/api/consultas/999999999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        using var doc = await response.LerJsonAsync();
        Assert.Equal("CONSULTA_NAO_ENCONTRADA", doc.RootElement.GetProperty("code").GetString());
    }
}
