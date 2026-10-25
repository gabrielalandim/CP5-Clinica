using Clinica.Application.Common;
using Clinica.Application.Dtos;

namespace Clinica.Application.Interfaces;

public interface IConsultaUseCase
{
    Task<OperationResult<PageResult<GetConsultaDto>>> ObterTodosAsync(int page, int pageSize, int? pacienteId);
    Task<OperationResult<GetConsultaDto>> ObterUmAsync(int id);
    Task<OperationResult<GetConsultaDto>> AgendarAsync(PostConsultaDto dto);
    Task<OperationResult<GetConsultaDto>> CancelarAsync(int id);
}
