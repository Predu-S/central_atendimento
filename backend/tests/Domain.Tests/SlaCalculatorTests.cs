using Domain;
using Xunit;
namespace Domain.Tests;
public class SlaCalculatorTests
{
    private readonly SlaCalculator calculator = new();
    private readonly HorarioComercial horario = new() { Inicio = new(8, 0), Fim = new(18, 0), FusoHorario = "UTC" };
    private static DateTime Utc(int dia, int hora, int minuto = 0) => new(2026, 9, dia, hora, minuto, 0, DateTimeKind.Utc);
    [Fact] public void ContaApenasExpediente() => Assert.Equal(120, calculator.MinutosUteis(Utc(28, 7), Utc(28, 10), horario));
    [Fact] public void IgnoraNoitesEFimDeSemana() => Assert.Equal(120, calculator.MinutosUteis(Utc(25, 17), Utc(28, 9), horario));
    [Fact] public void IntervaloForaDoExpedienteNaoConta() => Assert.Equal(0, calculator.MinutosUteis(Utc(26, 9), Utc(27, 17), horario));
    [Fact] public void PausaAbertaCongelaTempo() => Assert.Equal(60, calculator.MinutosUteis(Utc(28, 8), Utc(29, 17), horario, [new() { Inicio = Utc(28, 9) }]));
    [Fact] public void PausasSobrepostasNaoDescontamDuasVezes() => Assert.Equal(120, calculator.MinutosUteis(Utc(28, 8), Utc(28, 12), horario, [new() { Inicio = Utc(28, 9), Fim = Utc(28, 10, 30) }, new() { Inicio = Utc(28, 10), Fim = Utc(28, 11) }]));
    [Fact] public void PausaNoturnaNaoDescontaHorarioUtil() => Assert.Equal(120, calculator.MinutosUteis(Utc(25, 17), Utc(28, 9), horario, [new() { Inicio = Utc(25, 18), Fim = Utc(28, 8) }]));
    [Fact] public void PausaQueAbrangeTodoIntervaloZeraConsumo() => Assert.Equal(0, calculator.MinutosUteis(Utc(28, 8), Utc(28, 10), horario, [new() { Inicio = Utc(25, 8), Fim = Utc(29, 12) }]));
    [Fact] public void MultiplasPausasSomenteNoExpediente() => Assert.Equal(120, calculator.MinutosUteis(Utc(28, 8), Utc(28, 13), horario, [new() { Inicio = Utc(28, 8), Fim = Utc(28, 9) }, new() { Inicio = Utc(28, 10), Fim = Utc(28, 12) }]));
    [Fact] public void RespeitaDiasConfigurados()
    {
        var config = horario with { Dias = [DayOfWeek.Sunday] };
        Assert.Equal(600, calculator.MinutosUteis(Utc(26, 8), Utc(28, 18), config));
    }
    [Fact] public void ConverteFusoFortaleza()
    {
        var config = horario with { FusoHorario = "America/Fortaleza" };
        Assert.Equal(60, calculator.MinutosUteis(Utc(28, 10), Utc(28, 12), config));
    }
    [Theory]
    [InlineData(79, false, SituacaoSla.NoPrazo)]
    [InlineData(80, false, SituacaoSla.NoPrazo)]
    [InlineData(81, false, SituacaoSla.EmRisco)]
    [InlineData(100, false, SituacaoSla.Violado)]
    [InlineData(100, true, SituacaoSla.EmRisco)]
    [InlineData(101, true, SituacaoSla.Violado)]
    public void ClassificaLimites(double consumido, bool concluido, SituacaoSla esperado) => Assert.Equal(esperado, SlaCalculator.Medir(100, consumido, concluido).Situacao);
    [Fact] public void PercentualPodeExcederCemERestanteSerNegativo()
    {
        var resultado = SlaCalculator.Medir(100, 150, false);
        Assert.Equal(150, resultado.PercentualConsumido); Assert.Equal(-50, resultado.RestanteMinutos);
    }
    [Fact] public void RespostaEConclusaoCongelamSeusRelogios()
    {
        var t = new Ticket { CriadoEm = Utc(28, 8), PrimeiraRespostaEm = Utc(28, 9), ResolvidoEm = Utc(28, 12), Status = StatusTicket.Resolvido, PrazoPrimeiraRespostaMinutos = 60, PrazoResolucaoMinutos = 240 };
        var resultado = calculator.Calcular(t, horario, Utc(29, 17));
        Assert.Equal(60, resultado.PrimeiraResposta.ConsumidoMinutos); Assert.Equal(240, resultado.Resolucao.ConsumidoMinutos);
        Assert.True(resultado.PrimeiraResposta.Concluido); Assert.True(resultado.Resolucao.Concluido);
    }
    [Fact] public void MudancaDeStatusRegistraPausaEHistorico()
    {
        var t = new Ticket { CriadoEm = Utc(28, 8), PrazoPrimeiraRespostaMinutos = 240, PrazoResolucaoMinutos = 600 };
        t.AlterarStatus(StatusTicket.AguardandoCliente, Utc(28, 9)); t.AlterarStatus(StatusTicket.EmAndamento, Utc(28, 11));
        Assert.Single(t.Pausas); Assert.Equal(2, t.Historico.Count);
        Assert.Equal(120, calculator.Calcular(t, horario, Utc(28, 12)).Resolucao.ConsumidoMinutos);
    }
    [Fact] public void ReaberturaRetomaConsumoOriginalSemContarPeriodoEncerrado()
    {
        var t = new Ticket { CriadoEm = Utc(28, 8), PrazoPrimeiraRespostaMinutos = 240, PrazoResolucaoMinutos = 600 };
        t.AlterarStatus(StatusTicket.Resolvido, Utc(28, 10)); t.AlterarStatus(StatusTicket.EmAndamento, Utc(29, 10));
        Assert.Null(t.ResolvidoEm); Assert.Equal(180, calculator.Calcular(t, horario, Utc(29, 11)).Resolucao.ConsumidoMinutos);
    }
    [Fact] public void RepetirStatusNaoDuplicaPausa()
    {
        var t = new Ticket(); t.AlterarStatus(StatusTicket.AguardandoCliente, Utc(28, 9)); t.AlterarStatus(StatusTicket.AguardandoCliente, Utc(28, 10)); Assert.Single(t.Pausas);
    }
    [Fact] public void IntervaloInvertidoZeraConsumo() => Assert.Equal(0, calculator.MinutosUteis(Utc(28, 10), Utc(28, 8), horario));
}
