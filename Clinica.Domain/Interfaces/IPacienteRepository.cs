using Clinica.Domain.Entities;

namespace Clinica.Domain.Interfaces;

/// <summary>
/// Estilo "LANÇAMENTO": os métodos lançam NotFoundException / ConflictException.
/// </summary>
public interface IPacienteRepository
{
    Task<(IReadOnlyList<PacienteEntity> Items, int Total)> ObterTodosAsync(int page, int pageSize);
    Task<PacienteEntity> ObterUmAsync(int id);                        // lança NotFoundException
    Task<PacienteEntity> AdicionarAsync(PacienteEntity entity);       // lança ConflictException
    Task<PacienteEntity> AtualizarAsync(int id, PacienteEntity entity); // lança NotFound/Conflict
    Task DeletarAsync(int id);                                        // lança NotFoundException
}
