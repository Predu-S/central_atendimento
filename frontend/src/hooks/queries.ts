import { useQuery, useMutation, useQueryClient, keepPreviousData } from '@tanstack/react-query';
import { api } from '../services/api';
import type { FiltroTickets, TicketInput, LoteInput, MensagemInput, ConfiguracaoSlaDto } from '../types/api';
export function useDirectory() { return useQuery({ queryKey: ['directory'], queryFn: ({ signal }) => api.ranking(undefined, signal), staleTime: 300000 }); }
export function useClientes() { return useQuery({ queryKey: ['clientes'], queryFn: ({ signal }) => api.clientes(signal) }); }
export function useRegras() { return useQuery({ queryKey: ['regras'], queryFn: ({ signal }) => api.regras(signal), staleTime: 60000 }); }
export function useTickets(filters: FiltroTickets) { return useQuery({ queryKey: ['tickets', filters], queryFn: ({ signal }) => api.tickets(filters, signal), placeholderData: keepPreviousData, refetchInterval: 15000 }); }
export function useDetalhe(id: number) { return useQuery({ queryKey: ['ticket', id], queryFn: ({ signal }) => api.detalhe(id, signal), refetchInterval: 15000 }); }
export function useDashboard(filters: FiltroTickets) {
  const resumo = useQuery({ queryKey: ['resumo', filters], queryFn: ({ signal }) => api.resumo(filters, signal), refetchInterval: 30000 });
  const series = useQuery({ queryKey: ['series', filters], queryFn: ({ signal }) => api.series(filters, signal) });
  const risco = useQuery({ queryKey: ['risco', filters], queryFn: ({ signal }) => api.risco(filters, signal), refetchInterval: 5000 });
  return { resumo, series, risco };
}
export function useTicketMutations() {
  const client = useQueryClient();
  const refresh = () => client.invalidateQueries({ predicate: q => q.queryKey[0] !== 'regras' });
  const criar = useMutation({ mutationFn: (input: TicketInput) => api.criar(input), onSuccess: refresh });
  const atualizar = useMutation({ mutationFn: ({ id, input }: { id: number; input: TicketInput }) => api.atualizar(id, input), onSuccess: refresh });
  const lote = useMutation({ mutationFn: (input: LoteInput) => api.lote(input), onSuccess: refresh });
  const mensagem = useMutation({ mutationFn: ({ id, input }: { id: number; input: MensagemInput }) => api.mensagem(id, input), onSuccess: refresh });
  return { criar, atualizar, lote, mensagem };
}
export function useSaveRules() { const client = useQueryClient(); return useMutation({ mutationFn: (input: ConfiguracaoSlaDto) => api.salvarRegras(input), onSuccess: async () => { await client.invalidateQueries(); } }); }
