using Clinica.API.Extensions;
using Clinica.Application.Common;
using Clinica.Domain.Errors;
using Microsoft.AspNetCore.Diagnostics;

namespace Clinica.API.Middlewares;

/// <summary>
/// Rede de segurança: qualquer exception que escape dos UseCases vira uma resposta JSON padronizada.
/// Erros customizados (DomainException) mantêm o status correto (404, 409, 400, 503);
/// os demais viram 500 sem expor detalhes internos.
/// </summary>
public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) => _logger = logger;

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var status = ErrorMapper.StatusFor(exception);

        ErrorResponse body;
        if (exception is DomainException domain)
        {
            _logger.LogWarning("Erro de domínio {Code}: {Message}", domain.Code, domain.Message);
            body = new ErrorResponse(status, domain.Code, domain.Message, httpContext.Request.Path);
        }
        else
        {
            _logger.LogError(exception, "Erro não tratado em {Path}", httpContext.Request.Path);
            body = new ErrorResponse(status, "ERRO_INTERNO", "Ocorreu um erro inesperado.", httpContext.Request.Path);
        }

        httpContext.Response.StatusCode = status;
        await httpContext.Response.WriteAsJsonAsync(body, cancellationToken);
        return true;
    }
}
