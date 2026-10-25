using Clinica.Domain.Models;

namespace Clinica.Domain.Interfaces;

public interface IEnderecoService
{
    /// <exception cref="Clinica.Domain.Errors.NotFoundException">CEP inválido ou inexistente.</exception>
    /// <exception cref="Clinica.Domain.Errors.ExternalServiceException">Serviço externo indisponível.</exception>
    Task<EnderecoModel> ObterPorCepAsync(string cep);
}
