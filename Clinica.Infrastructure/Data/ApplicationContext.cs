using Clinica.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Clinica.Infrastructure.Data;

public class ApplicationContext : DbContext
{
    public ApplicationContext(DbContextOptions<ApplicationContext> options) : base(options) { }

    public DbSet<UserEntity> Usuarios => Set<UserEntity>();
    public DbSet<PacienteEntity> Pacientes => Set<PacienteEntity>();
    public DbSet<ConsultaEntity> Consultas => Set<ConsultaEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<UserEntity>(e =>
        {
            e.ToTable("tb_clinica_usuario");
            e.HasKey(x => x.Id);
            e.Property(x => x.Nome).HasMaxLength(100).IsRequired();
            e.Property(x => x.Email).HasMaxLength(150).IsRequired();
            e.Property(x => x.PasswordHash).HasMaxLength(300).IsRequired();
            e.Property(x => x.Role).HasMaxLength(20).IsRequired();
            e.HasIndex(x => x.Email).IsUnique();
        });

        modelBuilder.Entity<PacienteEntity>(e =>
        {
            e.ToTable("tb_clinica_paciente");
            e.HasKey(x => x.Id);
            e.Property(x => x.Nome).HasMaxLength(150).IsRequired();
            e.Property(x => x.Email).HasMaxLength(150).IsRequired();
            e.Property(x => x.Cep).HasMaxLength(8).IsRequired();
            e.Property(x => x.Logradouro).HasMaxLength(200);
            e.Property(x => x.Cidade).HasMaxLength(100);
            e.Property(x => x.Uf).HasMaxLength(2);
            e.HasIndex(x => x.Email).IsUnique();
        });

        modelBuilder.Entity<ConsultaEntity>(e =>
        {
            e.ToTable("tb_clinica_consulta");
            e.HasKey(x => x.Id);
            e.Property(x => x.Especialidade).HasMaxLength(80).IsRequired();
            e.Property(x => x.Status).HasMaxLength(20).IsRequired();
            e.Property(x => x.Valor).HasPrecision(10, 2);
            e.HasIndex(x => new { x.DataHora, x.Especialidade });
            e.HasOne(x => x.Paciente)
                .WithMany(p => p.Consultas)
                .HasForeignKey(x => x.PacienteId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
