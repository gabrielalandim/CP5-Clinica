namespace Clinica.Domain.Errors;

/// <summary>
/// Base de todos os erros customizados da aplicação.
/// Cada erro carrega um <see cref="Code"/> legível por máquina, usado para
/// mapear o status HTTP (UseCases e GlobalExceptionHandler).
/// </summary>
public abstract class DomainException : Exception
{
    public string Code { get; }

    protected DomainException(string code, string message, Exception? inner = null)
        : base(message, inner)
    {
        Code = code;
    }
}

/// <summary>Recurso não encontrado (lançada pelo Repository / serviço externo). → 404</summary>
public class NotFoundException : DomainException
{
    public NotFoundException(string message, string code = "RECURSO_NAO_ENCONTRADO")
        : base(code, message) { }
}

/// <summary>Conflito de regra de negócio, ex.: e-mail duplicado (lançada pelo Repository). → 409</summary>
public class ConflictException : DomainException
{
    public ConflictException(string message, string code = "CONFLITO")
        : base(code, message) { }
}

/// <summary>Violação de regra de negócio/validação. → 400</summary>
public class BusinessRuleException : DomainException
{
    public BusinessRuleException(string message, string code = "REGRA_DE_NEGOCIO")
        : base(code, message) { }
}

/// <summary>Falha ao consumir serviço externo (lançada pela Infrastructure). → 503</summary>
public class ExternalServiceException : DomainException
{
    public ExternalServiceException(string message, Exception? inner = null)
        : base("SERVICO_EXTERNO_INDISPONIVEL", message, inner) { }
}
