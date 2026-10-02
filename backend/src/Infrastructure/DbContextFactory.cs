using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
namespace Infrastructure;
public sealed class DbContextFactory : IDesignTimeDbContextFactory<AtendimentoDbContext>
{
    public AtendimentoDbContext CreateDbContext(string[] args) => new(new DbContextOptionsBuilder<AtendimentoDbContext>().UseSqlServer(Environment.GetEnvironmentVariable("ConnectionStrings__SqlServer") ?? "Server=localhost,1433;Database=Atendimento;User Id=sa;Password=Local_Only_ChangeMe123!;TrustServerCertificate=True").Options);
}
