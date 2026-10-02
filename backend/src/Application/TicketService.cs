using Domain;
using FluentValidation;
namespace Application;
public sealed class TicketService(IAtendimentoRepository repository, SlaService sla, TimeProvider clock,
    IValidator<TicketInput> ticketValidator, IValidator<MensagemInput> mensagemValidator, IValidator<LoteInput> loteValidator, IValidator<FiltroTickets> filtroValidator)
{
    public TicketDto Mapear(Ticket t, DateTime agora) => new(t.Id, t.Assunto, t.Descricao, new(t.ClienteId, t.Cliente.Nome),
        t.Tecnico is null ? null : new(t.Tecnico.Id, t.Tecnico.Nome), t.Equipe is null ? null : new(t.Equipe.Id, t.Equipe.Nome),
        t.Status, t.Prioridade, t.Canal, t.CriadoEm, t.AtualizadoEm, t.PrimeiraRespostaEm, t.ResolvidoEm, t.Csat, sla.Calcular(t, agora));
    public async Task<PaginaDto<TicketDto>> Listar(FiltroTickets filtro, CancellationToken ct)
    {
        await filtroValidator.ValidateAndThrowAsync(filtro, ct);
        var pagina = await repository.PaginarTickets(filtro, ct); var agora = clock.GetUtcNow().UtcDateTime;
        return new(pagina.Itens.Select(t => Mapear(t, agora)).ToList(), pagina.Total, filtro.Pagina, filtro.TamanhoPagina);
    }
    public async Task<TicketDetalheDto> Detalhe(int id, CancellationToken ct)
    {
        var t = await Encontrar(id, ct);
        var timeline = t.Mensagens.Select(m => new TimelineDto(m.CriadoEm, m.NotaInterna ? "NotaInterna" : "Resposta", new(m.Id, m.Conteudo, m.NotaInterna, new(m.TecnicoId, m.Tecnico.Nome), m.CriadoEm), null))
            .Concat(t.Historico.Select(h => new TimelineDto(h.CriadoEm, "Alteracao", null, new(h.Id, h.Campo, h.Anterior, h.Novo, h.CriadoEm))))
            .OrderBy(x => x.CriadoEm).ThenBy(x => x.Tipo).ToList();
        return new(Mapear(t, clock.GetUtcNow().UtcDateTime), t.Cliente.Email, timeline);
    }
    private async Task<Ticket> Encontrar(int id, CancellationToken ct) => await repository.ObterTicket(id, ct) ?? throw new NaoEncontradoException("Ticket não encontrado.");
    public async Task<TicketDetalheDto> Criar(TicketInput input, CancellationToken ct)
    {
        await ticketValidator.ValidateAndThrowAsync(input, ct);
        if (input.Status != StatusTicket.Aberto) throw new RegraNegocioException("Novos tickets devem iniciar como Aberto.");
        if (input.Csat.HasValue) throw new RegraNegocioException("CSAT só pode ser informado após encerramento.");
        var (cliente, tecnico, equipe) = await ValidarReferencias(input, ct);
        var config = await sla.Obter(ct); var regra = config.Regras.Single(r => r.Prioridade == input.Prioridade);
        var agora = clock.GetUtcNow().UtcDateTime;
        var t = new Ticket { Assunto = input.Assunto.Trim(), Descricao = input.Descricao.Trim(), ClienteId = cliente.Id, Cliente = cliente,
            TecnicoId = tecnico?.Id, Tecnico = tecnico, EquipeId = equipe?.Id, Equipe = equipe, Prioridade = input.Prioridade, Canal = input.Canal,
            CriadoEm = agora, AtualizadoEm = agora, Status = StatusTicket.Aberto, PrazoPrimeiraRespostaMinutos = regra.PrimeiraRespostaMinutos,
            PrazoResolucaoMinutos = regra.ResolucaoMinutos, HorarioSlaJson = SlaService.SerializarHorario(config.Horario) };
        t.Historico.Add(new HistoricoTicket { Campo = "Criacao", Novo = "Aberto", CriadoEm = agora });
        repository.Adicionar(t); await repository.Salvar(ct); return await Detalhe(t.Id, ct);
    }
    public async Task<TicketDetalheDto> Atualizar(int id, TicketInput input, CancellationToken ct)
    {
        await ticketValidator.ValidateAndThrowAsync(input, ct);
        if (input.Csat.HasValue && input.Status is not (StatusTicket.Resolvido or StatusTicket.Fechado)) throw new RegraNegocioException("CSAT só pode ser informado após encerramento.");
        var t = await Encontrar(id, ct); var (cliente, tecnico, equipe) = await ValidarReferencias(input, ct);
        var agora = clock.GetUtcNow().UtcDateTime;
        Historico(t, "Assunto", t.Assunto, input.Assunto.Trim(), agora); Historico(t, "Descricao", t.Descricao, input.Descricao.Trim(), agora);
        Historico(t, "ClienteId", t.ClienteId.ToString(), cliente.Id.ToString(), agora);
        Historico(t, "TecnicoId", t.TecnicoId?.ToString(), tecnico?.Id.ToString(), agora); Historico(t, "EquipeId", t.EquipeId?.ToString(), equipe?.Id.ToString(), agora);
        Historico(t, "Canal", t.Canal.ToString(), input.Canal.ToString(), agora); Historico(t, "Csat", t.Csat?.ToString(), input.Csat?.ToString(), agora);
        await AlterarPrioridade(t, input.Prioridade, agora, ct);
        t.Assunto = input.Assunto.Trim(); t.Descricao = input.Descricao.Trim(); t.Cliente = cliente; t.ClienteId = cliente.Id;
        t.Tecnico = tecnico; t.TecnicoId = tecnico?.Id; t.Equipe = equipe; t.EquipeId = equipe?.Id; t.Canal = input.Canal; t.Csat = input.Csat;
        t.AlterarStatus(input.Status, agora); t.AtualizadoEm = agora;
        await repository.Salvar(ct); return await Detalhe(t.Id, ct);
    }
    public async Task<IReadOnlyList<TicketDto>> Lote(LoteInput input, CancellationToken ct)
    {
        await loteValidator.ValidateAndThrowAsync(input, ct);
        var tickets = await repository.ObterTickets(input.Ids, ct);
        if (tickets.Count != input.Ids.Length) throw new NaoEncontradoException("Um ou mais tickets não existem. Nenhuma alteração foi aplicada.");
        var tecnico = input.TecnicoId.HasValue ? (await repository.Tecnicos(ct)).SingleOrDefault(t => t.Id == input.TecnicoId) ?? throw new NaoEncontradoException("Técnico não encontrado.") : null;
        var agora = clock.GetUtcNow().UtcDateTime;
        foreach (var t in tickets)
        {
            if (tecnico is not null)
            {
                Historico(t, "TecnicoId", t.TecnicoId?.ToString(), tecnico.Id.ToString(), agora); Historico(t, "EquipeId", t.EquipeId?.ToString(), tecnico.EquipeId.ToString(), agora);
                t.TecnicoId = tecnico.Id; t.Tecnico = tecnico; t.EquipeId = tecnico.EquipeId; t.Equipe = tecnico.Equipe;
            }
            if (input.Prioridade.HasValue) await AlterarPrioridade(t, input.Prioridade.Value, agora, ct);
            if (input.Status.HasValue) { t.AlterarStatus(input.Status.Value, agora); if (!t.Encerrado) t.Csat = null; }
            t.AtualizadoEm = agora;
        }
        await repository.Salvar(ct); return tickets.Select(t => Mapear(t, agora)).ToList();
    }
    public async Task<MensagemDto> Mensagem(int id, MensagemInput input, CancellationToken ct)
    {
        await mensagemValidator.ValidateAndThrowAsync(input, ct);
        var t = await Encontrar(id, ct);
        var tecnico = (await repository.Tecnicos(ct)).SingleOrDefault(x => x.Id == input.TecnicoId) ?? throw new NaoEncontradoException("Técnico não encontrado.");
        if (t.Encerrado && !input.NotaInterna) throw new RegraNegocioException("Reabra o ticket antes de responder.");
        var agora = clock.GetUtcNow().UtcDateTime;
        var m = new Mensagem { Conteudo = input.Conteudo.Trim(), TecnicoId = tecnico.Id, Tecnico = tecnico, NotaInterna = input.NotaInterna, CriadoEm = agora };
        t.Mensagens.Add(m);
        if (!input.NotaInterna) { t.PrimeiraRespostaEm ??= agora; if (t.Status == StatusTicket.Aberto) t.AlterarStatus(StatusTicket.EmAndamento, agora); }
        t.AtualizadoEm = agora; await repository.Salvar(ct);
        return new(m.Id, m.Conteudo, m.NotaInterna, new(tecnico.Id, tecnico.Nome), agora);
    }
    private async Task<(Cliente, Tecnico?, Equipe?)> ValidarReferencias(TicketInput input, CancellationToken ct)
    {
        var cliente = (await repository.Clientes(ct)).SingleOrDefault(c => c.Id == input.ClienteId) ?? throw new NaoEncontradoException("Cliente não encontrado.");
        var tecnico = input.TecnicoId.HasValue ? (await repository.Tecnicos(ct)).SingleOrDefault(t => t.Id == input.TecnicoId) ?? throw new NaoEncontradoException("Técnico não encontrado.") : null;
        var equipeId = input.EquipeId ?? tecnico?.EquipeId;
        var equipe = equipeId.HasValue ? (await repository.Equipes(ct)).SingleOrDefault(e => e.Id == equipeId) ?? throw new NaoEncontradoException("Equipe não encontrada.") : null;
        if (tecnico is not null && tecnico.EquipeId != equipe?.Id) throw new RegraNegocioException("Técnico não pertence à equipe informada.");
        return (cliente, tecnico, equipe);
    }
    private async Task AlterarPrioridade(Ticket t, Prioridade nova, DateTime agora, CancellationToken ct)
    {
        if (t.Prioridade == nova) return;
        if (t.Encerrado) throw new RegraNegocioException("Reabra o ticket antes de alterar a prioridade.");
        var config = await sla.Obter(ct); var regra = config.Regras.Single(r => r.Prioridade == nova);
        Historico(t, "Prioridade", t.Prioridade.ToString(), nova.ToString(), agora); t.Prioridade = nova;
        // A primeira resposta concluída conserva sua meta original. Resolução retém o tempo já gasto.
        if (!t.PrimeiraRespostaEm.HasValue) t.PrazoPrimeiraRespostaMinutos = regra.PrimeiraRespostaMinutos;
        t.PrazoResolucaoMinutos = regra.ResolucaoMinutos;
    }
    private static void Historico(Ticket t, string campo, string? anterior, string? novo, DateTime agora)
    {
        if (anterior == novo) return;
        t.Historico.Add(new HistoricoTicket { Campo = campo, Anterior = anterior, Novo = novo ?? "", CriadoEm = agora });
    }
}
