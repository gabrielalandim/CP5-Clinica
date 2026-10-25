namespace Clinica.Domain.Errors;

/// <summary>Erro devolvido (e não lançado) pelo Repository.</summary>
public record RepositoryError(string Code, string Message);

/// <summary>
/// Resultado de uma operação de Repository no estilo "RETORNO de erro":
/// o Repository não lança exceção, devolve o erro (código + mensagem) para o UseCase decidir.
/// </summary>
public class RepositoryResult<T>
{
    public T? Data { get; private set; }
    public RepositoryError? Error { get; private set; }
    public bool Ok => Error is null;

    public static RepositoryResult<T> Success(T data) => new() { Data = data };

    public static RepositoryResult<T> Fail(string code, string message) =>
        new() { Error = new RepositoryError(code, message) };
}

public static class RepositoryErrorCodes
{
    public const string PacienteNaoEncontrado = "PACIENTE_NAO_ENCONTRADO";
    public const string ConsultaNaoEncontrada = "CONSULTA_NAO_ENCONTRADA";
    public const string HorarioIndisponivel = "HORARIO_INDISPONIVEL";
    public const string ConsultaJaCancelada = "CONSULTA_JA_CANCELADA";
    public const string EmailJaCadastrado = "EMAIL_JA_CADASTRADO";
}
