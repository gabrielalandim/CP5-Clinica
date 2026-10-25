using Clinica.Application.Common;
using Clinica.Application.Dtos;
using Clinica.Application.Interfaces;
using Clinica.Application.Mappers;
using Clinica.Domain.Entities;
using Clinica.Domain.Interfaces;
using Clinica.Domain.Security;
using Microsoft.Extensions.Logging;
using System.Net;

namespace Clinica.Application.UseCases;

public class UserUseCase : IUserUseCase
{
    private readonly IUserRepository _repository;
    private readonly ITokenService _tokenService;
    private readonly ILogger<UserUseCase> _logger;

    public UserUseCase(IUserRepository repository, ITokenService tokenService, ILogger<UserUseCase> logger)
    {
        _repository = repository;
        _tokenService = tokenService;
        _logger = logger;
    }

    public async Task<OperationResult<GetUserDto>> RegistrarAsync(PostUserDto dto)
    {
        try
        {
            var entity = new UserEntity
            {
                Nome = dto.Nome.Trim(),
                Email = dto.Email.Trim().ToLowerInvariant(),
                PasswordHash = PasswordHasher.Hash(dto.Password),
                Role = "user" // cadastro público sempre cria perfil comum
            };

            // Repository no estilo RETORNO: devolve o erro em vez de lançar
            var r = await _repository.AdicionarAsync(entity);

            return r.Ok
                ? OperationResult<GetUserDto>.Success(r.Data!.ToDto(), (int)HttpStatusCode.Created)
                : ErrorMapper.ToFailure<GetUserDto>(r.Error!);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao registrar usuário");
            return OperationResult<GetUserDto>.Failure("Ocorreu um erro ao registrar o usuário.", (int)HttpStatusCode.InternalServerError);
        }
    }

    public async Task<OperationResult<GetAuthDto>> AutenticarAsync(PostAuthDto dto)
    {
        try
        {
            var user = await _repository.ObterPorEmailAsync(dto.Email.Trim().ToLowerInvariant());

            if (user is null || !PasswordHasher.Verify(dto.Senha, user.PasswordHash))
                return OperationResult<GetAuthDto>.Failure("E-mail ou senha inválidos.", (int)HttpStatusCode.Unauthorized, "CREDENCIAIS_INVALIDAS");

            // Geração do JWT fica na Infrastructure (ITokenService)
            var jwt = _tokenService.GerarToken(user);

            return OperationResult<GetAuthDto>.Success(
                new GetAuthDto { Email = user.Email, Role = user.Role, Jwt = jwt }, (int)HttpStatusCode.OK);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao autenticar usuário");
            return OperationResult<GetAuthDto>.Failure("Ocorreu um erro ao autenticar o usuário.", (int)HttpStatusCode.InternalServerError);
        }
    }
}
