using Domain;
using Microsoft.EntityFrameworkCore;
namespace Infrastructure;
public class AtendimentoDbContext(DbContextOptions options) : DbContext(options)
{
    public DbSet<Ticket> Tickets => Set<Ticket>(); public DbSet<Cliente> Clientes => Set<Cliente>();
    public DbSet<Tecnico> Tecnicos => Set<Tecnico>(); public DbSet<Equipe> Equipes => Set<Equipe>();
    public DbSet<RegraSla> RegrasSla => Set<RegraSla>(); public DbSet<ConfiguracaoSla> ConfiguracoesSla => Set<ConfiguracaoSla>();
    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<Cliente>().Property(x => x.Nome).HasMaxLength(200); b.Entity<Cliente>().Property(x => x.Email).HasMaxLength(254);
        b.Entity<Tecnico>().Property(x => x.Nome).HasMaxLength(200); b.Entity<Equipe>().Property(x => x.Nome).HasMaxLength(200);
        b.Entity<Tecnico>().HasOne(x => x.Equipe).WithMany().HasForeignKey(x => x.EquipeId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<RegraSla>().HasIndex(x => x.Prioridade).IsUnique();
        var t = b.Entity<Ticket>(); t.Ignore(x => x.Encerrado);
        t.Property(x => x.Assunto).HasMaxLength(200); t.Property(x => x.Descricao).HasMaxLength(10000);
        t.HasOne(x => x.Cliente).WithMany().HasForeignKey(x => x.ClienteId).OnDelete(DeleteBehavior.Restrict);
        t.HasOne(x => x.Tecnico).WithMany().HasForeignKey(x => x.TecnicoId).OnDelete(DeleteBehavior.Restrict);
        t.HasOne(x => x.Equipe).WithMany().HasForeignKey(x => x.EquipeId).OnDelete(DeleteBehavior.Restrict);
        t.HasMany(x => x.Mensagens).WithOne().HasForeignKey(x => x.TicketId).OnDelete(DeleteBehavior.Cascade);
        t.HasMany(x => x.Historico).WithOne().HasForeignKey(x => x.TicketId).OnDelete(DeleteBehavior.Cascade);
        t.HasMany(x => x.Pausas).WithOne().HasForeignKey(x => x.TicketId).OnDelete(DeleteBehavior.Cascade);
        t.HasIndex(x => x.CriadoEm); t.HasIndex(x => new { x.Status, x.Prioridade }); t.HasIndex(x => x.ResolvidoEm);
        b.Entity<Mensagem>().Property(x => x.Conteudo).HasMaxLength(10000);
        b.Entity<Mensagem>().HasOne(x => x.Tecnico).WithMany().HasForeignKey(x => x.TecnicoId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<HistoricoTicket>().Property(x => x.Campo).HasMaxLength(100);
        b.Entity<HistoricoTicket>().Property(x => x.Anterior).HasMaxLength(10000); b.Entity<HistoricoTicket>().Property(x => x.Novo).HasMaxLength(10000);
        // SQLite não conserva DateTime.Kind. Todas as datas persistidas são UTC.
        foreach (var entity in b.Model.GetEntityTypes()) foreach (var p in entity.GetProperties())
        {
            if (p.ClrType == typeof(DateTime)) p.SetValueConverter(new Microsoft.EntityFrameworkCore.Storage.ValueConversion.ValueConverter<DateTime, DateTime>(v => v, v => DateTime.SpecifyKind(v, DateTimeKind.Utc)));
            if (p.ClrType == typeof(DateTime?)) p.SetValueConverter(new Microsoft.EntityFrameworkCore.Storage.ValueConversion.ValueConverter<DateTime?, DateTime?>(v => v, v => v.HasValue ? DateTime.SpecifyKind(v.Value, DateTimeKind.Utc) : null));
        }
    }
}
