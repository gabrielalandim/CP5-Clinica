using Clinica.Application.UseCases;
using Clinica.Application.Dtos;
using Clinica.Domain.Entities;
using Clinica.Domain.Errors;
using Clinica.Domain.Interfaces;
using Clinica.Domain.Models;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Clinica.Tests.Unit;

public class PacienteUseCaseTests
{
    private readonly Mock<IPacienteRepository> _repo = new();
    private readonly Mock<IEnderecoService> _enderecos = new();
    private readonly PacienteUseCase _useCase;

    public PacienteUseCaseTests()
    {
        _useCase = new PacienteUseCase(_repo.Object, _enderecos.Object, NullLogger<PacienteUseCase>.Instance);
    }

    private static PostPacienteDto NovoDto() =>
        new() { Nome = "Ana Souza", Email = "ana@teste.com", Cep = "01001-000" };

    [Fact]
    public async Task ObterUm_Existente_Retorna200ComLinks()
    {
        _repo.Setup(r => r.ObterUmAsync(1)).ReturnsAsync(new PacienteEntity { Id = 1, Nome = "Ana", Email = "ana@teste.com" });

        var result = await _useCase.ObterUmAsync(1);

        Assert.True(result.IsSuccess);
        Assert.Equal(200, result.StatusCode);
        Assert.Contains(result.Value!.Links, l => l.Rel == "self" && l.Method == "GET");
        Assert.Contains(result.Value.Links, l => l.Rel == "delete_paciente" && l.Method == "DELETE");
    }

    [Fact]
    public async Task ObterUm_RepositorioLancaNotFound_Retorna404()
    {
        _repo.Setup(r => r.ObterUmAsync(99)).ThrowsAsync(new NotFoundException("Paciente 99 não encontrado."));

        var result = await _useCase.ObterUmAsync(99);

        Assert.False(result.IsSuccess);
        Assert.Equal(404, result.StatusCode);
        Assert.Equal("Paciente 99 não encontrado.", result.Error);
    }

    [Fact]
    public async Task Adicionar_CepValido_Retorna201ComEndereco()
    {
        _enderecos.Setup(s => s.ObterPorCepAsync("01001000"))
                  .ReturnsAsync(new EnderecoModel("01001000", "Praça da Sé", "São Paulo", "SP"));
        _repo.Setup(r => r.AdicionarAsync(It.IsAny<PacienteEntity>()))
             .ReturnsAsync((PacienteEntity e) => { e.Id = 7; return e; });

        var result = await _useCase.AdicionarAsync(NovoDto());

        Assert.True(result.IsSuccess);
        Assert.Equal(201, result.StatusCode);
        Assert.Equal(7, result.Value!.Id);
        Assert.Equal("São Paulo", result.Value.Cidade);
        Assert.Equal("SP", result.Value.Uf);
    }

    [Fact]
    public async Task Adicionar_CepComTamanhoInvalido_Retorna400_SemChamarServicoExterno()
    {
        var dto = NovoDto();
        dto.Cep = "123";

        var result = await _useCase.AdicionarAsync(dto);

        Assert.Equal(400, result.StatusCode);
        _enderecos.Verify(s => s.ObterPorCepAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Adicionar_CepInexistente_Retorna404()
    {
        _enderecos.Setup(s => s.ObterPorCepAsync(It.IsAny<string>())).ThrowsAsync(new NotFoundException("CEP não encontrado."));

        var result = await _useCase.AdicionarAsync(NovoDto());

        Assert.Equal(404, result.StatusCode);
    }

    [Fact]
    public async Task Adicionar_ServicoExternoForaDoAr_Retorna503()
    {
        _enderecos.Setup(s => s.ObterPorCepAsync(It.IsAny<string>())).ThrowsAsync(new ExternalServiceException("indisponível"));

        var result = await _useCase.AdicionarAsync(NovoDto());

        Assert.False(result.IsSuccess);
        Assert.Equal(503, result.StatusCode);
    }

    [Fact]
    public async Task Adicionar_EmailDuplicado_Retorna409()
    {
        _enderecos.Setup(s => s.ObterPorCepAsync(It.IsAny<string>()))
                  .ReturnsAsync(new EnderecoModel("01001000", "Praça da Sé", "São Paulo", "SP"));
        _repo.Setup(r => r.AdicionarAsync(It.IsAny<PacienteEntity>())).ThrowsAsync(new ConflictException("e-mail duplicado"));

        var result = await _useCase.AdicionarAsync(NovoDto());

        Assert.Equal(409, result.StatusCode);
    }

    [Fact]
    public async Task ObterTodos_MontaPageResultComLinksNextEPrev()
    {
        var itens = Enumerable.Range(11, 10).Select(i => new PacienteEntity { Id = i, Nome = $"P{i}", Email = $"p{i}@t.com" }).ToList();
        _repo.Setup(r => r.ObterTodosAsync(2, 10)).ReturnsAsync(((IReadOnlyList<PacienteEntity>)itens, 25));

        var result = await _useCase.ObterTodosAsync(2, 10);

        Assert.True(result.IsSuccess);
        var page = result.Value!;
        Assert.Equal(25, page.TotalItems);
        Assert.Equal(3, page.TotalPages);
        Assert.True(page.HasNext);
        Assert.True(page.HasPrevious);
        Assert.Equal(10, page.Items.Count);
        Assert.Contains(page.Links, l => l.Rel == "next");
        Assert.Contains(page.Links, l => l.Rel == "prev");
        Assert.Contains(page.Links, l => l.Rel == "create_paciente" && l.Method == "POST");
    }

    [Fact]
    public async Task Deletar_Existente_Retorna204()
    {
        _repo.Setup(r => r.DeletarAsync(1)).Returns(Task.CompletedTask);

        var result = await _useCase.DeletarAsync(1);

        Assert.True(result.IsSuccess);
        Assert.Equal(204, result.StatusCode);
    }
}
