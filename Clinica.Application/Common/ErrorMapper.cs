using Clinica.Domain.Errors;
using System.Net;

namespace Clinica.Application.Common;

/// <summary>
/// Ponto único que traduz erros customizados (lançados ou devolvidos pelo Repository)
/// em status HTTP. Usado pelos UseCases e pelo GlobalExceptionHandler da API.
/// </summary>
public static class ErrorMapper
{
    /// <summary>Status para erros LANÇADOS (exceptions customizadas).</summary>
    public static int StatusFor(Exception ex) => ex switch
    {
        NotFoundException => (int)HttpStatusCode.NotFound,
        ConflictException => (int)HttpStatusCode.Conflict,
        BusinessRuleException => (int)HttpStatusCode.BadRequest,
        ExternalServiceException => (int)HttpStatusCode.ServiceUnavailable,
        _ => (int)HttpStatusCode.InternalServerError
    };

    /// <summary>Status para erros DEVOLVIDOS pelo Repository (RepositoryError.Code).</summary>
    public static int StatusFor(RepositoryError error) => error.Code switch
    {
        RepositoryErrorCodes.PacienteNaoEncontrado => (int)HttpStatusCode.NotFound,
        RepositoryErrorCodes.ConsultaNaoEncontrada => (int)HttpStatusCode.NotFound,
        RepositoryErrorCodes.HorarioIndisponivel => (int)HttpStatusCode.Conflict,
        RepositoryErrorCodes.ConsultaJaCancelada => (int)HttpStatusCode.Conflict,
        RepositoryErrorCodes.EmailJaCadastrado => (int)HttpStatusCode.Conflict,
        _ => (int)HttpStatusCode.BadRequest
    };

    public static OperationResult<T> ToFailure<T>(DomainException ex) =>
        OperationResult<T>.Failure(ex.Message, StatusFor(ex), ex.Code);

    public static OperationResult<T> ToFailure<T>(RepositoryError error) =>
        OperationResult<T>.Failure(error.Message, StatusFor(error), error.Code);
}
