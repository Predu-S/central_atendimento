using System.Text.Json;
using Domain;
using FluentValidation;
namespace Application;
public sealed class SlaService(IAtendimentoRepository repository, SlaCalculator calculator, IValidator<ConfiguracaoSlaDto> validator)
{
    public static string SerializarHorario(HorarioComercial horario) => JsonSerializer.Serialize(horario);
    public static HorarioComercial LerHorario(string json) => JsonSerializer.Deserialize<HorarioComercial>(json) ?? throw new InvalidOperationException("Horário SLA ausente.");
    public SlaTicket Calcular(Ticket ticket, DateTime agora) => calculator.Calcular(ticket, LerHorario(ticket.HorarioSlaJson), agora);
    public async Task<ConfiguracaoSlaDto> Obter(CancellationToken ct)
    {
        var config = await repository.Configuracao(ct);
        var regras = await repository.Regras(ct);
        if (config is null || regras.Count != 4) throw new InvalidOperationException("Configuração SLA não inicializada. Execute migrations e seed.");
        return new(regras.OrderBy(r => r.Prioridade).Select(r => new RegraSlaDto(r.Prioridade, r.PrimeiraRespostaMinutos, r.ResolucaoMinutos)).ToList(), LerHorario(config.HorarioJson));
    }
    public async Task<ConfiguracaoSlaDto> Atualizar(ConfiguracaoSlaDto input, CancellationToken ct)
    {
        await validator.ValidateAndThrowAsync(input, ct);
        var regras = await repository.Regras(ct);
        foreach (var regra in regras)
        {
            var nova = input.Regras.Single(r => r.Prioridade == regra.Prioridade);
            regra.PrimeiraRespostaMinutos = nova.PrimeiraRespostaMinutos; regra.ResolucaoMinutos = nova.ResolucaoMinutos;
        }
        var config = await repository.Configuracao(ct) ?? throw new InvalidOperationException("Configuração SLA ausente.");
        config.HorarioJson = SerializarHorario(input.Horario);
        await repository.Salvar(ct); return await Obter(ct);
    }
}
