using Application;
using Domain;
using FluentValidation;
using Infrastructure;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;
namespace Domain.Tests;
public class PersistenceTests
{
    [Fact] public async Task MigrationsSqliteSeedEPaginacaoFuncionam()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:"); await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<SqliteAtendimentoDbContext>().UseSqlite(connection).Options;
        await using var db = new SqliteAtendimentoDbContext(options);
        await db.Database.MigrateAsync(); await SeedData.Inicializar(db, new(), true);
        Assert.Equal(240, await db.Tickets.CountAsync()); Assert.Equal(30, await db.Clientes.CountAsync()); Assert.Equal(10, await db.Tecnicos.CountAsync()); Assert.Equal(3, await db.Equipes.CountAsync());
        await SeedData.Inicializar(db, new(), true); Assert.Equal(240, await db.Tickets.CountAsync());
        var repository = new AtendimentoRepository(db);
        var pagina = await repository.PaginarTickets(new() { Pagina = 2, TamanhoPagina = 10, OrdenarPor = "id", Descendente = false }, default);
        Assert.Equal(240, pagina.Total); Assert.Equal(10, pagina.Itens.Count); Assert.Equal(11, pagina.Itens[0].Id);
        var f = new FiltroTickets { Status = StatusTicket.AguardandoCliente, Prioridade = Prioridade.Alta, Canal = Canal.Email };
        var filtrados = await repository.ListarTickets(f, default); Assert.NotEmpty(filtrados);
        Assert.All(filtrados, t => { Assert.Equal(f.Status, t.Status); Assert.Equal(f.Prioridade, t.Prioridade); Assert.Equal(f.Canal, t.Canal); });
    }
    [Fact] public async Task NotaInternaNaoMarcaPrimeiraRespostaERespostaPublicaMarca()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:"); await connection.OpenAsync();
        await using var db = new SqliteAtendimentoDbContext(new DbContextOptionsBuilder<SqliteAtendimentoDbContext>().UseSqlite(connection).Options);
        await db.Database.MigrateAsync(); await SeedData.Inicializar(db, new(), true);
        var repository = new AtendimentoRepository(db);
        var sla = new SlaService(repository, new(), new ConfiguracaoValidator());
        var service = new TicketService(repository, sla, TimeProvider.System, new TicketValidator(), new MensagemValidator(), new LoteValidator(), new FiltroValidator());
        var ticket = await service.Criar(new("Problema no caixa", "Erro ao finalizar venda", 1, 1, null, Prioridade.Alta, Canal.Chat), default);
        await service.Mensagem(ticket.Ticket.Id, new("Verificar log", 1, true), default);
        Assert.Null((await service.Detalhe(ticket.Ticket.Id, default)).Ticket.PrimeiraRespostaEm);
        await service.Mensagem(ticket.Ticket.Id, new("Olá, vamos ajudar", 1), default);
        var detalhado = await service.Detalhe(ticket.Ticket.Id, default);
        Assert.NotNull(detalhado.Ticket.PrimeiraRespostaEm); Assert.Equal(StatusTicket.EmAndamento, detalhado.Ticket.Status);
        var primeira = detalhado.Ticket.PrimeiraRespostaEm;
        await service.Mensagem(ticket.Ticket.Id, new("Mais detalhes", 1), default);
        Assert.Equal(primeira, (await service.Detalhe(ticket.Ticket.Id, default)).Ticket.PrimeiraRespostaEm);
        await Assert.ThrowsAsync<NaoEncontradoException>(() => service.Lote(new([ticket.Ticket.Id, 99999], null, StatusTicket.Fechado, null), default));
        Assert.Equal(StatusTicket.EmAndamento, (await service.Detalhe(ticket.Ticket.Id, default)).Ticket.Status);
    }
    [Fact] public void ValidaConfiguracaoDeExpediente()
    {
        var validator = new ConfiguracaoValidator();
        var regras = Enum.GetValues<Prioridade>().Select(p => new RegraSlaDto(p, 30, 240)).ToList();
        Assert.True(validator.Validate(
    new ConfiguracaoSlaDto(regras, new())).IsValid);

Assert.False(validator.Validate(
    new ConfiguracaoSlaDto(regras, new() { Dias = [] })).IsValid);

Assert.False(validator.Validate(
    new ConfiguracaoSlaDto(regras, new() { Fim = new(7, 0) })).IsValid);

Assert.False(validator.Validate(
    new ConfiguracaoSlaDto(regras, new() { FusoHorario = "inexistente" })).IsValid);
    }
}
