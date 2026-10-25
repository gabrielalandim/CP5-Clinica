using Clinica.Domain.Entities;
using Clinica.Domain.Errors;
using Clinica.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Clinica.Infrastructure.Data.Repositories;

/// <summary>Estilo RETORNO: devolve RepositoryResult com o erro, sem lançar exceção.</summary>
public class ConsultaRepository : IConsultaRepository
{
    private readonly ApplicationContext _context;

    public ConsultaRepository(ApplicationContext context) => _context = context;

    public async Task<(IReadOnlyList<ConsultaEntity> Items, int Total)> ObterTodosAsync(int page, int pageSize, int? pacienteId)
    {
        var query = _context.Consultas.AsNoTracking();

        if (pacienteId.HasValue)
            query = query.Where(c => c.PacienteId == pacienteId.Value);

        var total = await query.CountAsync();

        var items = await query
            .OrderBy(c => c.DataHora)
            .ThenBy(c => c.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, total);
    }

    public async Task<RepositoryResult<ConsultaEntity>> ObterUmAsync(int id)
    {
        var consulta = await _context.Consultas.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id);

        return consulta is null
            ? RepositoryResult<ConsultaEntity>.Fail(RepositoryErrorCodes.ConsultaNaoEncontrada, $"Consulta {id} não encontrada.")
            : RepositoryResult<ConsultaEntity>.Success(consulta);
    }

    public async Task<RepositoryResult<ConsultaEntity>> AgendarAsync(ConsultaEntity entity)
    {
        if (!await _context.Pacientes.ExisteAsync(p => p.Id == entity.PacienteId))
            return RepositoryResult<ConsultaEntity>.Fail(
                RepositoryErrorCodes.PacienteNaoEncontrado, $"Paciente {entity.PacienteId} não encontrado.");

        var ocupado = await _context.Consultas.ExisteAsync(c =>
            c.DataHora == entity.DataHora &&
            c.Especialidade == entity.Especialidade &&
            c.Status == StatusConsulta.Agendada);

        if (ocupado)
            return RepositoryResult<ConsultaEntity>.Fail(
                RepositoryErrorCodes.HorarioIndisponivel,
                $"Já existe consulta de {entity.Especialidade} agendada para {entity.DataHora:dd/MM/yyyy HH:mm}.");

        _context.Consultas.Add(entity);
        await _context.SaveChangesAsync();

        return RepositoryResult<ConsultaEntity>.Success(entity);
    }

    public async Task<RepositoryResult<ConsultaEntity>> CancelarAsync(int id)
    {
        var consulta = await _context.Consultas.FirstOrDefaultAsync(c => c.Id == id);

        if (consulta is null)
            return RepositoryResult<ConsultaEntity>.Fail(RepositoryErrorCodes.ConsultaNaoEncontrada, $"Consulta {id} não encontrada.");

        if (consulta.Status == StatusConsulta.Cancelada)
            return RepositoryResult<ConsultaEntity>.Fail(RepositoryErrorCodes.ConsultaJaCancelada, "A consulta já está cancelada.");

        consulta.Status = StatusConsulta.Cancelada;
        await _context.SaveChangesAsync();

        return RepositoryResult<ConsultaEntity>.Success(consulta);
    }
}
