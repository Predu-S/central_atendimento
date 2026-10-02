using Domain;
using FluentValidation;
namespace Application;
public sealed class AnalyticsService(IAtendimentoRepository repository, SlaService sla, TicketService tickets, TimeProvider clock, IValidator<FiltroTickets> validator)
{
    private DateTime Agora => clock.GetUtcNow().UtcDateTime;
    private static bool Dentro(DateTime? data, DateTime inicio, DateTime fim) => data.HasValue && data >= inicio && data < fim;
    private static double? Media(IEnumerable<double> dados) { var a = dados.ToArray(); return a.Length == 0 ? null : a.Average(); }
    private static KpiDto Kpi(double? atual, double? anterior) => new(atual, anterior, anterior is null or 0 || atual is null ? null : (atual - anterior) / Math.Abs(anterior.Value) * 100);
    private static bool Violado(Ticket t, SlaTicket s) => s.PrimeiraResposta.Situacao == SituacaoSla.Violado || s.Resolucao.Situacao == SituacaoSla.Violado || (t.Encerrado && !t.PrimeiraRespostaEm.HasValue);
    private static bool EmRisco(Ticket t, SlaTicket s) => !t.Encerrado && !Violado(t, s) && ((!s.PrimeiraResposta.Concluido && s.PrimeiraResposta.Situacao == SituacaoSla.EmRisco) || s.Resolucao.Situacao == SituacaoSla.EmRisco);
    private static double Restante(SlaTicket s) => s.PrimeiraResposta.Concluido ? s.Resolucao.RestanteMinutos : Math.Min(s.PrimeiraResposta.RestanteMinutos, s.Resolucao.RestanteMinutos);
    private async Task<(List<Ticket> Dados, DateTime Inicio, DateTime Fim)> Dados(FiltroTickets filtro, CancellationToken ct, bool somenteCriados = false)
    {
        await validator.ValidateAndThrowAsync(filtro, ct);
        var fim = filtro.Fim ?? Agora; var inicio = filtro.Inicio ?? fim.AddDays(-30);
        if (fim <= inicio || (fim - inicio).TotalDays > 366) throw new RegraNegocioException("Informe um período positivo de até 366 dias.");
        var consulta = new FiltroTickets { Inicio = somenteCriados ? inicio : null, Fim = somenteCriados ? fim : null,
            TecnicoId = filtro.TecnicoId, EquipeId = filtro.EquipeId, ClienteId = filtro.ClienteId, Prioridade = filtro.Prioridade, Canal = filtro.Canal, Status = filtro.Status, Busca = filtro.Busca };
        return (await repository.ListarTickets(consulta, ct), inicio, fim);
    }
    public async Task<ResumoDto> Resumo(FiltroTickets filtro, CancellationToken ct)
    {
        var (dados, inicio, fim) = await Dados(filtro, ct); var inicioAnterior = inicio - (fim - inicio);
        var config = await sla.Obter(ct); var fuso = TimeZoneInfo.FindSystemTimeZoneById(config.Horario.FusoHorario);
        double[] Estoque(DateTime limite) => new[] { StatusTicket.Aberto, StatusTicket.EmAndamento, StatusTicket.AguardandoCliente }
            .Select(s => (double)dados.Count(t => t.CriadoEm < limite && StatusEm(t, limite) == s)).ToArray();
        DateTime InicioDia(DateTime limite) => TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(TimeZoneInfo.ConvertTimeFromUtc(limite.AddTicks(-1), fuso).Date, DateTimeKind.Unspecified), fuso);
        double[] a = Estoque(fim), b = Estoque(inicio);
        double? TempoResposta(DateTime i, DateTime f) => Media(dados.Where(t => Dentro(t.PrimeiraRespostaEm, i, f)).Select(t => sla.Calcular(t, f).PrimeiraResposta.ConsumidoMinutos));
        double? TempoResolucao(DateTime i, DateTime f) => Media(dados.Where(t => Dentro(t.ResolvidoEm, i, f)).Select(t => sla.Calcular(t, f).Resolucao.ConsumidoMinutos));
        double? Cumprimento(DateTime i, DateTime f) { var d = dados.Where(t => Dentro(t.ResolvidoEm, i, f)).ToList(); return d.Count == 0 ? null : 100d * d.Count(t => !Violado(t, sla.Calcular(t, f))) / d.Count; }
        double? Csat(DateTime i, DateTime f) => Media(dados.Where(t => Dentro(t.ResolvidoEm, i, f) && t.Csat.HasValue).Select(t => (double)t.Csat!.Value));
        return new(Kpi(a[0], b[0]), Kpi(a[1], b[1]), Kpi(a[2], b[2]),
            Kpi(dados.Count(t => Dentro(t.ResolvidoEm, InicioDia(fim) > inicio ? InicioDia(fim) : inicio, fim)), dados.Count(t => Dentro(t.ResolvidoEm, InicioDia(inicio) > inicioAnterior ? InicioDia(inicio) : inicioAnterior, inicio))),
            Kpi(TempoResposta(inicio, fim), TempoResposta(inicioAnterior, inicio)), Kpi(TempoResolucao(inicio, fim), TempoResolucao(inicioAnterior, inicio)),
            Kpi(Cumprimento(inicio, fim), Cumprimento(inicioAnterior, inicio)), Kpi(Csat(inicio, fim), Csat(inicioAnterior, inicio)));
    }
    private static StatusTicket StatusEm(Ticket t, DateTime limite)
    {
        var evento = t.Historico.Where(h => h.Campo == "Status" && h.CriadoEm < limite).OrderByDescending(h => h.CriadoEm).ThenByDescending(h => h.Id).FirstOrDefault();
        return evento is not null && Enum.TryParse<StatusTicket>(evento.Novo, out var status) ? status : StatusTicket.Aberto;
    }
    public async Task<SeriesDto> Series(FiltroTickets filtro, CancellationToken ct)
    {
        var (dados, inicio, fim) = await Dados(filtro, ct); var config = await sla.Obter(ct);
        var fuso = TimeZoneInfo.FindSystemTimeZoneById(config.Horario.FusoHorario);
        DateOnly Dia(DateTime d) => DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(d, DateTimeKind.Utc), fuso));
        var criados = dados.Where(t => Dentro(t.CriadoEm, inicio, fim)).ToList();
        var abertos = criados.GroupBy(t => Dia(t.CriadoEm)).ToDictionary(g => g.Key, g => g.Count());
        var resolvidos = dados.Where(t => Dentro(t.ResolvidoEm, inicio, fim)).GroupBy(t => Dia(t.ResolvidoEm!.Value)).ToDictionary(g => g.Key, g => g.Count());
        var dias = new List<SerieDiaDto>(); for (var d = Dia(inicio); d <= Dia(fim.AddTicks(-1)); d = d.AddDays(1)) dias.Add(new(d, abertos.GetValueOrDefault(d), resolvidos.GetValueOrDefault(d)));
        return new(dias,
            Enum.GetValues<Canal>().Select(c => new VolumeDto(c.ToString(), criados.Count(t => t.Canal == c))).ToList(),
            Enum.GetValues<Prioridade>().Select(p => new VolumeDto(p.ToString(), criados.Count(t => t.Prioridade == p))).ToList(),
            criados.GroupBy(t => new { t.TecnicoId, Nome = t.Tecnico?.Nome ?? "Não atribuído" }).Select(g => new VolumeDto(g.Key.Nome, g.Count(), g.Key.TecnicoId)).OrderByDescending(x => x.Quantidade).ToList());
    }
    public async Task<IReadOnlyList<TicketDto>> Risco(FiltroTickets filtro, CancellationToken ct)
    {
        var (dados, _, _) = await Dados(filtro, ct, true); var agora = Agora;
        return dados.Select(t => (Ticket: t, Sla: sla.Calcular(t, agora))).Where(x => EmRisco(x.Ticket, x.Sla)).OrderBy(x => Restante(x.Sla)).Select(x => tickets.Mapear(x.Ticket, agora)).ToList();
    }
    public async Task<IndicadoresSlaDto> Indicadores(FiltroTickets filtro, CancellationToken ct)
    {
        var (dados, _, _) = await Dados(filtro, ct, true); var agora = Agora;
        var medicoes = dados.Select(t => (Ticket: t, Sla: sla.Calcular(t, agora))).ToList();
        var cumpridos = medicoes.Count(x => x.Ticket.Encerrado && !Violado(x.Ticket, x.Sla));
        var violados = medicoes.Count(x => Violado(x.Ticket, x.Sla));
        var risco = medicoes.Count(x => EmRisco(x.Ticket, x.Sla));
        var finalizados = medicoes.Count(x => x.Ticket.Encerrado);
        var fuso = TimeZoneInfo.FindSystemTimeZoneById((await sla.Obter(ct)).Horario.FusoHorario);
        DateOnly Semana(DateTime data) { var d = DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(data, DateTimeKind.Utc), fuso)); return d.AddDays(-((int)d.DayOfWeek + 6) % 7); }
        var evolucao = medicoes.Where(x => x.Ticket.Encerrado).GroupBy(x => Semana(x.Ticket.ResolvidoEm!.Value)).OrderBy(g => g.Key)
            .Select(g => { var c = g.Count(x => !Violado(x.Ticket, x.Sla)); return new EvolucaoSlaDto(g.Key, c, g.Count() - c, 100d * c / g.Count()); }).ToList();
        return new(cumpridos, violados, risco, medicoes.Count - cumpridos - violados - risco, finalizados == 0 ? null : 100d * cumpridos / finalizados, evolucao);
    }
    public async Task<IReadOnlyList<ViolacaoDto>> Violacoes(FiltroTickets filtro, CancellationToken ct)
    {
        var (dados, _, _) = await Dados(filtro, ct, true); var agora = Agora;
        return dados.Select(t => (Ticket: t, Sla: sla.Calcular(t, agora))).Where(x => Violado(x.Ticket, x.Sla)).OrderBy(x => Restante(x.Sla)).Select(x => {
            var motivos = new List<string>();
            if (x.Sla.PrimeiraResposta.Situacao == SituacaoSla.Violado) motivos.Add("Prazo de primeira resposta excedido");
            if (x.Sla.Resolucao.Situacao == SituacaoSla.Violado) motivos.Add("Prazo de resolução excedido");
            if (x.Ticket.Encerrado && !x.Ticket.PrimeiraRespostaEm.HasValue) motivos.Add("Encerrado sem primeira resposta");
            return new ViolacaoDto(tickets.Mapear(x.Ticket, agora), motivos);
        }).ToList();
    }
    public async Task<IReadOnlyList<RankingDto>> Ranking(FiltroTickets filtro, CancellationToken ct)
    {
        var (dados, inicio, fim) = await Dados(filtro, ct);
        var tecnicos = (await repository.Tecnicos(ct)).Where(t => (!filtro.TecnicoId.HasValue || t.Id == filtro.TecnicoId) && (!filtro.EquipeId.HasValue || t.EquipeId == filtro.EquipeId));
        return tecnicos.Select(t => {
            var resolvidos = dados.Where(x => x.TecnicoId == t.Id && Dentro(x.ResolvidoEm, inicio, fim)).ToList();
            return new RankingDto(new(t.Id, t.Nome), new(t.EquipeId, t.Equipe.Nome), resolvidos.Count,
                Media(resolvidos.Select(x => sla.Calcular(x, fim).Resolucao.ConsumidoMinutos)), Media(resolvidos.Where(x => x.Csat.HasValue).Select(x => (double)x.Csat!.Value)),
                resolvidos.Count == 0 ? null : 100d * resolvidos.Count(x => !Violado(x, sla.Calcular(x, fim))) / resolvidos.Count);
        }).OrderByDescending(x => x.Resolvidos).ThenBy(x => x.Tecnico.Nome).ToList();
    }
    public async Task<IReadOnlyList<CargaDto>> Carga(FiltroTickets filtro, CancellationToken ct)
    {
        var (dados, _, _) = await Dados(filtro, ct, true);
        return (await repository.Tecnicos(ct)).Where(t => (!filtro.TecnicoId.HasValue || t.Id == filtro.TecnicoId) && (!filtro.EquipeId.HasValue || t.EquipeId == filtro.EquipeId)).Select(t => {
            var ativos = dados.Where(x => x.TecnicoId == t.Id && !x.Encerrado).ToList();
            return new CargaDto(new(t.Id, t.Nome), new(t.EquipeId, t.Equipe.Nome), ativos.Count(x => x.Status == StatusTicket.Aberto), ativos.Count(x => x.Status == StatusTicket.EmAndamento), ativos.Count(x => x.Status == StatusTicket.AguardandoCliente), ativos.Count, ativos.Count > 10);
        }).OrderByDescending(x => x.Total).ToList();
    }
    public async Task<IReadOnlyList<ClienteDto>> Clientes(CancellationToken ct)
    {
        var dados = await repository.ListarTickets(new(), ct);
        return (await repository.Clientes(ct)).OrderBy(c => c.Nome).Select(c => {
            var historico = dados.Where(t => t.ClienteId == c.Id).ToList();
            return new ClienteDto(c.Id, c.Nome, c.Email, historico.Count, historico.Count == 0 ? null : historico.Max(t => t.AtualizadoEm), Media(historico.Where(t => t.Csat.HasValue).Select(t => (double)t.Csat!.Value)));
        }).ToList();
    }
    public async Task<PaginaDto<TicketDto>> TicketsCliente(int id, FiltroTickets filtro, CancellationToken ct)
    {
        if (!(await repository.Clientes(ct)).Any(c => c.Id == id)) throw new NaoEncontradoException("Cliente não encontrado.");
        filtro.ClienteId = id; return await tickets.Listar(filtro, ct);
    }
}
