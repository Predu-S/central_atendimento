import { Pause, Clock3 } from 'lucide-react';
import type { HorarioComercial, TicketDto, MedicaoSla } from '../types/api';
import { activeSla, tickSla, useNow, workingElapsed } from '../hooks/useSlaClock';
import { duration, label } from '../services/format';
export function SlaMeasure({ measure, paused = false, name, now, readAt, horario, countdown = false }: { measure: MedicaoSla; paused?: boolean; name: string; now: number; readAt: number; horario?: HorarioComercial; countdown?: boolean }) {
  const current = tickSla(measure, paused, workingElapsed(readAt, now, horario)); const pct = Math.max(0, Math.min(100, current.percentualConsumido));
  return <div className={`sla-progress sla-${current.situacao}`}><div><span>{paused && !measure.concluido ? <Pause size={12}/> : <Clock3 size={12}/>} {name}</span><b title={`${label(current.situacao)} · ${Math.round(current.percentualConsumido)}% consumido`}>{duration(current.restanteMinutos, countdown)}</b></div><div className="progress-track"><div style={{ width: `${pct}%` }}/></div><small>{measure.concluido ? 'Concluído' : paused ? 'Contagem pausada' : label(current.situacao)}</small></div>;
}
export function SlaProgress({ ticket, readAt, horario, countdown = false }: { ticket: TicketDto; readAt: number; horario?: HorarioComercial; countdown?: boolean }) { const now = useNow(); const { measure, name } = activeSla(ticket); return <SlaMeasure measure={measure} paused={ticket.sla.pausado} name={name} now={now} readAt={readAt} horario={horario} countdown={countdown}/>; }
