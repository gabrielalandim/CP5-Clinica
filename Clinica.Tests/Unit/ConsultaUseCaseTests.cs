using Clinica.Application.Dtos;
using Clinica.Application.UseCases;
using Clinica.Domain.Entities;
using Clinica.Domain.Errors;
using Clinica.Domain.Interfaces;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Clinica.Tests.Unit;

public class ConsultaUseCaseTests
{
    private readonly Mock<IConsultaRepository> _repo = new();
    private readonly ConsultaUseCase _useCase;

    public ConsultaUseCaseTests()
    {
        _useCase = new ConsultaUseCase(_repo.Object, NullLogger<ConsultaUseCase>.Instance);
    }

    private static PostConsultaDto NovoDto() => new()
    {
        PacienteId = 1,
        DataHora = DateTime.Now.AddDays(5),
        Especialidade = "Cardiologia",
        Valor = 250m
    };

    [Fact]
    public async Task Agendar_Sucesso_Retorna201ComLinkDeCancelamento()
    {
        _repo.Setup(r => r.AgendarAsync(It.IsAny<ConsultaEntity>()))
             .ReturnsAsync((ConsultaEntity e) => { e.Id = 3; return RepositoryResult<ConsultaEntity>.Success(e); });

        var result = await _useCase.AgendarAsync(NovoDto());

        Assert.True(result.IsSuccess);
        Assert.Equal(201, result.StatusCode);
        Assert.Contains(result.Value!.Links, l => l.Rel == "cancelar_consulta" && l.Method == "DELETE");
    }

    [Fact]
    public async Task Agendar_DataNoPassado_Retorna400_SemChamarRepositorio()
    {
        var dto = NovoDto();
        dto.DataHora = DateTime.Now.AddDays(-1);

        var result = await _useCase.AgendarAsync(dto);

        Assert.Equal(400, result.StatusCode);
        _repo.Verify(r => r.AgendarAsync(It.IsAny<ConsultaEntity>()), Times.Never);
    }

    [Theory]
    [InlineData(RepositoryErrorCodes.PacienteNaoEncontrado, 404)]
    [InlineData(RepositoryErrorCodes.HorarioIndisponivel, 409)]
    [InlineData("OUTRO_ERRO", 400)]
    public async Task Agendar_ErroDevolvidoPeloRepositorio_ViraStatusHttpCorreto(string codigo, int statusEsperado)
    {
        _repo.Setup(r => r.AgendarAsync(It.IsAny<ConsultaEntity>()))
             .ReturnsAsync(RepositoryResult<ConsultaEntity>.Fail(codigo, "mensagem do repositório"));

        var result = await _useCase.AgendarAsync(NovoDto());

        Assert.False(result.IsSuccess);
        Assert.Equal(statusEsperado, result.StatusCode);
        Assert.Equal("mensagem do repositório", result.Error);
    }

    [Fact]
    public async Task Cancelar_JaCancelada_Retorna409()
    {
        _repo.Setup(r => r.CancelarAsync(5))
             .ReturnsAsync(RepositoryResult<ConsultaEntity>.Fail(RepositoryErrorCodes.ConsultaJaCancelada, "já cancelada"));

        var result = await _useCase.CancelarAsync(5);

        Assert.Equal(409, result.StatusCode);
    }

    [Fact]
    public async Task ObterUm_Inexistente_Retorna404()
    {
        _repo.Setup(r => r.ObterUmAsync(42))
             .ReturnsAsync(RepositoryResult<ConsultaEntity>.Fail(RepositoryErrorCodes.ConsultaNaoEncontrada, "não encontrada"));

        var result = await _useCase.ObterUmAsync(42);

        Assert.Equal(404, result.StatusCode);
    }
}
