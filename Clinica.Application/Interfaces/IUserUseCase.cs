using Clinica.Application.Common;
using Clinica.Application.Dtos;

namespace Clinica.Application.Interfaces;

public interface IUserUseCase
{
    Task<OperationResult<GetUserDto>> RegistrarAsync(PostUserDto dto);
    Task<OperationResult<GetAuthDto>> AutenticarAsync(PostAuthDto dto);
}
