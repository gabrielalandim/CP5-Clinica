using Clinica.Application.Dtos;
using Clinica.Application.UseCases;
using Clinica.Domain.Entities;
using Clinica.Domain.Errors;
using Clinica.Domain.Interfaces;
using Clinica.Domain.Security;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Clinica.Tests.Unit;

public class UserUseCaseTests
{
    private readonly Mock<IUserRepository> _repo = new();
    private readonly Mock<ITokenService> _tokenService = new();
    private readonly UserUseCase _useCase;

    public UserUseCaseTests()
    {
        _tokenService.Setup(t => t.GerarToken(It.IsAny<UserEntity>())).Returns("jwt-de-teste");
        _useCase = new UserUseCase(_repo.Object, _tokenService.Object, NullLogger<UserUseCase>.Instance);
    }

    [Fact]
    public async Task Autenticar_CredenciaisValidas_RetornaJwtGeradoPeloTokenService()
    {
        _repo.Setup(r => r.ObterPorEmailAsync("admin@clinica.com")).ReturnsAsync(new UserEntity
        {
            Email = "admin@clinica.com",
            Role = "admin",
            PasswordHash = PasswordHasher.Hash("Admin@123")
        });

        var result = await _useCase.AutenticarAsync(new PostAuthDto { Email = "admin@clinica.com", Senha = "Admin@123" });

        Assert.True(result.IsSuccess);
        Assert.Equal(200, result.StatusCode);
        Assert.Equal("jwt-de-teste", result.Value!.Jwt);
        Assert.Equal("admin", result.Value.Role);
        _tokenService.Verify(t => t.GerarToken(It.Is<UserEntity>(u => u.Role == "admin")), Times.Once);
    }

    [Fact]
    public async Task Autenticar_SenhaErrada_Retorna401()
    {
        _repo.Setup(r => r.ObterPorEmailAsync("user@clinica.com")).ReturnsAsync(new UserEntity
        {
            Email = "user@clinica.com",
            Role = "user",
            PasswordHash = PasswordHasher.Hash("User@123")
        });

        var result = await _useCase.AutenticarAsync(new PostAuthDto { Email = "user@clinica.com", Senha = "errada" });

        Assert.Equal(401, result.StatusCode);
        Assert.Equal("CREDENCIAIS_INVALIDAS", result.ErrorCode);
        _tokenService.Verify(t => t.GerarToken(It.IsAny<UserEntity>()), Times.Never);
    }

    [Fact]
    public async Task Autenticar_UsuarioInexistente_Retorna401()
    {
        _repo.Setup(r => r.ObterPorEmailAsync(It.IsAny<string>())).ReturnsAsync((UserEntity?)null);

        var result = await _useCase.AutenticarAsync(new PostAuthDto { Email = "x@x.com", Senha = "123456" });

        Assert.Equal(401, result.StatusCode);
    }

    [Fact]
    public async Task Registrar_EmailJaCadastrado_Retorna409()
    {
        _repo.Setup(r => r.AdicionarAsync(It.IsAny<UserEntity>()))
             .ReturnsAsync(RepositoryResult<UserEntity>.Fail(RepositoryErrorCodes.EmailJaCadastrado, "duplicado"));

        var result = await _useCase.RegistrarAsync(new PostUserDto
        {
            Nome = "Teste", Email = "t@t.com", Password = "123456", ConfirmPassword = "123456"
        });

        Assert.Equal(409, result.StatusCode);
        Assert.Equal(RepositoryErrorCodes.EmailJaCadastrado, result.ErrorCode);
    }

    [Fact]
    public async Task Registrar_Sucesso_SempreCriaPerfilUser()
    {
        UserEntity? salvo = null;
        _repo.Setup(r => r.AdicionarAsync(It.IsAny<UserEntity>()))
             .ReturnsAsync((UserEntity e) => { salvo = e; return RepositoryResult<UserEntity>.Success(e); });

        var result = await _useCase.RegistrarAsync(new PostUserDto
        {
            Nome = "Teste", Email = "t@t.com", Password = "123456", ConfirmPassword = "123456"
        });

        Assert.Equal(201, result.StatusCode);
        Assert.Equal("user", salvo!.Role);
        Assert.NotEqual("123456", salvo.PasswordHash); // senha nunca em texto puro
    }

    [Fact]
    public void PasswordHasher_VerificaSenhaCorretaEIncorreta()
    {
        var hash = PasswordHasher.Hash("Segredo@1");

        Assert.True(PasswordHasher.Verify("Segredo@1", hash));
        Assert.False(PasswordHasher.Verify("outra", hash));
    }
}
