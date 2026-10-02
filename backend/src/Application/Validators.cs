using Domain;
using FluentValidation;
namespace Application;
public sealed class FiltroValidator : AbstractValidator<FiltroTickets>
{
    public FiltroValidator()
    {
        RuleFor(x => x.Pagina).GreaterThan(0); RuleFor(x => x.TamanhoPagina).InclusiveBetween(1, 100);
        RuleFor(x => x.OrdenarPor).Must(x => new[] { "criadoEm", "atualizadoEm", "prioridade", "status", "assunto", "id" }.Contains(x));
        RuleFor(x => x).Must(x => !x.Inicio.HasValue || !x.Fim.HasValue || x.Fim >= x.Inicio).WithMessage("Fim deve ser maior ou igual ao início.");
        RuleFor(x => x).Must(x => !x.Inicio.HasValue || !x.Fim.HasValue || (x.Fim.Value - x.Inicio.Value).TotalDays <= 366).WithMessage("Período máximo: 366 dias.");
        RuleFor(x => x.Inicio).Must(x => !x.HasValue || x.Value.Kind == DateTimeKind.Utc).WithMessage("Use data/hora UTC com sufixo Z.");
        RuleFor(x => x.Fim).Must(x => !x.HasValue || x.Value.Kind == DateTimeKind.Utc).WithMessage("Use data/hora UTC com sufixo Z.");
        RuleFor(x => x.Status).IsInEnum().When(x => x.Status.HasValue);
        RuleFor(x => x.Prioridade).IsInEnum().When(x => x.Prioridade.HasValue);
        RuleFor(x => x.Canal).IsInEnum().When(x => x.Canal.HasValue);
        RuleFor(x => x.Busca).MaximumLength(200);
        RuleFor(x => x.TecnicoId).GreaterThan(0).When(x => x.TecnicoId.HasValue);
        RuleFor(x => x.EquipeId).GreaterThan(0).When(x => x.EquipeId.HasValue);
        RuleFor(x => x.ClienteId).GreaterThan(0).When(x => x.ClienteId.HasValue);
    }
}
public sealed class TicketValidator : AbstractValidator<TicketInput>
{
    public TicketValidator()
    {
        RuleFor(x => x.Assunto).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Descricao).NotEmpty().MaximumLength(10000);
        RuleFor(x => x.ClienteId).GreaterThan(0);
        RuleFor(x => x.TecnicoId).GreaterThan(0).When(x => x.TecnicoId.HasValue);
        RuleFor(x => x.EquipeId).GreaterThan(0).When(x => x.EquipeId.HasValue);
        RuleFor(x => x.Status).IsInEnum(); RuleFor(x => x.Prioridade).IsInEnum(); RuleFor(x => x.Canal).IsInEnum();
        RuleFor(x => x.Csat).InclusiveBetween(1, 5).When(x => x.Csat.HasValue);
    }
}
public sealed class MensagemValidator : AbstractValidator<MensagemInput>
{
    public MensagemValidator() { RuleFor(x => x.Conteudo).NotEmpty().MaximumLength(10000); RuleFor(x => x.TecnicoId).GreaterThan(0); }
}
public sealed class LoteValidator : AbstractValidator<LoteInput>
{
    public LoteValidator()
    {
        RuleFor(x => x.Ids).NotEmpty().Must(x => x is not null && x.Length <= 100 && x.All(i => i > 0) && x.Distinct().Count() == x.Length).WithMessage("Informe até 100 IDs positivos e distintos.");
        RuleFor(x => x).Must(x => x.TecnicoId.HasValue || x.Status.HasValue || x.Prioridade.HasValue).WithMessage("Informe uma alteração.");
        RuleFor(x => x.TecnicoId).GreaterThan(0).When(x => x.TecnicoId.HasValue);
        RuleFor(x => x.Status).IsInEnum().When(x => x.Status.HasValue); RuleFor(x => x.Prioridade).IsInEnum().When(x => x.Prioridade.HasValue);
    }
}
public sealed class ConfiguracaoValidator : AbstractValidator<ConfiguracaoSlaDto>
{
    public ConfiguracaoValidator()
    {
        RuleFor(x => x.Regras).NotNull().Must(x => x is not null && x.Count == 4 && x.Select(r => r.Prioridade).Distinct().Count() == 4).WithMessage("Informe as quatro prioridades, sem repetição.");
        RuleForEach(x => x.Regras).ChildRules(r => {
            r.RuleFor(x => x.Prioridade).IsInEnum();
            r.RuleFor(x => x.PrimeiraRespostaMinutos).InclusiveBetween(1, 525600);
            r.RuleFor(x => x.ResolucaoMinutos).InclusiveBetween(1, 525600).GreaterThanOrEqualTo(x => x.PrimeiraRespostaMinutos);
        });
        RuleFor(x => x.Horario).NotNull();
        When(x => x.Horario is not null, () => {
            RuleFor(x => x.Horario.Dias).NotEmpty().Must(d => d is not null && d.All(Enum.IsDefined) && d.Distinct().Count() == d.Length);
            RuleFor(x => x.Horario.Fim).GreaterThan(x => x.Horario.Inicio);
            RuleFor(x => x.Horario.FusoHorario).Must(ExisteFuso).WithMessage("Fuso horário inválido.");
        });
    }
    private static bool ExisteFuso(string id) { try { TimeZoneInfo.FindSystemTimeZoneById(id); return true; } catch (Exception e) when (e is TimeZoneNotFoundException or InvalidTimeZoneException or ArgumentNullException) { return false; } }
}
