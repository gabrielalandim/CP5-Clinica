using Clinica.Application.Dtos;
using Clinica.Domain.Entities;

namespace Clinica.Application.Mappers;

public static class PacienteMapper
{
    public static GetPacienteDto ToDto(this PacienteEntity e, IEnumerable<LinkDto> links) => new()
    {
        Id = e.Id,
        Nome = e.Nome,
        Email = e.Email,
        Cep = e.Cep,
        Logradouro = e.Logradouro,
        Cidade = e.Cidade,
        Uf = e.Uf,
        Links = links.ToList()
    };
}

public static class ConsultaMapper
{
    public static GetConsultaDto ToDto(this ConsultaEntity e, IEnumerable<LinkDto> links) => new()
    {
        Id = e.Id,
        PacienteId = e.PacienteId,
        DataHora = e.DataHora,
        Especialidade = e.Especialidade,
        Valor = e.Valor,
        Status = e.Status,
        Links = links.ToList()
    };
}

public static class UserMapper
{
    public static GetUserDto ToDto(this UserEntity e) => new()
    {
        Id = e.Id,
        Nome = e.Nome,
        Email = e.Email,
        Role = e.Role
    };
}
