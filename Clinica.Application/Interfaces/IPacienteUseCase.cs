using Clinica.Application.Common;
using Clinica.Application.Dtos;

namespace Clinica.Application.Interfaces;

public interface IPacienteUseCase
{
    Task<OperationResult<PageResult<GetPacienteDto>>> ObterTodosAsync(int page, int pageSize);
    Task<OperationResult<GetPacienteDto>> ObterUmAsync(int id);
    Task<OperationResult<GetPacienteDto>> AdicionarAsync(PostPacienteDto dto);
    Task<OperationResult<GetPacienteDto>> AtualizarAsync(int id, PutPacienteDto dto);
    Task<OperationResult<bool>> DeletarAsync(int id);
}
