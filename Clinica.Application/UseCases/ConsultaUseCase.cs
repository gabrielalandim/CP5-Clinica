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

public class ConsultaUseCase : IConsultaUseCase
{
    private const string Base = "/api/consultas";

    private readonly IConsultaRepository _repository;
    private readonly ILogger<ConsultaUseCase> _logger;

    public ConsultaUseCase(IConsultaRepository repository, ILogger<ConsultaUseCase> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<OperationResult<PageResult<GetConsultaDto>>> ObterTodosAsync(int page, int pageSize, int? pacienteId)
    {
        try
        {
            (page, pageSize) = Hateoas.Normalizar(page, pageSize);

            var (items, total) = await _repository.ObterTodosAsync(page, pageSize, pacienteId);

            var result = PageResult<GetConsultaDto>.Create(
                items.Select(c => c.ToDto(LinksConsulta(c))).ToList(), page, pageSize, total);

            var extra = pacienteId.HasValue ? $"&pacienteId={pacienteId}" : string.Empty;
            result.Links = Hateoas.ForPage(Base, page, pageSize, result.TotalPages, "agendar_consulta", extra);

            return OperationResult<PageResult<GetConsultaDto>>.Success(result, (int)HttpStatusCode.OK);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao listar consultas");
            return OperationResult<PageResult<GetConsultaDto>>.Failure("Ocorreu um erro ao listar as consultas.", (int)HttpStatusCode.InternalServerError);
        }
    }

    public async Task<OperationResult<GetConsultaDto>> ObterUmAsync(int id)
    {
        try
        {
            var r = await _repository.ObterUmAsync(id);
            return r.Ok
                ? OperationResult<GetConsultaDto>.Success(r.Data!.ToDto(LinksConsulta(r.Data!)), (int)HttpStatusCode.OK)
                : Falha(r.Error!);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao obter consulta {Id}", id);
            return OperationResult<GetConsultaDto>.Failure("Ocorreu um erro ao obter a consulta.", (int)HttpStatusCode.InternalServerError);
        }
    }

    public async Task<OperationResult<GetConsultaDto>> AgendarAsync(PostConsultaDto dto)
    {
        if (dto.DataHora <= DateTime.Now)
            return OperationResult<GetConsultaDto>.Failure("A data da consulta deve ser futura.", (int)HttpStatusCode.BadRequest, "DATA_INVALIDA");

        try
        {
            var entity = new ConsultaEntity
            {
                PacienteId = dto.PacienteId,
                DataHora = dto.DataHora,
                Especialidade = dto.Especialidade.Trim(),
                Valor = dto.Valor,
                Status = StatusConsulta.Agendada
            };

            // O Repository devolve o erro (não lança)
            var r = await _repository.AgendarAsync(entity);

            return r.Ok
                ? OperationResult<GetConsultaDto>.Success(r.Data!.ToDto(LinksConsulta(r.Data!)), (int)HttpStatusCode.Created)
                : Falha(r.Error!);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao agendar consulta");
            return OperationResult<GetConsultaDto>.Failure("Ocorreu um erro ao agendar a consulta.", (int)HttpStatusCode.InternalServerError);
        }
    }

    public async Task<OperationResult<GetConsultaDto>> CancelarAsync(int id)
    {
        try
        {
            var r = await _repository.CancelarAsync(id);
            return r.Ok
                ? OperationResult<GetConsultaDto>.Success(r.Data!.ToDto(LinksConsulta(r.Data!)), (int)HttpStatusCode.OK)
                : Falha(r.Error!);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao cancelar consulta {Id}", id);
            return OperationResult<GetConsultaDto>.Failure("Ocorreu um erro ao cancelar a consulta.", (int)HttpStatusCode.InternalServerError);
        }
    }

    /// <summary>Converte o erro DEVOLVIDO pelo Repository (RepositoryError) em Operation Result.</summary>
    private static OperationResult<GetConsultaDto> Falha(RepositoryError error) =>
        ErrorMapper.ToFailure<GetConsultaDto>(error);

    private static List<LinkDto> LinksConsulta(ConsultaEntity c)
    {
        var links = new List<LinkDto>
        {
            new($"{Base}/{c.Id}", "self", "GET"),
            new($"/api/pacientes/{c.PacienteId}", "paciente", "GET")
        };

        if (c.Status == StatusConsulta.Agendada)
            links.Add(new($"{Base}/{c.Id}", "cancelar_consulta", "DELETE"));

        return links;
    }
}
