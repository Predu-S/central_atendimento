namespace Domain;
public record HorarioComercial
{
    public DayOfWeek[] Dias { get; init; } = [DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday];
    public TimeOnly Inicio { get; init; } = new(8, 0);
    public TimeOnly Fim { get; init; } = new(18, 0);
    public string FusoHorario { get; init; } = "America/Fortaleza";
}
public record MedicaoSla(double PrazoMinutos, double ConsumidoMinutos, double RestanteMinutos, double PercentualConsumido, SituacaoSla Situacao, bool Concluido);
public record SlaTicket(MedicaoSla PrimeiraResposta, MedicaoSla Resolucao, bool Pausado);
public sealed class SlaCalculator
{
    public double MinutosUteis(DateTime inicioUtc, DateTime fimUtc, HorarioComercial horario, IEnumerable<PausaSla>? pausas = null)
    {
        if (fimUtc <= inicioUtc) return 0;
        var fuso = TimeZoneInfo.FindSystemTimeZoneById(horario.FusoHorario);
        var inicio = DateTime.SpecifyKind(inicioUtc, DateTimeKind.Utc);
        var fim = DateTime.SpecifyKind(fimUtc, DateTimeKind.Utc);
        var diaInicial = TimeZoneInfo.ConvertTimeFromUtc(inicio, fuso).Date;
        var diaFinal = TimeZoneInfo.ConvertTimeFromUtc(fim, fuso).Date;
        var intervalos = (pausas ?? []).Select(p => (Inicio: p.Inicio < inicio ? inicio : p.Inicio, Fim: (p.Fim ?? fim) > fim ? fim : p.Fim ?? fim))
            .Where(p => p.Fim > p.Inicio).OrderBy(p => p.Inicio).ToList();
        double minutos = 0;
        for (var dia = diaInicial; dia <= diaFinal; dia = dia.AddDays(1))
        {
            if (!horario.Dias.Contains(dia.DayOfWeek)) continue;
            var abre = TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(dia + horario.Inicio.ToTimeSpan(), DateTimeKind.Unspecified), fuso);
            var fecha = TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(dia + horario.Fim.ToTimeSpan(), DateTimeKind.Unspecified), fuso);
            var a = abre > inicio ? abre : inicio; var b = fecha < fim ? fecha : fim;
            if (b <= a) continue;
            var cursor = a;
            foreach (var pausa in intervalos)
            {
                if (pausa.Fim <= cursor || pausa.Inicio >= b) continue;
                if (pausa.Inicio > cursor) minutos += (pausa.Inicio - cursor).TotalMinutes;
                cursor = pausa.Fim > cursor ? pausa.Fim : cursor;
                if (cursor >= b) break;
            }
            if (cursor < b) minutos += (b - cursor).TotalMinutes;
        }
        return minutos;
    }
    public SlaTicket Calcular(Ticket ticket, HorarioComercial horario, DateTime agora)
    {
        var respostaFim = ticket.PrimeiraRespostaEm ?? ticket.ResolvidoEm ?? agora;
        var resolucaoFim = ticket.ResolvidoEm ?? agora;
        return new(
            Medir(ticket.PrazoPrimeiraRespostaMinutos, MinutosUteis(ticket.CriadoEm, respostaFim, horario, ticket.Pausas), ticket.PrimeiraRespostaEm.HasValue),
            Medir(ticket.PrazoResolucaoMinutos, MinutosUteis(ticket.CriadoEm, resolucaoFim, horario, ticket.Pausas), ticket.ResolvidoEm.HasValue),
            ticket.Status == StatusTicket.AguardandoCliente);
    }
    public static MedicaoSla Medir(double prazo, double consumido, bool concluido)
    {
        if (prazo <= 0) throw new ArgumentOutOfRangeException(nameof(prazo));
        var restante = prazo - consumido;
        // Prazo exatamente atingido: entrega no limite cumpre; pendência no limite viola.
        var situacao = consumido > prazo || (!concluido && consumido >= prazo) ? SituacaoSla.Violado : restante < prazo * .2 ? SituacaoSla.EmRisco : SituacaoSla.NoPrazo;
        return new(prazo, consumido, restante, consumido / prazo * 100, situacao, concluido);
    }
}
