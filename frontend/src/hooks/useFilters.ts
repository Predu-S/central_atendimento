import { useSearchParams } from 'react-router-dom';
import type { FiltroTickets, Prioridade, Canal, StatusTicket } from '../types/api';
import { priorities, statuses, channels } from '../services/format';
export function dateValue(date: Date) { return `${date.getFullYear()}-${String(date.getMonth() + 1).padStart(2, '0')}-${String(date.getDate()).padStart(2, '0')}`; }
export function defaultPeriod() { const today = new Date(); const from = new Date(today); from.setDate(today.getDate() - 29); return { de: dateValue(from), ate: dateValue(today) }; }
export function useFilters() {
  const [params, setParams] = useSearchParams(); const defaults = defaultPeriod();
  const period = { de: params.get('de') ?? defaults.de, ate: params.get('ate') ?? defaults.ate };
  const de = period.de ? new Date(`${period.de}T00:00:00`) : undefined;
  const ate = period.ate ? new Date(`${period.ate}T00:00:00`) : undefined;
  if (ate) ate.setDate(ate.getDate() + 1);
  const validDate = (d: Date | undefined) => d && !Number.isNaN(d.getTime()) ? d.toISOString() : undefined;
  const id = (key: string) => { const n = Number(params.get(key)); return Number.isInteger(n) && n > 0 ? n : undefined; };
  const prioridade = params.get('prioridade'); const canal = params.get('canal'); const status = params.get('status');
  const filters: FiltroTickets = { inicio: validDate(de), fim: validDate(ate), equipeId: id('equipeId'), tecnicoId: id('tecnicoId'), prioridade: priorities.includes(prioridade as Prioridade) ? prioridade as Prioridade : undefined, canal: channels.includes(canal as Canal) ? canal as Canal : undefined, status: statuses.includes(status as StatusTicket) ? status as StatusTicket : undefined, busca: params.get('busca') || undefined };
  const error = period.de && period.ate && (period.ate < period.de || ((ate?.getTime() || 0) - (de?.getTime() || 0)) / 86400000 > 366) ? 'Selecione um período válido de até 366 dias.' : null;
  function update(values: Record<string, string>) { setParams(current => { const next = new URLSearchParams(current); for (const [key, value] of Object.entries(values)) { if (value === '') next.delete(key); else next.set(key, value); } next.delete('pagina'); return next; }, { replace: true }); }
  function reset() { setParams({}, { replace: true }); }
  return { filters, period, update, reset, error, params, setParams };
}
