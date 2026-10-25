using Clinica.Application.Interfaces;
using Clinica.Application.UseCases;
using Clinica.Domain.Interfaces;
using Clinica.Infrastructure.Data;
using Clinica.Infrastructure.Data.Repositories;
using Clinica.Infrastructure.Security;
using Clinica.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Clinica.IoC;

public static class Bootstrap
{
    public static void AddIoC(IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<ApplicationContext>(options =>
            options.UseOracle(configuration.GetConnectionString("Oracle")));

        // Repositories (Infrastructure) e UseCases (Application)
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IUserUseCase, UserUseCase>();

        services.AddScoped<IPacienteRepository, PacienteRepository>();
        services.AddScoped<IPacienteUseCase, PacienteUseCase>();

        services.AddScoped<IConsultaRepository, ConsultaRepository>();
        services.AddScoped<IConsultaUseCase, ConsultaUseCase>();

        // Segurança: geração do JWT
        services.AddSingleton<ITokenService, JwtTokenService>();

        // Otimização: cache em memória (usado pelo serviço de CEP)
        services.AddMemoryCache();

        // Serviço externo (ViaCEP) com HttpClient tipado + resiliência
        services.AddHttpClient<IEnderecoService, ViaCepService>(client =>
        {
            var url = configuration["ExternalServices:ViaCepUrl"] ?? "https://viacep.com.br/ws/";
            client.BaseAddress = new Uri(url);
        })
        .AddStandardResilienceHandler(options =>
        {
            // Retry com backoff exponencial para falhas transitórias (5xx, 408, timeout)
            options.Retry.MaxRetryAttempts = 3;
            options.Retry.Delay = TimeSpan.FromMilliseconds(300);

            // Timeout de cada tentativa e do pedido inteiro (com retries)
            options.AttemptTimeout.Timeout = TimeSpan.FromSeconds(5);
            options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(20);

            // Circuit breaker: abre após 50% de falhas (mín. 5 chamadas) em 30s e fica aberto 15s
            options.CircuitBreaker.FailureRatio = 0.5;
            options.CircuitBreaker.MinimumThroughput = 5;
            options.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(30);
            options.CircuitBreaker.BreakDuration = TimeSpan.FromSeconds(15);
        });
    }

    /// <summary>Cria as tabelas e os usuários iniciais (admin e user).</summary>
    public static async Task InitializeDatabaseAsync(IServiceProvider provider)
    {
        using var scope = provider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationContext>();
        await DbInitializer.InitializeAsync(db);
    }
}
