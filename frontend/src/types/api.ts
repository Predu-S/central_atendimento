export type StatusTicket = 'Aberto' | 'EmAndamento' | 'AguardandoCliente' | 'Resolvido' | 'Fechado';
export type Prioridade = 'Critica' | 'Alta' | 'Media' | 'Baixa';
export type Canal = 'WhatsApp' | 'Email' | 'Telefone' | 'Chat';
export type SituacaoSla = 'NoPrazo' | 'EmRisco' | 'Violado';
export type DiaSemana = 'Sunday' | 'Monday' | 'Tuesday' | 'Wednesday' | 'Thursday' | 'Friday' | 'Saturday';
export interface PessoaDto { id: number; nome: string }
export interface MedicaoSla { prazoMinutos: number; consumidoMinutos: number; restanteMinutos: number; percentualConsumido: number; situacao: SituacaoSla; concluido: boolean }
export interface SlaTicket { primeiraResposta: MedicaoSla; resolucao: MedicaoSla; pausado: boolean }
export interface TicketDto { id: number; assunto: string; descricao: string; cliente: PessoaDto; tecnico: PessoaDto | null; equipe: PessoaDto | null; status: StatusTicket; prioridade: Prioridade; canal: Canal; criadoEm: string; atualizadoEm: string; primeiraRespostaEm: string | null; resolvidoEm: string | null; csat: number | null; sla: SlaTicket }
export interface MensagemDto { id: number; conteudo: string; notaInterna: boolean; tecnico: PessoaDto; criadoEm: string }
export interface HistoricoDto { id: number; campo: string; anterior: string | null; novo: string; criadoEm: string }
export interface TimelineDto { criadoEm: string; tipo: string; mensagem: MensagemDto | null; alteracao: HistoricoDto | null }
export interface TicketDetalheDto { ticket: TicketDto; emailCliente: string; timeline: TimelineDto[] }
export interface TicketInput { assunto: string; descricao: string; clienteId: number; tecnicoId: number | null; equipeId: number | null; prioridade: Prioridade; canal: Canal; status: StatusTicket; csat: number | null }
export interface LoteInput { ids: number[]; tecnicoId?: number; status?: StatusTicket; prioridade?: Prioridade }
export interface MensagemInput { conteudo: string; tecnicoId: number; notaInterna: boolean }
export interface PaginaDto<T> { itens: T[]; total: number; pagina: number; tamanhoPagina: number }
export interface FiltroTickets { inicio?: string; fim?: string; equipeId?: number; tecnicoId?: number; prioridade?: Prioridade; canal?: Canal; status?: StatusTicket; clienteId?: number; busca?: string; pagina?: number; tamanhoPagina?: number; ordenarPor?: 'criadoEm' | 'atualizadoEm' | 'prioridade' | 'status' | 'assunto' | 'id'; descendente?: boolean }
export interface KpiDto { valor: number | null; anterior: number | null; variacaoPercentual: number | null }
export interface ResumoDto { abertos: KpiDto; emAndamento: KpiDto; aguardandoCliente: KpiDto; resolvidosHoje: KpiDto; tempoMedioPrimeiraRespostaMinutos: KpiDto; tempoMedioResolucaoMinutos: KpiDto; slaCumpridoPercentual: KpiDto; csat: KpiDto }
export interface SerieDiaDto { dia: string; abertos: number; resolvidos: number }
export interface VolumeDto { nome: string; quantidade: number; id: number | null }
export interface SeriesDto { porDia: SerieDiaDto[]; porCanal: VolumeDto[]; porPrioridade: VolumeDto[]; porTecnico: VolumeDto[] }
export interface HorarioComercial { dias: DiaSemana[]; inicio: string; fim: string; fusoHorario: string }
export interface RegraSlaDto { prioridade: Prioridade; primeiraRespostaMinutos: number; resolucaoMinutos: number }
export interface ConfiguracaoSlaDto { regras: RegraSlaDto[]; horario: HorarioComercial }
export interface EvolucaoSlaDto { semana: string; cumpridos: number; violados: number; percentualCumprido: number | null }
export interface IndicadoresSlaDto { cumpridos: number; violados: number; emRisco: number; pendentes: number; percentualCumprido: number | null; evolucaoSemanal: EvolucaoSlaDto[] }
export interface ViolacaoDto { ticket: TicketDto; motivos: string[] }
export interface RankingDto { tecnico: PessoaDto; equipe: PessoaDto; resolvidos: number; tempoMedioResolucaoMinutos: number | null; csat: number | null; slaCumpridoPercentual: number | null }
export interface CargaDto { tecnico: PessoaDto; equipe: PessoaDto; abertos: number; emAndamento: number; aguardandoCliente: number; total: number; sobrecarga: boolean }
export interface ClienteDto { id: number; nome: string; email: string; totalTickets: number; ultimoContato: string | null; csat: number | null }
export interface ProblemDetails { title?: string; detail?: string; status?: number; traceId?: string; errors?: Record<string, string[]> }
