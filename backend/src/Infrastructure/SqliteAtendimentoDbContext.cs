using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
namespace Infrastructure;
public sealed class SqliteAtendimentoDbContext(DbContextOptions<SqliteAtendimentoDbContext> options) : AtendimentoDbContext(options);
public sealed class SqliteDbContextFactory : IDesignTimeDbContextFactory<SqliteAtendimentoDbContext>
{
    public SqliteAtendimentoDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<SqliteAtendimentoDbContext>().UseSqlite(Environment.GetEnvironmentVariable("ConnectionStrings__Sqlite") ?? "Data Source=atendimento.db");
        return new(options.Options);
    }
}
