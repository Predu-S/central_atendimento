using Application;
using Microsoft.AspNetCore.Mvc;
namespace Api;
[ApiController, Route("api/tickets")]
public sealed class TicketsController(TicketService service) : ControllerBase
{
    [HttpGet] public Task<PaginaDto<TicketDto>> Listar([FromQuery] FiltroTickets filtro, CancellationToken ct) => service.Listar(filtro, ct);
    [HttpGet("{id:int}")] public Task<TicketDetalheDto> Detalhe(int id, CancellationToken ct) => service.Detalhe(id, ct);
    [HttpPost] public async Task<ActionResult<TicketDetalheDto>> Criar(TicketInput input, CancellationToken ct) { var ticket = await service.Criar(input, ct); return CreatedAtAction(nameof(Detalhe), new { id = ticket.Ticket.Id }, ticket); }
    [HttpPut("{id:int}")] public Task<TicketDetalheDto> Atualizar(int id, TicketInput input, CancellationToken ct) => service.Atualizar(id, input, ct);
    [HttpPatch("lote")] public Task<IReadOnlyList<TicketDto>> Lote(LoteInput input, CancellationToken ct) => service.Lote(input, ct);
    [HttpPost("{id:int}/mensagens")] public async Task<ActionResult<MensagemDto>> Mensagem(int id, MensagemInput input, CancellationToken ct) { var mensagem = await service.Mensagem(id, input, ct); return CreatedAtAction(nameof(Detalhe), new { id }, mensagem); }
}
[ApiController, Route("api/dashboard")]
public sealed class DashboardController(AnalyticsService service) : ControllerBase
{
    [HttpGet("resumo")] public Task<ResumoDto> Resumo([FromQuery] FiltroTickets filtro, CancellationToken ct) => service.Resumo(filtro, ct);
    [HttpGet("series")] public Task<SeriesDto> Series([FromQuery] FiltroTickets filtro, CancellationToken ct) => service.Series(filtro, ct);
    [HttpGet("sla-em-risco")] public Task<IReadOnlyList<TicketDto>> Risco([FromQuery] FiltroTickets filtro, CancellationToken ct) => service.Risco(filtro, ct);
}
[ApiController, Route("api/sla")]
public sealed class SlaController(SlaService service, AnalyticsService analytics) : ControllerBase
{
    [HttpGet("regras")] public Task<ConfiguracaoSlaDto> Regras(CancellationToken ct) => service.Obter(ct);
    [HttpPut("regras")] public Task<ConfiguracaoSlaDto> Atualizar(ConfiguracaoSlaDto input, CancellationToken ct) => service.Atualizar(input, ct);
    [HttpGet("indicadores")] public Task<IndicadoresSlaDto> Indicadores([FromQuery] FiltroTickets filtro, CancellationToken ct) => analytics.Indicadores(filtro, ct);
    [HttpGet("violacoes")] public Task<IReadOnlyList<ViolacaoDto>> Violacoes([FromQuery] FiltroTickets filtro, CancellationToken ct) => analytics.Violacoes(filtro, ct);
}
[ApiController, Route("api/equipe")]
public sealed class EquipeController(AnalyticsService service) : ControllerBase
{
    [HttpGet("ranking")] public Task<IReadOnlyList<RankingDto>> Ranking([FromQuery] FiltroTickets filtro, CancellationToken ct) => service.Ranking(filtro, ct);
    [HttpGet("carga")] public Task<IReadOnlyList<CargaDto>> Carga([FromQuery] FiltroTickets filtro, CancellationToken ct) => service.Carga(filtro, ct);
}
[ApiController, Route("api/clientes")]
public sealed class ClientesController(AnalyticsService service) : ControllerBase
{
    [HttpGet] public Task<IReadOnlyList<ClienteDto>> Clientes(CancellationToken ct) => service.Clientes(ct);
    [HttpGet("{id:int}/tickets")] public Task<PaginaDto<TicketDto>> Tickets(int id, [FromQuery] FiltroTickets filtro, CancellationToken ct) => service.TicketsCliente(id, filtro, ct);
}
