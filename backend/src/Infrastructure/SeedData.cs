using Application;
using Domain;
using Microsoft.EntityFrameworkCore;
namespace Infrastructure;
public static class SeedData
{
    public static async Task Inicializar(AtendimentoDbContext db, HorarioComercial horario, bool exemplos, CancellationToken ct = default)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        if (!await db.RegrasSla.AnyAsync(ct))
        {
            var resposta = new[] { 30, 60, 120, 240 }; var resolucao = new[] { 240, 480, 960, 1800 };
            foreach (var p in Enum.GetValues<Prioridade>()) db.RegrasSla.Add(new RegraSla { Prioridade = p, PrimeiraRespostaMinutos = resposta[(int)p], ResolucaoMinutos = resolucao[(int)p] });
        }
        if (!await db.ConfiguracoesSla.AnyAsync(ct)) db.ConfiguracoesSla.Add(new ConfiguracaoSla { HorarioJson = SlaService.SerializarHorario(horario) });
        await db.SaveChangesAsync(ct);
        if (!exemplos || await db.Tickets.AnyAsync(ct)) { await transaction.CommitAsync(ct); return; }
        if (await db.Clientes.AnyAsync(ct) || await db.Tecnicos.AnyAsync(ct)) throw new InvalidOperationException("Seed de exemplo exige base sem clientes e técnicos.");
        var equipes = new[] { "Suporte ERP", "Infraestrutura", "Integrações e BI" }.Select(n => new Equipe { Nome = n }).ToArray();
        db.Equipes.AddRange(equipes);
        string[] nomes = ["Ana Beatriz Costa", "Bruno Oliveira", "Carla Fernandes", "Diego Santos", "Eduarda Lima", "Felipe Rocha", "Gabriela Almeida", "Henrique Souza", "Isabela Martins", "João Pedro Silva"];
        var tecnicos = nomes.Select((n, i) => new Tecnico { Nome = n, Equipe = equipes[i % 3] }).ToArray(); db.Tecnicos.AddRange(tecnicos);
        string[] empresas = ["Mercadinho São José", "Farmácia Boa Saúde", "Autopeças Nordeste", "Padaria Pão Quente", "Retífica Mafra", "Loja Casa Bela", "Supermercado União", "Distribuidora Potiguar", "Agropecuária Sertão", "Restaurante Sabor da Terra", "Clínica Vida", "Papelaria Escolar", "Construções Oliveira", "Moda Estação", "Eletrônica Central", "Pet Shop Amigo Fiel", "Açougue Bom Corte", "Ótica Visão Clara", "Móveis Fortaleza", "Atacado Alvorada", "Oficina Nova Era", "Café do Centro", "Bazar Primavera", "Transportes Horizonte", "Confeitaria Doce Lar", "Tecidos Santa Luzia", "Livraria Saber", "Hotel Brisa", "Materiais São Paulo", "Informática Soluções"];
        var clientes = empresas.Select((n, i) => new Cliente { Nome = n, Email = $"contato{i + 1:00}@exemplo.com.br" }).ToArray(); db.Clientes.AddRange(clientes);
        await db.SaveChangesAsync(ct);
        var regras = await db.RegrasSla.OrderBy(r => r.Prioridade).ToListAsync(ct);
        string[] assuntos = ["Rejeição na emissão de NF-e", "Divergência no saldo de estoque", "Impressora não imprime cupom", "Erro ao exportar relatório de vendas", "Lentidão no fechamento do caixa", "Integração de pedidos não atualiza", "Falha no backup do banco de dados", "Permissão para novo usuário", "Dashboard de atendimentos desatualizado", "Importação de produtos duplicados", "Certificado digital não reconhecido", "Conciliação de contas a receber"];
        var random = new Random(42); var agora = DateTime.UtcNow; var fuso = TimeZoneInfo.FindSystemTimeZoneById(horario.FusoHorario);
        var hojeLocal = TimeZoneInfo.ConvertTimeFromUtc(agora, fuso).Date;
        for (var i = 0; i < 240; i++)
        {
            var tecnico = tecnicos[i % 10]; var regra = regras[i % 4];
            var local = hojeLocal.AddDays(-(i % 60)).AddHours(8 + random.Next(8)).AddMinutes(random.Next(60));
            var criado = TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(local, DateTimeKind.Unspecified), fuso);
            if (criado > agora) criado = agora.AddMinutes(-random.Next(1, 120));
            var t = new Ticket { Assunto = assuntos[i % assuntos.Length], Descricao = $"Cliente {clientes[i % 30].Nome} relata dificuldade na operação. Solicitada análise e orientação da equipe de suporte.",
                Cliente = clientes[i % 30], Tecnico = tecnico, Equipe = tecnico.Equipe, Prioridade = regra.Prioridade, Canal = (Canal)(i % 4),
                CriadoEm = criado, AtualizadoEm = criado, Status = StatusTicket.Aberto, PrazoPrimeiraRespostaMinutos = regra.PrimeiraRespostaMinutos,
                PrazoResolucaoMinutos = regra.ResolucaoMinutos, HorarioSlaJson = SlaService.SerializarHorario(horario) };
            t.Historico.Add(new HistoricoTicket { Campo = "Criacao", Novo = "Aberto", CriadoEm = criado });
            var statusFinal = (StatusTicket)(i % 5);
            var resposta = criado.AddMinutes(i % 7 == 0 ? 900 : random.Next(5, 90));
            if (resposta < agora && statusFinal != StatusTicket.Aberto)
            {
                t.PrimeiraRespostaEm = resposta;
                t.Mensagens.Add(new Mensagem { Tecnico = tecnico, Conteudo = "Olá! Recebemos sua solicitação e estamos verificando os detalhes informados.", CriadoEm = resposta });
                t.AlterarStatus(StatusTicket.EmAndamento, resposta);
                var nota = resposta.AddMinutes(15);
                if (nota < agora) t.Mensagens.Add(new Mensagem { Tecnico = tecnico, NotaInterna = true, Conteudo = "Análise inicial concluída. Verificar parâmetros do sistema e logs da operação.", CriadoEm = nota });
                if (statusFinal == StatusTicket.AguardandoCliente)
                {
                    var espera = resposta.AddHours(1); if (espera < agora) t.AlterarStatus(StatusTicket.AguardandoCliente, espera);
                }
                else if (statusFinal is StatusTicket.Resolvido or StatusTicket.Fechado)
                {
                    var resolvido = resposta.AddHours(i % 6 == 0 ? 96 : random.Next(1, 16));
                    if (resolvido < agora)
                    {
                        if (i % 3 == 0)
                        {
                            t.AlterarStatus(StatusTicket.AguardandoCliente, resposta.AddMinutes(30));
                            t.AlterarStatus(StatusTicket.EmAndamento, resposta.AddMinutes(50));
                        }
                        t.Mensagens.Add(new Mensagem { Tecnico = tecnico, Conteudo = "Ajuste realizado e funcionamento validado com o cliente. Orientações enviadas.", CriadoEm = resolvido });
                        t.AlterarStatus(statusFinal, resolvido); t.Csat = random.Next(3, 6);
                    }
                }
            }
            if (t.Mensagens.Count == 0) t.Mensagens.Add(new Mensagem { Tecnico = tecnico, NotaInterna = true, Conteudo = "Chamado registrado. Aguardando triagem da equipe.", CriadoEm = criado });
            t.AtualizadoEm = t.Mensagens.Select(m => m.CriadoEm).Append(t.AtualizadoEm).Max();
            db.Tickets.Add(t);
        }
        await db.SaveChangesAsync(ct); await transaction.CommitAsync(ct);
    }
}
