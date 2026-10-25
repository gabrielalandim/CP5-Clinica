using System.ComponentModel.DataAnnotations;

namespace Clinica.Application.Dtos;

public class PostConsultaDto
{
    [Range(1, int.MaxValue)]
    public int PacienteId { get; set; }

    [Required]
    public DateTime DataHora { get; set; }

    [Required, StringLength(80)]
    public string Especialidade { get; set; } = string.Empty;

    [Range(0.0, 100000.0)]
    public decimal Valor { get; set; }
}

public class GetConsultaDto
{
    public int Id { get; set; }
    public int PacienteId { get; set; }
    public DateTime DataHora { get; set; }
    public string Especialidade { get; set; } = string.Empty;
    public decimal Valor { get; set; }
    public string Status { get; set; } = string.Empty;
    public List<LinkDto> Links { get; set; } = new();
}
