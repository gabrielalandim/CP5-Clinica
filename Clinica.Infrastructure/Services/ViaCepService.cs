using Clinica.Domain.Errors;
using Clinica.Domain.Interfaces;
using Clinica.Domain.Models;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Polly.CircuitBreaker;
using Polly.Timeout;
using System.Net;
using System.Text.Json;

namespace Clinica.Infrastructure.Services;

/// <summary>
/// Consome a API pública ViaCEP (serviço externo).
/// <list type="bullet">
/// <item>HttpClient tipado via IHttpClientFactory com resilience handler (retry + circuit breaker + timeout) – ver Bootstrap.</item>
/// <item>Cache em memória dos CEPs já consultados (otimização: evita chamadas repetidas).</item>
/// <item>Tratamento de resposta: CEP inexistente → NotFoundException; falha/instabilidade → ExternalServiceException.</item>
/// </list>
/// </summary>
public class ViaCepService : IEnderecoService
{
    private static readonly TimeSpan CacheDuration = TimeSpan.FromHours(12);

    private readonly HttpClient _http;
    private readonly IMemoryCache _cache;
    private readonly ILogger<ViaCepService> _logger;

    public ViaCepService(HttpClient http, IMemoryCache cache, ILogger<ViaCepService> logger)
    {
        _http = http;
        _cache = cache;
        _logger = logger;
    }

    public async Task<EnderecoModel> ObterPorCepAsync(string cep)
    {
        var cacheKey = $"viacep:{cep}";
        if (_cache.TryGetValue(cacheKey, out EnderecoModel? emCache) && emCache is not null)
        {
            _logger.LogInformation("CEP {Cep} obtido do cache", cep);
            return emCache;
        }

        try
        {
            using var response = await _http.GetAsync($"{cep}/json/");

            if (response.StatusCode == HttpStatusCode.BadRequest)
                throw new NotFoundException($"CEP inválido: {cep}.", "CEP_INVALIDO");

            response.EnsureSuccessStatusCode();

            using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            var root = doc.RootElement;

            // ViaCEP responde 200 com {"erro": true} quando o CEP não existe
            if (root.TryGetProperty("erro", out _))
                throw new NotFoundException($"CEP não encontrado: {cep}.", "CEP_NAO_ENCONTRADO");

            string Get(string name) =>
                root.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() ?? "" : "";

            var endereco = new EnderecoModel(cep, Get("logradouro"), Get("localidade"), Get("uf"));

            _cache.Set(cacheKey, endereco, CacheDuration);
            _logger.LogInformation("CEP {Cep} consultado no ViaCEP: {Cidade}/{Uf}", cep, endereco.Cidade, endereco.Uf);

            return endereco;
        }
        catch (Exception ex) when (ex is HttpRequestException
                                      or TaskCanceledException
                                      or BrokenCircuitException
                                      or TimeoutRejectedException
                                      or JsonException)
        {
            _logger.LogError(ex, "Falha ao consultar o ViaCEP para o CEP {Cep}", cep);
            throw new ExternalServiceException("O serviço de consulta de CEP está indisponível. Tente novamente.", ex);
        }
    }
}
