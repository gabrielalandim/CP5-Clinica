using Clinica.Application.Common;
using Clinica.Domain.Errors;

namespace Clinica.Tests.Unit;

/// <summary>Garante que cada erro customizado (lançado ou devolvido) vira o status HTTP certo.</summary>
public class ErrorMapperTests
{
    public static IEnumerable<object[]> ExceptionsLancadas() => new[]
    {
        new object[] { new NotFoundException("x"), 404 },
        new object[] { new ConflictException("x"), 409 },
        new object[] { new BusinessRuleException("x"), 400 },
        new object[] { new ExternalServiceException("x"), 503 },
        new object[] { new InvalidOperationException("x"), 500 }
    };

    [Theory]
    [MemberData(nameof(ExceptionsLancadas))]
    public void StatusFor_Exception(Exception ex, int esperado) =>
        Assert.Equal(esperado, ErrorMapper.StatusFor(ex));

    [Theory]
    [InlineData(RepositoryErrorCodes.PacienteNaoEncontrado, 404)]
    [InlineData(RepositoryErrorCodes.ConsultaNaoEncontrada, 404)]
    [InlineData(RepositoryErrorCodes.HorarioIndisponivel, 409)]
    [InlineData(RepositoryErrorCodes.ConsultaJaCancelada, 409)]
    [InlineData(RepositoryErrorCodes.EmailJaCadastrado, 409)]
    [InlineData("QUALQUER_OUTRO", 400)]
    public void StatusFor_ErroDevolvidoPeloRepository(string codigo, int esperado) =>
        Assert.Equal(esperado, ErrorMapper.StatusFor(new RepositoryError(codigo, "msg")));

    [Fact]
    public void ToFailure_PreservaMensagemECodigo()
    {
        var result = ErrorMapper.ToFailure<int>(new ConflictException("e-mail duplicado", "EMAIL_JA_CADASTRADO"));

        Assert.False(result.IsSuccess);
        Assert.Equal(409, result.StatusCode);
        Assert.Equal("e-mail duplicado", result.Error);
        Assert.Equal("EMAIL_JA_CADASTRADO", result.ErrorCode);
    }

    [Fact]
    public void RepositoryResult_SucessoEFalha()
    {
        var ok = RepositoryResult<string>.Success("dado");
        var falha = RepositoryResult<string>.Fail(RepositoryErrorCodes.HorarioIndisponivel, "ocupado");

        Assert.True(ok.Ok);
        Assert.Equal("dado", ok.Data);
        Assert.False(falha.Ok);
        Assert.Equal(RepositoryErrorCodes.HorarioIndisponivel, falha.Error!.Code);
    }
}
