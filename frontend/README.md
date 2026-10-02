# Frontend — Central de Atendimento

Dashboard de suporte em React + TypeScript (Vite), Tailwind CSS, React Router, TanStack Query e Recharts. Interface em português (BR), responsiva e com tema claro/escuro persistido no navegador.

## Executar

Pré-requisitos: Node.js 22.12+ ou 24, npm e a API .NET em execução.

```bash
cd frontend
npm install
cp .env.example .env
npm run dev
```

PowerShell: use `Copy-Item .env.example .env` no lugar de `cp`.

Abra http://localhost:5173. Essa porta é fixa para corresponder ao CORS do backend. Se estiver ocupada, libere-a antes de iniciar.

### URL da API

O padrão solicitado é `http://localhost:5000`. Defina a origem do backend em `.env`, sem sufixo `/api`:

```env
VITE_API_BASE_URL=http://localhost:5000
```

O perfil de execução já entregue no backend usa **5080**. Para usá-lo, altere o frontend para `VITE_API_BASE_URL=http://localhost:5080`, ou execute o backend na porta 5000:

```bash
cd backend
dotnet restore
dotnet run --project src/Api --urls http://localhost:5000
```

Reinicie o Vite depois de alterar `.env`. O frontend mostra erro de conexão e botão de tentar novamente quando o backend não está disponível; não substitui os dados por dados fictícios.

**Dependência:** o PR #2 do backend contém as migrations e as correções de inicialização necessárias. Integre-o antes de executar o conjunto a partir de `main`. O workflow do frontend usa o commit validado `8519e4a3038cced51d332921e661ae0e16604178` dessa versão como contrato da integração.

## Estrutura

- `src/components`: layout, filtros, tabelas, formulário/modal de ticket, drawer, estados e componentes de SLA.
- `src/pages`: Visão Geral, Tickets, SLA, Equipe, Clientes e Relatórios.
- `src/hooks`: queries/mutations, filtros persistidos na URL e relógio de SLA.
- `src/services`: cliente HTTP, tratamento de ProblemDetails, formatação e CSV.
- `src/types`: interfaces dos DTOs, requests e enums da API.

Os contratos seguem `backend/src/Application/Contracts.cs`, `Domain/SlaCalculator.cs` e `Api/Controllers.cs`. O teste de integração também consulta o Swagger gerado e verifica os schemas esperados. Equipes e técnicos são obtidos de `/api/equipe/ranking`, que retorna todos os técnicos, inclusive aqueles sem resoluções no período.

## Funcionalidades

- Visão Geral: oito KPIs com variação, quatro gráficos, filtros de período/equipe/técnico/prioridade/canal e lista de SLA em risco.
- Tickets: busca, filtros combináveis, ordenação e paginação no servidor, seleção da página e lote de até 100 tickets. Criação e edição respeitam a validação do backend.
- Kanban: arrastar e soltar entre os cinco status; seletor de status em cada card como alternativa por teclado/toque. Carrega 100 tickets por vez, com botão para carregar as próximas páginas e total visível.
- Drawer: cliente, técnico, dois relógios de SLA, timeline de mensagens/notas/histórico, edição, transferência, alteração de status e encerramento. Respostas exigem autor; tickets encerrados exigem reabertura para respostas públicas. Notas internas continuam disponíveis.
- SLA: regras por prioridade, dias/horas/fuso de expediente, indicadores, evolução semanal e violações com motivos.
- Equipe: ranking, CSAT, cumprimento e tempo médio, carga por status e indicação de sobrecarga (>10 ativos).
- Clientes: busca, total de chamados, último contato, CSAT e histórico paginado de todo o relacionamento.
- Relatórios: exportação de tickets, violações e ranking em CSV. A exportação de tickets percorre todas as páginas, mostra progresso e pode ser cancelada. CSV usa UTF-8 com BOM e `;`, protege valores textuais contra fórmulas e preserva aspas/quebras de linha.

## Convenções

- Enums enviados à API mantêm seus nomes exatos (`EmAndamento`, `AguardandoCliente`, `Critica`, `Media`); os rótulos visíveis são traduzidos/formados em PT-BR.
- Datas selecionadas são interpretadas no fuso local do navegador e enviadas em UTC, com `inicio` inclusivo e `fim` exclusivo (dia seguinte ao fim escolhido). Período padrão: últimos 30 dias. Período máximo: 366 dias.
- Filtros globais ficam na query string e são preservados entre módulos; filtros exclusivos de tickets ficam nessa tela. Histórico do cliente consulta todos os períodos.
- KPIs exibem `—` para valores sem dados e “Sem base de comparação” para variações com referência nula/zero. Menores tempos, menos abertos e menos aguardando são tratados como melhora; satisfação/cumprimento/resoluções usam crescimento como melhora.
- A API é a fonte do SLA. A lista em risco sincroniza a cada 5 segundos; tickets e drawer sincronizam a cada 15 segundos. Entre leituras, o relógio visual interpola somente segundos do expediente, congela em pausas/conclusões e interrompe a interpolação após 60 segundos sem leitura nova.
- O DTO não expõe o horário histórico de cada ticket. A interpolação curta usa o expediente global atual; a leitura seguinte da API corrige qualquer diferença para tickets com um horário antigo. Valores do servidor continuam preservando o contrato histórico.
- Alterar as regras globais afeta novos tickets, conforme o backend. É necessário reabrir um ticket encerrado antes de alterar sua prioridade.
- Os dados vêm da API; não há login neste escopo. A escolha de autor da mensagem corresponde à referência de técnico exigida pelo backend.

## Validar

```bash
npm run lint
npm run build
```

O build verifica TypeScript estrito e gera `dist/`. As páginas são carregadas por demanda. Para pré-visualizar o build:

```bash
npm run preview -- --port 5173
```

Para hospedar, configure fallback das rotas SPA para `index.html`. A origem hospedada também precisa ser permitida no CORS da API.

### Testes de integração

Com a API em `localhost:5000` e o frontend livre na porta 5173:

```bash
npx playwright install chromium
npm run test:e2e
```

Se a API estiver em outra origem, defina `VITE_API_BASE_URL` tanto para o Vite quanto para os testes. O Playwright inicia o Vite automaticamente.

O workflow `.github/workflows/frontend.yml` executa instalação por lockfile, lint, build e testes Chromium com uma API .NET real e SQLite. Verifica Swagger, telas, filtros/paginação, criação, nota interna/primeira resposta, pausa, transferência/encerramento, lote, drag-and-drop, regras, clientes, exportação completa, temas, layout móvel e estados de erro/vazio. Logs, Swagger, screenshots, traces de falhas e build ficam no artifact `frontend-validation`.
