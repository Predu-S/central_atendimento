using Application;
using Domain;
using Microsoft.EntityFrameworkCore;
namespace Infrastructure;
public sealed class AtendimentoRepository(AtendimentoDbContext db) : IAtendimentoRepository
{
    private IQueryable<Ticket> Consulta(FiltroTickets f)
    {
        var q = db.Tickets.AsQueryable();
        if (f.Inicio.HasValue) q = q.Where(t => t.CriadoEm >= f.Inicio.Value);
        if (f.Fim.HasValue) q = q.Where(t => t.CriadoEm < f.Fim.Value);
        if (f.EquipeId.HasValue) q = q.Where(t => t.EquipeId == f.EquipeId);
        if (f.TecnicoId.HasValue) q = q.Where(t => t.TecnicoId == f.TecnicoId);
        if (f.ClienteId.HasValue) q = q.Where(t => t.ClienteId == f.ClienteId);
        if (f.Status.HasValue) q = q.Where(t => t.Status == f.Status);
        if (f.Prioridade.HasValue) q = q.Where(t => t.Prioridade == f.Prioridade);
        if (f.Canal.HasValue) q = q.Where(t => t.Canal == f.Canal);
        if (!string.IsNullOrWhiteSpace(f.Busca))
        {
            var busca = f.Busca.Trim().ToLower(); var numerico = int.TryParse(busca, out var id);
            q = q.Where(t => t.Assunto.ToLower().Contains(busca) || t.Descricao.ToLower().Contains(busca) || t.Cliente.Nome.ToLower().Contains(busca) || (numerico && t.Id == id));
        }
        return q;
    }
    private static IQueryable<Ticket> Incluir(IQueryable<Ticket> q) => q.Include(t => t.Cliente).Include(t => t.Tecnico).Include(t => t.Equipe).Include(t => t.Pausas).Include(t => t.Historico).AsSplitQuery();
    public Task<List<Ticket>> ListarTickets(FiltroTickets f, CancellationToken ct) => Incluir(Consulta(f)).AsNoTracking().ToListAsync(ct);
    public async Task<(List<Ticket> Itens, int Total)> PaginarTickets(FiltroTickets f, CancellationToken ct)
    {
        var q = Consulta(f); var total = await q.CountAsync(ct);
        IOrderedQueryable<Ticket> ordenado = (f.OrdenarPor, f.Descendente) switch {
            ("atualizadoEm", true) => q.OrderByDescending(t => t.AtualizadoEm), ("atualizadoEm", false) => q.OrderBy(t => t.AtualizadoEm),
            ("prioridade", true) => q.OrderByDescending(t => t.Prioridade), ("prioridade", false) => q.OrderBy(t => t.Prioridade),
            ("status", true) => q.OrderByDescending(t => t.Status), ("status", false) => q.OrderBy(t => t.Status),
            ("assunto", true) => q.OrderByDescending(t => t.Assunto), ("assunto", false) => q.OrderBy(t => t.Assunto),
            ("id", true) => q.OrderByDescending(t => t.Id), ("id", false) => q.OrderBy(t => t.Id),
            (_, true) => q.OrderByDescending(t => t.CriadoEm), _ => q.OrderBy(t => t.CriadoEm) };
        // O offset é calculado em long para evitar overflow; páginas além do total ficam vazias.
        var offset = ((long)f.Pagina - 1) * f.TamanhoPagina;
        if (offset >= total) return ([], total);
        var itens = await Incluir(ordenado.ThenBy(t => t.Id).Skip((int)offset).Take(f.TamanhoPagina)).AsNoTracking().ToListAsync(ct);
        return (itens, total);
    }
    public Task<Ticket?> ObterTicket(int id, CancellationToken ct) => Incluir(db.Tickets).Include(t => t.Mensagens).ThenInclude(m => m.Tecnico).SingleOrDefaultAsync(t => t.Id == id, ct);
    public Task<List<Ticket>> ObterTickets(int[] ids, CancellationToken ct) => Incluir(db.Tickets.Where(t => ids.Contains(t.Id))).ToListAsync(ct);
    public Task<List<Cliente>> Clientes(CancellationToken ct) => db.Clientes.ToListAsync(ct);
    public Task<List<Tecnico>> Tecnicos(CancellationToken ct) => db.Tecnicos.Include(t => t.Equipe).ToListAsync(ct);
    public Task<List<Equipe>> Equipes(CancellationToken ct) => db.Equipes.ToListAsync(ct);
    public Task<List<RegraSla>> Regras(CancellationToken ct) => db.RegrasSla.ToListAsync(ct);
    public Task<ConfiguracaoSla?> Configuracao(CancellationToken ct) => db.ConfiguracoesSla.SingleOrDefaultAsync(ct);
    public void Adicionar(Ticket t) => db.Tickets.Add(t); public void Adicionar(ConfiguracaoSla c) => db.ConfiguracoesSla.Add(c);
    public Task Salvar(CancellationToken ct) => db.SaveChangesAsync(ct);
}
