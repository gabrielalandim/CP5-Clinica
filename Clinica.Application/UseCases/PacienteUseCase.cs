using Clinica.Application.Common;
using Clinica.Application.Dtos;
using Clinica.Application.Interfaces;
using Clinica.Application.Mappers;
using Clinica.Domain.Entities;
using Clinica.Domain.Errors;
using Clinica.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using System.Net;

namespace Clinica.Application.UseCases;

public class PacienteUseCase : IPacienteUseCase
{
    private const string Base = "/api/pacientes";

    private readonly IPacienteRepository _repository;
    private readonly IEnderecoService _enderecoService;
    private readonly ILogger<PacienteUseCase> _logger;

    public PacienteUseCase(IPacienteRepository repository, IEnderecoService enderecoService, ILogger<PacienteUseCase> logger)
    {
        _repository = repository;
        _enderecoService = enderecoService;
        _logger = logger;
    }

    public async Task<OperationResult<PageResult<GetPacienteDto>>> ObterTodosAsync(int page, int pageSize)
    {
        try
        {
            (page, pageSize) = Hateoas.Normalizar(page, pageSize);

            var (items, total) = await _repository.ObterTodosAsync(page, pageSize);

            var result = PageResult<GetPacienteDto>.Create(
                items.Select(p => p.ToDto(LinksPaciente(p.Id))).ToList(), page, pageSize, total);

            result.Links = Hateoas.ForPage(Base, page, pageSize, result.TotalPages, "create_paciente");

            return OperationResult<PageResult<GetPacienteDto>>.Success(result, (int)HttpStatusCode.OK);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao listar pacientes");
            return OperationResult<PageResult<GetPacienteDto>>.Failure("Ocorreu um erro ao listar os pacientes.", (int)HttpStatusCode.InternalServerError);
        }
    }

    public async Task<OperationResult<GetPacienteDto>> ObterUmAsync(int id)
    {
        try
        {
            var paciente = await _repository.ObterUmAsync(id);
            return OperationResult<GetPacienteDto>.Success(paciente.ToDto(LinksPaciente(id)), (int)HttpStatusCode.OK);
        }
        catch (NotFoundException ex)
        {
            // Repository LANÇA NotFoundException → convertido em Operation Result 404
            _logger.LogWarning("Paciente {Id} não encontrado", id);
            return ErrorMapper.ToFailure<GetPacienteDto>(ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao obter paciente {Id}", id);
            return OperationResult<GetPacienteDto>.Failure("Ocorreu um erro ao obter o paciente.", (int)HttpStatusCode.InternalServerError);
        }
    }

    public async Task<OperationResult<GetPacienteDto>> AdicionarAsync(PostPacienteDto dto)
    {
        var cep = new string(dto.Cep.Where(char.IsDigit).ToArray());
        if (cep.Length != 8)
            return OperationResult<GetPacienteDto>.Failure("CEP inválido. Informe 8 dígitos.", (int)HttpStatusCode.BadRequest, "CEP_INVALIDO");

        try
        {
            // Chamada ao serviço externo (ViaCEP) na camada de Infrastructure
            var endereco = await _enderecoService.ObterPorCepAsync(cep);

            var entity = new PacienteEntity
            {
                Nome = dto.Nome.Trim(),
                Email = dto.Email.Trim().ToLowerInvariant(),
                Cep = endereco.Cep,
                Logradouro = endereco.Logradouro,
                Cidade = endereco.Cidade,
                Uf = endereco.Uf
            };

            var salvo = await _repository.AdicionarAsync(entity);

            return OperationResult<GetPacienteDto>.Success(salvo.ToDto(LinksPaciente(salvo.Id)), (int)HttpStatusCode.Created);
        }
        catch (ExternalServiceException ex)
        {
            _logger.LogError(ex, "Serviço externo de CEP indisponível");
            return ErrorMapper.ToFailure<GetPacienteDto>(ex); // 503
        }
        catch (DomainException ex)
        {
            // NotFoundException (CEP inexistente) → 404 | ConflictException (e-mail duplicado) → 409
            _logger.LogWarning("Falha de negócio ao adicionar paciente: {Code} - {Message}", ex.Code, ex.Message);
            return ErrorMapper.ToFailure<GetPacienteDto>(ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao adicionar paciente");
            return OperationResult<GetPacienteDto>.Failure("Ocorreu um erro ao salvar o paciente.", (int)HttpStatusCode.InternalServerError);
        }
    }

    public async Task<OperationResult<GetPacienteDto>> AtualizarAsync(int id, PutPacienteDto dto)
    {
        try
        {
            var entity = new PacienteEntity
            {
                Nome = dto.Nome.Trim(),
                Email = dto.Email.Trim().ToLowerInvariant()
            };

            var atualizado = await _repository.AtualizarAsync(id, entity);

            return OperationResult<GetPacienteDto>.Success(atualizado.ToDto(LinksPaciente(id)), (int)HttpStatusCode.OK);
        }
        catch (DomainException ex)
        {
            // Repository LANÇA NotFoundException / ConflictException → 404 / 409
            return ErrorMapper.ToFailure<GetPacienteDto>(ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao atualizar paciente {Id}", id);
            return OperationResult<GetPacienteDto>.Failure("Ocorreu um erro ao atualizar o paciente.", (int)HttpStatusCode.InternalServerError);
        }
    }

    public async Task<OperationResult<bool>> DeletarAsync(int id)
    {
        try
        {
            await _repository.DeletarAsync(id);
            return OperationResult<bool>.Success(true, (int)HttpStatusCode.NoContent);
        }
        catch (NotFoundException ex)
        {
            return ErrorMapper.ToFailure<bool>(ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao deletar paciente {Id}", id);
            return OperationResult<bool>.Failure("Ocorreu um erro ao deletar o paciente.", (int)HttpStatusCode.InternalServerError);
        }
    }

    private static List<LinkDto> LinksPaciente(int id) => new()
    {
        new($"{Base}/{id}", "self", "GET"),
        new($"{Base}/{id}", "update_paciente", "PUT"),
        new($"{Base}/{id}", "delete_paciente", "DELETE"),
        new($"/api/consultas?pacienteId={id}", "consultas_do_paciente", "GET")
    };
}
