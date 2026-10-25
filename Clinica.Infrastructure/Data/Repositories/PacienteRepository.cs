using Clinica.Domain.Entities;
using Clinica.Domain.Errors;
using Clinica.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Clinica.Infrastructure.Data.Repositories;

/// <summary>
/// Estilo LANÇAMENTO de erro: quando algo dá errado o Repository lança
/// NotFoundException / ConflictException (Domain/Errors) e o UseCase converte em Operation Result.
/// </summary>
public class PacienteRepository : IPacienteRepository
{
    private readonly ApplicationContext _context;

    public PacienteRepository(ApplicationContext context) => _context = context;

    public async Task<(IReadOnlyList<PacienteEntity> Items, int Total)> ObterTodosAsync(int page, int pageSize)
    {
        var query = _context.Pacientes.AsNoTracking();

        var total = await query.CountAsync();

        var items = await query
            .OrderBy(p => p.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, total);
    }

    public async Task<PacienteEntity> ObterUmAsync(int id)
    {
        var paciente = await _context.Pacientes.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id);

        return paciente ?? throw new NotFoundException($"Paciente {id} não encontrado.", RepositoryErrorCodes.PacienteNaoEncontrado);
    }

    public async Task<PacienteEntity> AdicionarAsync(PacienteEntity entity)
    {
        if (await _context.Pacientes.ExisteAsync(p => p.Email == entity.Email))
            throw new ConflictException($"Já existe um paciente com o e-mail '{entity.Email}'.", RepositoryErrorCodes.EmailJaCadastrado);

        _context.Pacientes.Add(entity);
        await SalvarAsync(entity.Email);

        return entity;
    }

    public async Task<PacienteEntity> AtualizarAsync(int id, PacienteEntity entity)
    {
        var paciente = await _context.Pacientes.FirstOrDefaultAsync(p => p.Id == id)
            ?? throw new NotFoundException($"Paciente {id} não encontrado.", RepositoryErrorCodes.PacienteNaoEncontrado);

        if (await _context.Pacientes.ExisteAsync(p => p.Email == entity.Email && p.Id != id))
            throw new ConflictException($"Já existe um paciente com o e-mail '{entity.Email}'.", RepositoryErrorCodes.EmailJaCadastrado);

        paciente.Nome = entity.Nome;
        paciente.Email = entity.Email;

        await SalvarAsync(entity.Email);

        return paciente;
    }

    public async Task DeletarAsync(int id)
    {
        var paciente = await _context.Pacientes.FirstOrDefaultAsync(p => p.Id == id)
            ?? throw new NotFoundException($"Paciente {id} não encontrado.", RepositoryErrorCodes.PacienteNaoEncontrado);

        _context.Pacientes.Remove(paciente);
        await _context.SaveChangesAsync();
    }

    /// <summary>
    /// Salva e converte violação do índice único de e-mail (cadastros simultâneos)
    /// na exception customizada ConflictException.
    /// </summary>
    private async Task SalvarAsync(string email)
    {
        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("ORA-00001") == true)
        {
            throw new ConflictException($"Já existe um paciente com o e-mail '{email}'.", RepositoryErrorCodes.EmailJaCadastrado);
        }
    }
}
