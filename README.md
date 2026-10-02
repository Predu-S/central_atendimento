# Dashboard de Chamados, SLA e Tickets

Especificação do dashboard de gerenciamento de chamados de suporte, com foco em acompanhamento de SLA e gestão de tickets.

- **Backend:** .NET 8 (ASP.NET Core Web API, C#)
- **Frontend:** React + TypeScript
- **Idioma da interface:** português (BR)

---

## 1. Visão geral

O dashboard permite acompanhar o volume de chamados, o cumprimento de SLA, a carga da equipe e o histórico de clientes, além de gerenciar tickets em formato de lista e Kanban.

### Módulos

| Módulo | Objetivo |
|---|---|
| Visão Geral | KPIs, gráficos e chamados prestes a estourar o SLA |
| Tickets | Listagem, filtros, Kanban, detalhe e criação de tickets |
| SLA | Regras, indicadores, evolução e violações |
| Equipe | Ranking e carga de trabalho dos técnicos |
| Clientes | Lista de clientes e histórico de chamados |
| Relatórios | Exportação e relatórios prontos |

---

## 2. Stack

### Backend
- .NET 8, ASP.NET Core Web API, C#
- Entity Framework Core
- Swagger / OpenAPI
- SQL Server (SQLite opcional em desenvolvimento, configurável via `appsettings`)
- FluentValidation
- xUnit (testes do cálculo de SLA)

### Frontend
- React + TypeScript (Vite)
- Tailwind CSS
- React Router
- TanStack Query
- Recharts

---

## 3. Backend

### 3.1 Arquitetura

Camadas em projetos separados, dentro de `backend/`:

| Camada | Responsabilidade |
|---|---|
| Api | Controllers, configuração, CORS, tratamento global de erros |
| Application | Serviços, DTOs, validações |
| Domain | Entidades, enums, regras de negócio |
| Infrastructure | EF Core, repositórios, migrations, seed |

Boas práticas: DTOs separados das entidades, erros em `ProblemDetails`, logs estruturados, CORS liberado para `http://localhost:5173`.

### 3.2 Domínio

**Entidades:** `Ticket`, `Cliente`, `Tecnico`, `Equipe`, `RegraSla`, `Mensagem`, `HistoricoTicket`.

**Enums:**

| Enum | Valores |
|---|---|
| `StatusTicket` | Aberto, EmAndamento, AguardandoCliente, Resolvido, Fechado |
| `Prioridade` | Critica, Alta, Media, Baixa |
| `Canal` | WhatsApp, Email, Telefone, Chat |

### 3.3 Regras de SLA

- Cada prioridade possui uma `RegraSla` com prazo de **primeira resposta** e prazo de **resolução** (em minutos).
- O horário comercial é configurável (dias da semana, hora de início e fim). O tempo só conta dentro dele.
- O status `AguardandoCliente` **pausa** a contagem.
- Cada ticket expõe: tempo restante, percentual consumido e situação.

| Situação | Critério |
|---|---|
| NoPrazo | Mais de 20% do prazo restante |
| EmRisco | Menos de 20% do prazo restante |
| Violado | Prazo esgotado |

### 3.4 Endpoints

#### Dashboard
| Método | Rota | Descrição |
|---|---|---|
| GET | `/api/dashboard/resumo` | KPIs com variação vs. período anterior |
| GET | `/api/dashboard/series` | Abertos x resolvidos por dia; volume por canal, prioridade e técnico |
| GET | `/api/dashboard/sla-em-risco` | Tickets prestes a estourar SLA, ordenados por tempo restante |

Filtros aceitos via query string: período, equipe, técnico, prioridade, canal.

#### Tickets
| Método | Rota | Descrição |
|---|---|---|
| GET | `/api/tickets` | Paginação, busca, filtros (status, prioridade, canal, técnico, equipe, período) e ordenação |
| GET | `/api/tickets/{id}` | Detalhe com cliente, técnico, timeline de mensagens e histórico |
| POST | `/api/tickets` | Criar ticket |
| PUT | `/api/tickets/{id}` | Atualizar ticket |
| PATCH | `/api/tickets/lote` | Atribuir técnico, alterar status e prioridade em massa |
| POST | `/api/tickets/{id}/mensagens` | Responder ou adicionar nota interna |

#### SLA
| Método | Rota | Descrição |
|---|---|---|
| GET | `/api/sla/regras` | Listar regras |
| PUT | `/api/sla/regras` | Atualizar regras e horário comercial |
| GET | `/api/sla/indicadores` | Cumprido, violado, em risco e evolução semanal |
| GET | `/api/sla/violacoes` | Violações com motivo, técnico e cliente |

#### Equipe e clientes
| Método | Rota | Descrição |
|---|---|---|
| GET | `/api/equipe/ranking` | Ranking de técnicos |
| GET | `/api/equipe/carga` | Carga atual por técnico |
| GET | `/api/clientes` | Lista de clientes |
| GET | `/api/clientes/{id}/tickets` | Histórico de chamados do cliente |

### 3.5 Dados de exemplo (seed)

- Pelo menos 200 tickets nos últimos 60 dias
- 10 técnicos distribuídos em 3 equipes
- 30 clientes
- Nomes e assuntos realistas em português (BR), com mensagens, histórico e algumas violações de SLA

### 3.6 Requisitos

- Migrations geradas e aplicadas automaticamente em desenvolvimento
- README em `backend/` com passos para rodar e URL do Swagger
- Ao final, `dotnet build` e testes devem passar

---

## 4. Frontend

### 4.1 Organização

Pasta `frontend/` com as subpastas: `components`, `pages`, `hooks`, `services`, `types`.

- Cliente HTTP centralizado, com URL base configurável por variável de ambiente
- Tipos TypeScript espelhando os DTOs do backend
- Estados de loading, erro e vazio em todas as telas
- Responsivo, com tema claro/escuro

### 4.2 Navegação (sidebar fixa)

Visão Geral · Tickets · SLA · Equipe · Clientes · Relatórios

### 4.3 Visão Geral

**Cards de KPI** (cada um com variação, seta verde/vermelha):
abertos, em andamento, aguardando cliente, resolvidos hoje, tempo médio de primeira resposta, tempo médio de resolução, % de SLA cumprido, CSAT.

**Gráficos:**
- Linha: abertos x resolvidos (30 dias)
- Barras: volume por canal
- Rosca: distribuição por prioridade
- Barras horizontais: volume por técnico

**Lista "Prestes a estourar SLA"** com contagem regressiva em tempo real.

**Filtros globais:** período, equipe, técnico, prioridade, canal.

### 4.4 Aba Tickets

**Tabela:** ID, assunto, cliente, técnico, prioridade (badge colorido), status, canal, SLA restante (barra de progresso + tempo), abertura, última atualização.

**Funcionalidades:**
- Busca, filtros combináveis, ordenação e paginação no servidor
- Seleção em massa (atribuir técnico, alterar status e prioridade)
- Alternância entre **Lista** e **Kanban**, com arrastar e soltar entre status atualizando via API
- **Drawer lateral** ao clicar no ticket: dados do cliente, técnico, timeline da conversa, notas internas, histórico de alterações e ações (responder, transferir, alterar status, encerrar)
- Botão **Novo ticket** com formulário em modal e validação

### 4.5 Aba SLA

- Edição das regras por prioridade e do horário comercial
- Indicadores: cumprido, violado, em risco
- Gráfico de evolução semanal
- Tabela de violações com motivo, técnico e cliente

| Cor | Significado |
|---|---|
| Verde | No prazo |
| Amarelo | Em risco |
| Vermelho | Violado |

### 4.6 Equipe, Clientes e Relatórios

- **Equipe:** ranking de técnicos (resolvidos, tempo médio, CSAT, SLA) e carga atual com indicador de sobrecarga
- **Clientes:** lista com total de chamados, último contato e CSAT; histórico de chamados ao abrir um cliente
- **Relatórios:** filtro por período e exportação em CSV (PDF opcional)

### 4.7 Requisitos

- README em `frontend/` com passos para rodar
- Ao final, `npm run build` deve passar sem erros de tipo ou lint

---

## 5. Ordem de entrega

### Etapa 1: Backend
1. Solução em camadas, entidades, migrations e seed
2. Regras de SLA com testes
3. Endpoints de dashboard e tickets
4. Endpoints de SLA, equipe e clientes

### Etapa 2: Frontend
1. Layout base (sidebar, rotas, tema), cliente HTTP e tipos
2. Visão Geral e aba Tickets completas
3. SLA, Equipe, Clientes e Relatórios

---

## 6. Critérios de aceite

- [ ] API sobe e o Swagger lista todos os endpoints
- [ ] Seed popula o banco com dados realistas
- [ ] Cálculo de SLA respeita horário comercial e pausa em `AguardandoCliente`
- [ ] Visão Geral exibe KPIs, gráficos e lista de SLA em risco
- [ ] Tickets: filtros, paginação, Kanban com arrastar e soltar e drawer de detalhe funcionando
- [ ] Criação e edição de tickets persistem no banco
- [ ] Abas SLA, Equipe, Clientes e Relatórios consumindo a API
- [ ] Builds do backend e do frontend sem erros
