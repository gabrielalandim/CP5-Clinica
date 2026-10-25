using Clinica.Domain.Entities;
using Clinica.Domain.Errors;

namespace Clinica.Domain.Interfaces;

/// <summary>
/// Estilo "RETORNO": os métodos devolvem RepositoryResult com o erro, sem lançar exceção.
/// </summary>
public interface IConsultaRepository
{
    Task<(IReadOnlyList<ConsultaEntity> Items, int Total)> ObterTodosAsync(int page, int pageSize, int? pacienteId);
    Task<RepositoryResult<ConsultaEntity>> ObterUmAsync(int id);
    Task<RepositoryResult<ConsultaEntity>> AgendarAsync(ConsultaEntity entity);
    Task<RepositoryResult<ConsultaEntity>> CancelarAsync(int id);
}
