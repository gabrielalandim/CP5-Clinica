using Clinica.Domain.Errors;
using Clinica.Domain.Interfaces;
using Clinica.Domain.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Clinica.Tests.Support;

/// <summary>
/// Sobe a API completa usando o MESMO banco da aplicação (Oracle da connection string configurada).
/// Apenas o serviço externo (ViaCEP) é substituído por um fake, para o teste não depender da internet.
/// </summary>
public class ApiFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IEnderecoService>();
            services.AddSingleton<IEnderecoService, FakeEnderecoService>();
        });
    }
}

/// <summary>
/// Uma única instância da API (e uma única inicialização do banco) compartilhada por todas as
/// classes de integração, que rodam em sequência para não disputarem os mesmos registros.
/// </summary>
[CollectionDefinition(Name)]
public class ApiCollection : ICollectionFixture<ApiFactory>
{
    public const string Name = "Api";
}

public class FakeEnderecoService : IEnderecoService
{
    public const string CepInexistente = "00000000";
    public const string CepServicoForaDoAr = "99999999";

    public Task<EnderecoModel> ObterPorCepAsync(string cep) => cep switch
    {
        CepInexistente => throw new NotFoundException($"CEP não encontrado: {cep}."),
        CepServicoForaDoAr => throw new ExternalServiceException("O serviço de consulta de CEP está indisponível."),
        _ => Task.FromResult(new EnderecoModel(cep, "Praça da Sé", "São Paulo", "SP"))
    };
}
