using Clinica.Domain.Entities;
using Clinica.Domain.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;

namespace Clinica.Infrastructure.Data;

/// <summary>Cria as tabelas (se não existirem) e os usuários iniciais.</summary>
public static class DbInitializer
{
    // Garante que só uma inicialização rode por vez no processo
    // (os testes de integração sobem a API mais de uma vez em paralelo).
    private static readonly SemaphoreSlim Lock = new(1, 1);

    public static async Task InitializeAsync(ApplicationContext db)
    {
        await Lock.WaitAsync();
        try
        {
            if (!await TabelaExisteAsync(db, "tb_clinica_usuario"))
            {
                var creator = db.GetService<IRelationalDatabaseCreator>();
                await creator.CreateTablesAsync();
            }

            await SeedUserAsync(db, "Administrador", "admin@clinica.com", "Admin@123", "admin");
            await SeedUserAsync(db, "Usuário Comum", "user@clinica.com", "User@123", "user");
        }
        finally
        {
            Lock.Release();
        }
    }

    /// <summary>Consulta o dicionário do Oracle (USER_TABLES) para saber se a tabela já existe.</summary>
    private static async Task<bool> TabelaExisteAsync(ApplicationContext db, string tabela)
    {
        var connection = db.Database.GetDbConnection();
        await db.Database.OpenConnectionAsync();
        try
        {
            await using var command = connection.CreateCommand();
            command.CommandText = $"SELECT COUNT(*) FROM USER_TABLES WHERE TABLE_NAME = '{tabela}'";
            var total = Convert.ToInt32(await command.ExecuteScalarAsync());
            return total > 0;
        }
        finally
        {
            await db.Database.CloseConnectionAsync();
        }
    }

    private static async Task SeedUserAsync(ApplicationContext db, string nome, string email, string senha, string role)
    {
        if (await db.Usuarios.ExisteAsync(u => u.Email == email))
            return;

        db.Usuarios.Add(new UserEntity
        {
            Nome = nome,
            Email = email,
            PasswordHash = PasswordHasher.Hash(senha),
            Role = role
        });

        await db.SaveChangesAsync();
    }
}
