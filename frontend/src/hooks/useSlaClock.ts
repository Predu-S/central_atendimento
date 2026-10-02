import { useEffect, useState } from 'react';
import type { HorarioComercial, MedicaoSla, TicketDto } from '../types/api';
export function useNow() { const [now, setNow] = useState(Date.now()); useEffect(() => { const interval = setInterval(() => setNow(Date.now()), 1000); return () => clearInterval(interval); }, []); return now; }
export function workingElapsed(from: number, to: number, horario?: HorarioComercial) {
  if (!horario || to <= from) return 0;
  let fmt: Intl.DateTimeFormat;
  try { fmt = new Intl.DateTimeFormat('en-US', { timeZone: horario.fusoHorario, weekday: 'long', hour: '2-digit', minute: '2-digit', second: '2-digit', hourCycle: 'h23' }); } catch { return 0; }
  const seconds = (t: string) => t.split(':').reduce((sum, part, i) => sum + Number(part) * [3600, 60, 1][i], 0);
  const start = seconds(horario.inicio), end = seconds(horario.fim); let elapsed = 0;
  // Após 60s sem uma leitura nova, a interpolação congela; a API é sempre a referência.
  for (let tick = from; tick < Math.min(to, from + 60000); tick += 1000) { const p = Object.fromEntries(fmt.formatToParts(tick).map(x => [x.type, x.value])); const time = Number(p.hour) * 3600 + Number(p.minute) * 60 + Number(p.second); if (horario.dias.includes(p.weekday as HorarioComercial['dias'][number]) && time >= start && time < end) elapsed += Math.min(1000, to - tick); }
  return elapsed / 60000;
}
export function activeSla(t: TicketDto): { measure: MedicaoSla; name: string } { if (!t.sla.primeiraResposta.concluido && t.sla.primeiraResposta.restanteMinutos < t.sla.resolucao.restanteMinutos) return { measure: t.sla.primeiraResposta, name: '1ª resposta' }; return { measure: t.sla.resolucao, name: 'Resolução' }; }
export function tickSla(measure: MedicaoSla, paused: boolean, elapsed: number): MedicaoSla { if (measure.concluido || paused) return measure; const remaining = measure.restanteMinutos - elapsed; const consumed = measure.consumidoMinutos + elapsed; return { ...measure, restanteMinutos: remaining, consumidoMinutos: consumed, percentualConsumido: consumed / measure.prazoMinutos * 100, situacao: remaining <= 0 ? 'Violado' : remaining < measure.prazoMinutos * .2 ? 'EmRisco' : 'NoPrazo' }; }
