namespace Domain;
public enum StatusTicket { Aberto, EmAndamento, AguardandoCliente, Resolvido, Fechado }
public enum Prioridade { Critica, Alta, Media, Baixa }
public enum Canal { WhatsApp, Email, Telefone, Chat }
public enum SituacaoSla { NoPrazo, EmRisco, Violado }
public class Cliente { public int Id { get; set; } public string Nome { get; set; } = ""; public string Email { get; set; } = ""; }
public class Equipe { public int Id { get; set; } public string Nome { get; set; } = ""; }
public class Tecnico { public int Id { get; set; } public string Nome { get; set; } = ""; public int EquipeId { get; set; } public Equipe Equipe { get; set; } = null!; }
public class RegraSla { public int Id { get; set; } public Prioridade Prioridade { get; set; } public int PrimeiraRespostaMinutos { get; set; } public int ResolucaoMinutos { get; set; } }
public class ConfiguracaoSla { public int Id { get; set; } = 1; public string HorarioJson { get; set; } = ""; }
public class Ticket
{
    public int Id { get; set; }
    public string Assunto { get; set; } = "";
    public string Descricao { get; set; } = "";
    public int ClienteId { get; set; }
    public Cliente Cliente { get; set; } = null!;
    public int? TecnicoId { get; set; }
    public Tecnico? Tecnico { get; set; }
    public int? EquipeId { get; set; }
    public Equipe? Equipe { get; set; }
    public StatusTicket Status { get; set; }
    public Prioridade Prioridade { get; set; }
    public Canal Canal { get; set; }
    public DateTime CriadoEm { get; set; }
    public DateTime AtualizadoEm { get; set; }
    public DateTime? PrimeiraRespostaEm { get; set; }
    public DateTime? ResolvidoEm { get; set; }
    public int? Csat { get; set; }
    public int PrazoPrimeiraRespostaMinutos { get; set; }
    public int PrazoResolucaoMinutos { get; set; }
    public string HorarioSlaJson { get; set; } = "";
    public List<Mensagem> Mensagens { get; set; } = [];
    public List<HistoricoTicket> Historico { get; set; } = [];
    public List<PausaSla> Pausas { get; set; } = [];
    public bool Encerrado => Status is StatusTicket.Resolvido or StatusTicket.Fechado;
    public void AlterarStatus(StatusTicket novo, DateTime agora)
    {
        if (novo == Status) return;
        if (Status == StatusTicket.AguardandoCliente)
            foreach (var pausa in Pausas.Where(p => p.Fim == null)) pausa.Fim = agora;
        if (novo == StatusTicket.AguardandoCliente) Pausas.Add(new PausaSla { Inicio = agora });
        // Reabertura retoma o orçamento original; o período encerrado não conta.
        if (Encerrado && novo is not (StatusTicket.Resolvido or StatusTicket.Fechado))
        {
            if (ResolvidoEm.HasValue) Pausas.Add(new PausaSla { Inicio = ResolvidoEm.Value, Fim = agora });
            ResolvidoEm = null;
        }
        if (novo is StatusTicket.Resolvido or StatusTicket.Fechado) ResolvidoEm ??= agora;
        Historico.Add(new HistoricoTicket { CriadoEm = agora, Campo = "Status", Anterior = Status.ToString(), Novo = novo.ToString() });
        Status = novo; AtualizadoEm = agora;
    }
}
public class Mensagem { public int Id { get; set; } public int TicketId { get; set; } public string Conteudo { get; set; } = ""; public bool NotaInterna { get; set; } public int TecnicoId { get; set; } public Tecnico Tecnico { get; set; } = null!; public DateTime CriadoEm { get; set; } }
public class HistoricoTicket { public int Id { get; set; } public int TicketId { get; set; } public string Campo { get; set; } = ""; public string? Anterior { get; set; } public string Novo { get; set; } = ""; public DateTime CriadoEm { get; set; } }
public class PausaSla { public int Id { get; set; } public int TicketId { get; set; } public DateTime Inicio { get; set; } public DateTime? Fim { get; set; } }
