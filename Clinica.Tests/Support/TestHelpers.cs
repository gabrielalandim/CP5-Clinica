using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace Clinica.Tests.Support;

public static class TestHelpers
{
    public const string AdminEmail = "admin@clinica.com";
    public const string AdminSenha = "Admin@123";
    public const string UserEmail = "user@clinica.com";
    public const string UserSenha = "User@123";

    public static async Task<HttpClient> ClientAutenticadoAsync(this ApiFactory factory, string email, string senha)
    {
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth", new { email, senha });
        response.EnsureSuccessStatusCode();

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var jwt = doc.RootElement.GetProperty("jwt").GetString()!;

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwt);
        return client;
    }

    public static async Task<JsonDocument> LerJsonAsync(this HttpResponseMessage response) =>
        JsonDocument.Parse(await response.Content.ReadAsStringAsync());

    public static string EmailUnico() => $"paciente-{Guid.NewGuid():N}@teste.com";

    public static async Task<int> CriarPacienteAsync(HttpClient client, string? email = null, string cep = "01001-000")
    {
        var response = await client.PostAsJsonAsync("/api/pacientes", new
        {
            nome = "Paciente de Teste",
            email = email ?? EmailUnico(),
            cep
        });
        response.EnsureSuccessStatusCode();

        using var doc = await response.LerJsonAsync();
        return doc.RootElement.GetProperty("id").GetInt32();
    }
}
