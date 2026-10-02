# Backend — Central de Atendimento

API de chamados e SLA em .NET 8, com camadas Api, Application, Domain e Infrastructure. Swagger, EF Core, FluentValidation e xUnit. O frontend pode consumir a API a partir de `http://localhost:5173`.

## Rodar com SQLite (desenvolvimento)

Pré-requisitos: SDK .NET 8 e acesso ao NuGet.

```bash
cd backend
dotnet restore Atendimento.sln
dotnet tool restore
dotnet build Atendimento.sln
dotnet test Atendimento.sln
dotnet run --project src/Api
```

- API: http://localhost:5080
- Swagger: http://localhost:5080/swagger
- O perfil de execução ativa `Development`, aplica migrations e popula a base automaticamente.
- O arquivo `atendimento.db` fica no diretório de execução. Para reiniciar os exemplos, pare a API e exclua esse arquivo e seus arquivos `-shm`/`-wal`, se presentes.
- Seed determinístico com 240 tickets nos últimos 60 dias, 30 clientes, 10 técnicos e 3 equipes. Inclui mensagens, notas internas, histórico, pausas, avaliações e violações. A segunda inicialização não duplica os dados.

Para aplicar migrations SQLite manualmente (a API também as aplica em desenvolvimento):

```bash
dotnet ef database update --project src/Infrastructure --context SqliteAtendimentoDbContext
```

## SQL Server

`appsettings.json` usa SQL Server; `appsettings.Development.json` troca para SQLite. Ajuste `Database:Provider` e a connection string por configuração ou variáveis de ambiente. Não use a senha de exemplo fora do ambiente local.

Linux/macOS:

```bash
export Database__Provider=SqlServer
export ConnectionStrings__SqlServer='Server=localhost,1433;Database=Atendimento;User Id=sa;Password=SUA_SENHA;TrustServerCertificate=True'
dotnet ef database update --project src/Infrastructure --context AtendimentoDbContext
dotnet run --project src/Api
```

PowerShell:

```powershell
$env:Database__Provider = 'SqlServer'
$env:ConnectionStrings__SqlServer = 'Server=localhost,1433;Database=Atendimento;User Id=sa;Password=SUA_SENHA;TrustServerCertificate=True'
dotnet ef database update --project src/Infrastructure --context AtendimentoDbContext
dotnet run --project src/Api
```

Migrations e snapshots são independentes por provider, em `Infrastructure/Migrations/SqlServer` e `Infrastructure/Migrations/Sqlite`. Após alterar o modelo, gere migrations para ambos:

```bash
dotnet ef migrations add NomeDaAlteracaoSqlServer --project src/Infrastructure --context AtendimentoDbContext --output-dir Migrations/SqlServer
dotnet ef migrations add NomeDaAlteracaoSqlite --project src/Infrastructure --context SqliteAtendimentoDbContext --output-dir Migrations/Sqlite
```

Para desligar os exemplos, defina `Database:Seed=false`: a inicialização em desenvolvimento ainda cria as quatro regras de SLA e o horário comercial. Em produção não há migrations, seed automático nem Swagger; aplique as migrations previamente e inicialize as regras uma vez, usando `Development` com `Database:Seed=false` na base SQL Server. SQLite é restrito a desenvolvimento/testes.

## Contrato e filtros

Enums são serializados como strings. Todas as datas de entrada e saída são UTC (`2026-09-01T00:00:00Z`). `inicio` é inclusivo e `fim` exclusivo. Datas de série diária usam o fuso comercial. O período padrão dos relatórios é de 30 dias até agora; intervalo máximo de 366 dias. Tickets sem período consultam todo o histórico.

Filtros combináveis: `inicio`, `fim`, `equipeId`, `tecnicoId`, `prioridade`, `canal`, `status`, `clienteId`, `busca`. Paginação de tickets: `pagina` (1), `tamanhoPagina` (20, máximo 100), `ordenarPor` (`criadoEm`, `atualizadoEm`, `prioridade`, `status`, `assunto`, `id`), `descendente` (true). Busca textual no assunto, descrição e nome do cliente, ou pelo ID exato. Filtros, ordenação e paginação de tickets são executados no banco; ID desempata a ordenação.

```text
GET /api/tickets?pagina=1&tamanhoPagina=20&status=Aberto&prioridade=Alta&ordenarPor=criadoEm&descendente=true
GET /api/dashboard/resumo?inicio=2026-09-01T00:00:00Z&fim=2026-10-01T00:00:00Z&equipeId=1&canal=WhatsApp
```

| Método | Endpoint |
|---|---|
| GET | `/api/dashboard/resumo`, `/api/dashboard/series`, `/api/dashboard/sla-em-risco` |
| GET | `/api/tickets`, `/api/tickets/{id}` |
| POST / PUT | `/api/tickets` / `/api/tickets/{id}` |
| PATCH | `/api/tickets/lote` |
| POST | `/api/tickets/{id}/mensagens` |
| GET / PUT | `/api/sla/regras` |
| GET | `/api/sla/indicadores`, `/api/sla/violacoes` |
| GET | `/api/equipe/ranking`, `/api/equipe/carga` |
| GET | `/api/clientes`, `/api/clientes/{id}/tickets` |

### Criar / atualizar ticket

POST exige `status=Aberto`. PUT substitui os campos editáveis; valores nulos de técnico/equipe removem a atribuição. Equipe omitida é inferida do técnico. Combinações incoerentes são rejeitadas. CSAT (1–5) só é aceito em tickets resolvidos/fechados.

```json
{
  "assunto": "Erro na emissão de NF-e",
  "descricao": "Certificado digital não reconhecido ao transmitir a nota.",
  "clienteId": 1,
  "tecnicoId": 1,
  "equipeId": null,
  "prioridade": "Alta",
  "canal": "WhatsApp",
  "status": "Aberto",
  "csat": null
}
```

### Alteração em lote e mensagens

Até 100 IDs distintos. Os campos omitidos no lote permanecem como estão. A atribuição de técnico também atribui sua equipe. IDs/referências inválidos impedem a gravação de todo o lote.

```json
{ "ids": [1, 2], "tecnicoId": 3, "status": "EmAndamento", "prioridade": "Alta" }
```

```json
{ "conteudo": "Olá! Vamos verificar os parâmetros fiscais.", "tecnicoId": 1, "notaInterna": false }
```

Notas internas não registram primeira resposta. A primeira mensagem pública registra a primeira resposta uma única vez e move tickets abertos para `EmAndamento`. Tickets encerrados exigem reabertura para nova resposta pública. O detalhe retorna dados do ticket, e-mail do cliente e timeline ordenada de mensagens e alterações.

### Editar regras de SLA

GET/PUT `/api/sla/regras` incluem as quatro prioridades e o horário. Dias usam nomes de `DayOfWeek`. O intervalo deve iniciar e terminar no mesmo dia, sem atravessar a meia-noite.

```json
{
  "regras": [
    { "prioridade": "Critica", "primeiraRespostaMinutos": 30, "resolucaoMinutos": 240 },
    { "prioridade": "Alta", "primeiraRespostaMinutos": 60, "resolucaoMinutos": 480 },
    { "prioridade": "Media", "primeiraRespostaMinutos": 120, "resolucaoMinutos": 960 },
    { "prioridade": "Baixa", "primeiraRespostaMinutos": 240, "resolucaoMinutos": 1800 }
  ],
  "horario": {
    "dias": ["Monday", "Tuesday", "Wednesday", "Thursday", "Friday"],
    "inicio": "08:00:00",
    "fim": "18:00:00",
    "fusoHorario": "America/Fortaleza"
  }
}
```

## Convenções de SLA e indicadores

- O orçamento de primeira resposta e resolução parte da abertura; ambos contam apenas minutos úteis e descontam a união dos intervalos de pausa.
- `AguardandoCliente` pausa ambos os relógios. Resposta pública conclui o primeiro; resolução/fechamento conclui o segundo. Reabertura conserva o consumo anterior e desconta o intervalo encerrado.
- Menos de 20% restante: `EmRisco`; exatamente 20%: `NoPrazo`. Pendência com prazo esgotado: `Violado`. Entrega exatamente no limite cumpre o prazo; entrega após o limite viola.
- Tempo restante pode ser negativo e percentual consumido pode ultrapassar 100%. O DTO informa cada relógio separadamente e se está concluído/pausado.
- Regras e horário são copiados para o ticket na criação. Alterações globais afetam novos tickets. Alterar prioridade de um ticket aberto atualiza a meta de resolução e a primeira resposta ainda pendente; conserva o consumo e o horário originais. Reabra antes de alterar prioridade de um ticket encerrado.
- Cumprimento exige primeira resposta e resolução dentro da meta; encerramento sem resposta conta como violação. CSAT é média das notas 1–5; ausência de avaliações retorna `null`.
- Resumo: estoque de status ao fim do período, reconstruído por histórico; comparação com o estoque ao início. Médias, CSAT e cumprimento usam eventos de resposta/resolução ocorridos no período, inclusive tickets criados antes dele. O período anterior tem a mesma duração. `resolvidosHoje` representa o último dia local do período e compara com o último dia local do período anterior. Variação é percentual; referência nula/zero retorna `null`.
- Séries: abertura e resolução são contadas pela data do evento. Volume por canal/prioridade/técnico usa tickets criados no período; dias sem eventos aparecem com zero.
- Risco, violações, indicadores e carga usam tickets criados no período e calculam a situação atual. Evolução semanal agrupa resoluções por segunda-feira, com cumprimento calculado apenas nos encerrados. `violados` pode incluir tickets ainda ativos; percentual cumprido usa só encerrados.
- Ranking usa resoluções no período; carga mostra ativos por técnico. Sobrecarga significa mais de 10 tickets ativos. Dimensões como técnico/equipe/prioridade/canal refletem a atribuição atual mesmo em consultas históricas.

## Erros e validação

Validação de payload/query retorna 400; recurso inexistente retorna 404; conflito de regra de negócio retorna 409; falha inesperada retorna 500. Erros usam `ProblemDetails`, com `traceId` e, quando aplicável, `errors` por campo. Logs registram método, caminho, status e duração, sem conteúdo das mensagens.

A API entregue não inclui autenticação/autorização: `tecnicoId` é uma referência do domínio enviada pelo cliente. Autenticação e políticas de acesso devem ser adicionadas antes de disponibilizar dados reais publicamente.

## Validação automatizada

O workflow `.github/workflows/backend.yml` executa restore, build Release, xUnit, script de migrations SQL Server e smoke HTTP com SQLite e SQL Server 2022. Os testes verificam limites de SLA, noites/fins de semana, pausas sobrepostas, fuso, reabertura, migrations, seed idempotente, paginação/filtros, notas internas e respostas. O smoke roda nos dois providers e percorre os endpoints, CRUD, validação, lote e CORS. Resultados TRX e logs ficam no artifact `backend-validation`.
