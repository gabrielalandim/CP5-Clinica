namespace Clinica.Domain.Entities;

public static class StatusConsulta
{
    public const string Agendada = "Agendada";
    public const string Cancelada = "Cancelada";
}

public class ConsultaEntity
{
    public int Id { get; set; }
    public int PacienteId { get; set; }
    public PacienteEntity? Paciente { get; set; }
    public DateTime DataHora { get; set; }
    public string Especialidade { get; set; } = string.Empty;
    public decimal Valor { get; set; }
    public string Status { get; set; } = StatusConsulta.Agendada;
}
