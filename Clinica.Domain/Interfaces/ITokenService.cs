using Clinica.Domain.Entities;

namespace Clinica.Domain.Interfaces;

/// <summary>Geração do token de acesso (implementado na Infrastructure com JWT).</summary>
public interface ITokenService
{
    string GerarToken(UserEntity user);
}
