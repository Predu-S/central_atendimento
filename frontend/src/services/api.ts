import type { ClienteDto, ConfiguracaoSlaDto, FiltroTickets, IndicadoresSlaDto, LoteInput, MensagemDto, MensagemInput, PaginaDto, ProblemDetails, RankingDto, CargaDto, ResumoDto, SeriesDto, TicketDetalheDto, TicketDto, TicketInput, ViolacaoDto } from '../types/api';
export const API_BASE_URL = (import.meta.env.VITE_API_BASE_URL || 'http://localhost:5000').replace(/\/+$/, '');
export class ApiError extends Error {
  status: number; problem: ProblemDetails;
  constructor(status: number, problem: ProblemDetails) { super(problem.detail || problem.title || 'Não foi possível concluir a operação.'); this.status = status; this.problem = problem; }
}
function queryString(filters?: FiltroTickets) { const query = new URLSearchParams(); for (const [key, value] of Object.entries(filters || {})) if (value !== undefined && value !== '') query.set(key, String(value)); const text = query.toString(); return text ? `?${text}` : ''; }
async function http<T>(path: string, options: RequestInit = {}): Promise<T> {
  const timeout = AbortSignal.timeout(30000);
  const signal = options.signal ? AbortSignal.any([options.signal, timeout]) : timeout;
  let response: Response;
  try { response = await fetch(`${API_BASE_URL}${path}`, { ...options, signal, headers: { Accept: 'application/json', ...(options.body ? { 'Content-Type': 'application/json' } : {}), ...options.headers } }); }
  catch (error) { if (options.signal?.aborted) throw error; throw new Error(`Não foi possível conectar à API em ${API_BASE_URL}. Verifique se o backend está em execução e se a URL está correta.`); }
  const text = await response.text();
  let data: unknown;
  try { data = text ? JSON.parse(text) : undefined; } catch { throw new Error('A API retornou uma resposta inválida. Verifique a URL configurada.'); }
  if (!response.ok) throw new ApiError(response.status, (data && typeof data === 'object' ? data : { title: 'Falha na operação' }) as ProblemDetails);
  return data as T;
}
const get = <T>(path: string, filters?: FiltroTickets, signal?: AbortSignal) => http<T>(path + queryString(filters), { signal });
const send = <T>(path: string, method: string, data: unknown) => http<T>(path, { method, body: JSON.stringify(data) });
export const api = {
  resumo: (f?: FiltroTickets, s?: AbortSignal) => get<ResumoDto>('/api/dashboard/resumo', f, s),
  series: (f?: FiltroTickets, s?: AbortSignal) => get<SeriesDto>('/api/dashboard/series', f, s),
  risco: (f?: FiltroTickets, s?: AbortSignal) => get<TicketDto[]>('/api/dashboard/sla-em-risco', f, s),
  tickets: (f?: FiltroTickets, s?: AbortSignal) => get<PaginaDto<TicketDto>>('/api/tickets', f, s),
  detalhe: (id: number, s?: AbortSignal) => get<TicketDetalheDto>(`/api/tickets/${id}`, undefined, s),
  criar: (input: TicketInput) => send<TicketDetalheDto>('/api/tickets', 'POST', input),
  atualizar: (id: number, input: TicketInput) => send<TicketDetalheDto>(`/api/tickets/${id}`, 'PUT', input),
  lote: (input: LoteInput) => send<TicketDto[]>('/api/tickets/lote', 'PATCH', input),
  mensagem: (id: number, input: MensagemInput) => send<MensagemDto>(`/api/tickets/${id}/mensagens`, 'POST', input),
  regras: (s?: AbortSignal) => get<ConfiguracaoSlaDto>('/api/sla/regras', undefined, s),
  salvarRegras: (input: ConfiguracaoSlaDto) => send<ConfiguracaoSlaDto>('/api/sla/regras', 'PUT', input),
  indicadores: (f?: FiltroTickets, s?: AbortSignal) => get<IndicadoresSlaDto>('/api/sla/indicadores', f, s),
  violacoes: (f?: FiltroTickets, s?: AbortSignal) => get<ViolacaoDto[]>('/api/sla/violacoes', f, s),
  ranking: (f?: FiltroTickets, s?: AbortSignal) => get<RankingDto[]>('/api/equipe/ranking', f, s),
  carga: (f?: FiltroTickets, s?: AbortSignal) => get<CargaDto[]>('/api/equipe/carga', f, s),
  clientes: (s?: AbortSignal) => get<ClienteDto[]>('/api/clientes', undefined, s),
  ticketsCliente: (id: number, f?: FiltroTickets, s?: AbortSignal) => get<PaginaDto<TicketDto>>(`/api/clientes/${id}/tickets`, f, s),
};
export async function exportTickets(filters: FiltroTickets, signal: AbortSignal, progress: (count: number, total: number) => void) {
  const first = await api.tickets({ ...filters, pagina: 1, tamanhoPagina: 100, ordenarPor: 'id', descendente: false }, signal);
  const rows = [...first.itens]; progress(rows.length, first.total);
  for (let pagina = 2; pagina <= Math.ceil(first.total / 100); pagina++) { const result = await api.tickets({ ...filters, pagina, tamanhoPagina: 100, ordenarPor: 'id', descendente: false }, signal); rows.push(...result.itens); progress(rows.length, first.total); }
  return rows;
}
