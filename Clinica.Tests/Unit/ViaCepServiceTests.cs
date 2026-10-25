using Clinica.Domain.Errors;
using Clinica.Infrastructure.Services;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using System.Net;
using System.Text;

namespace Clinica.Tests.Unit;

/// <summary>
/// Testa o tratamento de resposta do serviço externo (ViaCEP) sem acessar a internet:
/// um HttpMessageHandler falso devolve as respostas que o ViaCEP devolveria.
/// </summary>
public class ViaCepServiceTests
{
    private sealed class StubHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _responder;
        public int Chamadas { get; private set; }

        public StubHandler(Func<HttpRequestMessage, HttpResponseMessage> responder) => _responder = responder;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Chamadas++;
            return Task.FromResult(_responder(request));
        }
    }

    private static (ViaCepService Service, StubHandler Handler) Criar(HttpStatusCode status, string json)
    {
        var handler = new StubHandler(_ => new HttpResponseMessage(status)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        });

        var http = new HttpClient(handler) { BaseAddress = new Uri("https://viacep.com.br/ws/") };
        var service = new ViaCepService(http, new MemoryCache(new MemoryCacheOptions()), NullLogger<ViaCepService>.Instance);
        return (service, handler);
    }

    [Fact]
    public async Task CepValido_RetornaEnderecoMapeado()
    {
        var (service, _) = Criar(HttpStatusCode.OK,
            """{"cep":"01001-000","logradouro":"Praça da Sé","localidade":"São Paulo","uf":"SP"}""");

        var endereco = await service.ObterPorCepAsync("01001000");

        Assert.Equal("01001000", endereco.Cep);
        Assert.Equal("Praça da Sé", endereco.Logradouro);
        Assert.Equal("São Paulo", endereco.Cidade);
        Assert.Equal("SP", endereco.Uf);
    }

    [Fact]
    public async Task MesmoCep_SegundaChamadaVemDoCache()
    {
        var (service, handler) = Criar(HttpStatusCode.OK,
            """{"logradouro":"Praça da Sé","localidade":"São Paulo","uf":"SP"}""");

        await service.ObterPorCepAsync("01001000");
        await service.ObterPorCepAsync("01001000");

        Assert.Equal(1, handler.Chamadas);
    }

    [Fact]
    public async Task CepInexistente_ViaCepRespondeErroTrue_LancaNotFound()
    {
        var (service, _) = Criar(HttpStatusCode.OK, """{"erro": true}""");

        var ex = await Assert.ThrowsAsync<NotFoundException>(() => service.ObterPorCepAsync("00000000"));
        Assert.Equal("CEP_NAO_ENCONTRADO", ex.Code);
    }

    [Fact]
    public async Task ViaCepResponde400_LancaNotFound()
    {
        var (service, _) = Criar(HttpStatusCode.BadRequest, "");

        await Assert.ThrowsAsync<NotFoundException>(() => service.ObterPorCepAsync("12345678"));
    }

    [Fact]
    public async Task ViaCepForaDoAr_Responde500_LancaExternalServiceException()
    {
        var (service, _) = Criar(HttpStatusCode.InternalServerError, "");

        var ex = await Assert.ThrowsAsync<ExternalServiceException>(() => service.ObterPorCepAsync("01001000"));
        Assert.Equal("SERVICO_EXTERNO_INDISPONIVEL", ex.Code);
    }

    [Fact]
    public async Task RespostaInvalida_LancaExternalServiceException()
    {
        var (service, _) = Criar(HttpStatusCode.OK, "isto não é json");

        await Assert.ThrowsAsync<ExternalServiceException>(() => service.ObterPorCepAsync("01001000"));
    }
}
