using Clinica.Domain.Entities;
using Clinica.Domain.Errors;
using Clinica.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Clinica.Infrastructure.Data.Repositories;

public class UserRepository : IUserRepository
{
    private readonly ApplicationContext _context;

    public UserRepository(ApplicationContext context) => _context = context;

    public Task<UserEntity?> ObterPorEmailAsync(string email) =>
        _context.Usuarios.AsNoTracking().FirstOrDefaultAsync(u => u.Email == email);

    public async Task<RepositoryResult<UserEntity>> AdicionarAsync(UserEntity entity)
    {
        if (await _context.Usuarios.ExisteAsync(u => u.Email == entity.Email))
            return RepositoryResult<UserEntity>.Fail(
                RepositoryErrorCodes.EmailJaCadastrado, $"Já existe um usuário com o e-mail '{entity.Email}'.");

        _context.Usuarios.Add(entity);
        await _context.SaveChangesAsync();

        return RepositoryResult<UserEntity>.Success(entity);
    }
}
