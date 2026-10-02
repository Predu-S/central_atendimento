using Domain;
namespace Application;
public class FiltroTickets
{
    public DateTime? Inicio { get; set; }
    public DateTime? Fim { get; set; }
    public int? EquipeId { get; set; }
    public int? TecnicoId { get; set; }
    public Prioridade? Prioridade { get; set; }
    public Canal? Canal { get; set; }
    public StatusTicket? Status { get; set; }
    public int? ClienteId { get; set; }
    public string? Busca { get; set; }
    public int Pagina { get; set; } = 1;
    public int TamanhoPagina { get; set; } = 20;
    public string OrdenarPor { get; set; } = "criadoEm";
    public bool Descendente { get; set; } = true;
}
public record TicketInput(string Assunto, string Descricao, int ClienteId, int? TecnicoId, int? EquipeId, Prioridade Prioridade, Canal Canal, StatusTicket Status = StatusTicket.Aberto, int? Csat = null);
public record LoteInput(int[] Ids, int? TecnicoId, StatusTicket? Status, Prioridade? Prioridade);
public record MensagemInput(string Conteudo, int TecnicoId, bool NotaInterna = false);
public record PessoaDto(int Id, string Nome);
public record ClienteDto(int Id, string Nome, string Email, int TotalTickets, DateTime? UltimoContato, double? Csat);
public record MensagemDto(int Id, string Conteudo, bool NotaInterna, PessoaDto Tecnico, DateTime CriadoEm);
public record HistoricoDto(int Id, string Campo, string? Anterior, string Novo, DateTime CriadoEm);
public record TimelineDto(DateTime CriadoEm, string Tipo, MensagemDto? Mensagem, HistoricoDto? Alteracao);
public record TicketDto(int Id, string Assunto, string Descricao, PessoaDto Cliente, PessoaDto? Tecnico, PessoaDto? Equipe,
    StatusTicket Status, Prioridade Prioridade, Canal Canal, DateTime CriadoEm, DateTime AtualizadoEm,
    DateTime? PrimeiraRespostaEm, DateTime? ResolvidoEm, int? Csat, SlaTicket Sla);
public record TicketDetalheDto(TicketDto Ticket, string EmailCliente, IReadOnlyList<TimelineDto> Timeline);
public record PaginaDto<T>(IReadOnlyList<T> Itens, int Total, int Pagina, int TamanhoPagina);
public record RegraSlaDto(Prioridade Prioridade, int PrimeiraRespostaMinutos, int ResolucaoMinutos);
public record ConfiguracaoSlaDto(IReadOnlyList<RegraSlaDto> Regras, HorarioComercial Horario);
public record KpiDto(double? Valor, double? Anterior, double? VariacaoPercentual);
public record ResumoDto(KpiDto Abertos, KpiDto EmAndamento, KpiDto AguardandoCliente, KpiDto ResolvidosHoje,
    KpiDto TempoMedioPrimeiraRespostaMinutos, KpiDto TempoMedioResolucaoMinutos, KpiDto SlaCumpridoPercentual, KpiDto Csat);
public record SerieDiaDto(DateOnly Dia, int Abertos, int Resolvidos);
public record VolumeDto(string Nome, int Quantidade, int? Id = null);
public record SeriesDto(IReadOnlyList<SerieDiaDto> PorDia, IReadOnlyList<VolumeDto> PorCanal, IReadOnlyList<VolumeDto> PorPrioridade, IReadOnlyList<VolumeDto> PorTecnico);
public record EvolucaoSlaDto(DateOnly Semana, int Cumpridos, int Violados, double? PercentualCumprido);
public record IndicadoresSlaDto(int Cumpridos, int Violados, int EmRisco, int Pendentes, double? PercentualCumprido, IReadOnlyList<EvolucaoSlaDto> EvolucaoSemanal);
public record ViolacaoDto(TicketDto Ticket, IReadOnlyList<string> Motivos);
public record RankingDto(PessoaDto Tecnico, PessoaDto Equipe, int Resolvidos, double? TempoMedioResolucaoMinutos, double? Csat, double? SlaCumpridoPercentual);
public record CargaDto(PessoaDto Tecnico, PessoaDto Equipe, int Abertos, int EmAndamento, int AguardandoCliente, int Total, bool Sobrecarga);
public interface IAtendimentoRepository
{
    Task<List<Ticket>> ListarTickets(FiltroTickets filtro, CancellationToken ct);
    Task<(List<Ticket> Itens, int Total)> PaginarTickets(FiltroTickets filtro, CancellationToken ct);
    Task<Ticket?> ObterTicket(int id, CancellationToken ct);
    Task<List<Ticket>> ObterTickets(int[] ids, CancellationToken ct);
    Task<List<Cliente>> Clientes(CancellationToken ct);
    Task<List<Tecnico>> Tecnicos(CancellationToken ct);
    Task<List<Equipe>> Equipes(CancellationToken ct);
    Task<List<RegraSla>> Regras(CancellationToken ct);
    Task<ConfiguracaoSla?> Configuracao(CancellationToken ct);
    void Adicionar(Ticket ticket);
    void Adicionar(ConfiguracaoSla configuracao);
    Task Salvar(CancellationToken ct);
}
public sealed class NaoEncontradoException(string mensagem) : Exception(mensagem);
public sealed class RegraNegocioException(string mensagem) : Exception(mensagem);
