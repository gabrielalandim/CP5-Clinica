using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace Clinica.Infrastructure.Data;

public static class QueryableExtensions
{
    /// <summary>
    /// Verifica existência de forma compatível com Oracle 19c e otimizada.
    /// <para>
    /// O <c>AnyAsync</c> do Oracle.EntityFrameworkCore 8.23 gera
    /// <c>CASE WHEN EXISTS(...) THEN TRUE ELSE FALSE END</c> (sintaxe do Oracle 23ai),
    /// que no banco da FIAP (19c) causa <c>ORA-00904: "FALSE": identificador inválido</c>.
    /// </para>
    /// Aqui geramos <c>SELECT 1 ... FETCH FIRST 1 ROWS ONLY</c>: para no primeiro registro
    /// encontrado (não conta a tabela inteira) e não usa literais booleanos.
    /// </summary>
    public static async Task<bool> ExisteAsync<T>(this IQueryable<T> query, Expression<Func<T, bool>> predicate)
        where T : class
    {
        var encontrado = await query
            .AsNoTracking()
            .Where(predicate)
            .Select(_ => 1)
            .FirstOrDefaultAsync();

        return encontrado == 1;
    }
}
