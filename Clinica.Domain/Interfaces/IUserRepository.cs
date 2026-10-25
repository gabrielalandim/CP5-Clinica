using Clinica.Domain.Entities;
using Clinica.Domain.Errors;

namespace Clinica.Domain.Interfaces;

public interface IUserRepository
{
    Task<UserEntity?> ObterPorEmailAsync(string email);
    Task<RepositoryResult<UserEntity>> AdicionarAsync(UserEntity entity);
}
