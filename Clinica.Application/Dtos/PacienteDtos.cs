using System.ComponentModel.DataAnnotations;

namespace Clinica.Application.Dtos;

public class PostPacienteDto
{
    [Required, StringLength(150)]
    public string Nome { get; set; } = string.Empty;

    [Required, EmailAddress, StringLength(150)]
    public string Email { get; set; } = string.Empty;

    /// <summary>CEP com 8 dígitos (aceita hífen). O endereço é buscado no ViaCEP.</summary>
    [Required]
    public string Cep { get; set; } = string.Empty;
}

public class PutPacienteDto
{
    [Required, StringLength(150)]
    public string Nome { get; set; } = string.Empty;

    [Required, EmailAddress, StringLength(150)]
    public string Email { get; set; } = string.Empty;
}

public class GetPacienteDto
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Cep { get; set; } = string.Empty;
    public string Logradouro { get; set; } = string.Empty;
    public string Cidade { get; set; } = string.Empty;
    public string Uf { get; set; } = string.Empty;
    public List<LinkDto> Links { get; set; } = new();
}
