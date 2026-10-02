import { useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import { Search, ArrowUpRight, Building2 } from 'lucide-react';
import { useClientes, useRegras } from '../hooks/queries';
import { api } from '../services/api';
import type { ClienteDto } from '../types/api';
import { dateTime, number } from '../services/format';
import { Button, Empty, PageTitle, Panel, Pagination, QueryState } from '../components/ui';
import { Overlay } from '../components/Overlay';
import { TicketTable } from '../components/TicketTable';
import { TicketDrawer } from '../components/TicketDrawer';
function ClientHistory({ cliente, onClose, onTicket }: { cliente: ClienteDto; onClose: () => void; onTicket: (id: number) => void }) {
  const [page, setPage] = useState(1); const regras = useRegras(); const query = useQuery({ queryKey: ['clienteTickets', cliente.id, page], queryFn: ({ signal }) => api.ticketsCliente(cliente.id, { pagina: page, tamanhoPagina: 20 }, signal) });
  return <Overlay title={cliente.nome} onClose={onClose}><div className="client-history"><p className="muted">{cliente.email} · {cliente.totalTickets} chamados em todo o histórico</p><QueryState query={query} empty={!query.data?.itens.length}><TicketTable tickets={query.data?.itens || []} onOpen={onTicket} readAt={query.dataUpdatedAt} horario={regras.data?.horario}/></QueryState>{query.data && <Pagination page={page} size={20} total={query.data.total} onPage={setPage}/>}</div></Overlay>;
}
export function Clientes() {
  const query = useClientes(); const [search, setSearch] = useState(''), [selected, setSelected] = useState<ClienteDto | null>(null), [ticket, setTicket] = useState<number | null>(null);
  const list = (query.data || []).filter(c => `${c.nome} ${c.email}`.toLocaleLowerCase('pt-BR').includes(search.toLocaleLowerCase('pt-BR')));
  return <><PageTitle title="Clientes" description="Contexto e histórico para cada relacionamento."/><Panel title="Base de clientes" description={query.data ? `${query.data.length} clientes na sua operação` : 'Clientes e histórico de chamados'} action={<Building2 size={22} className="text-brand-500"/>}><div className="client-toolbar search-form"><Search size={17}/><input aria-label="Buscar clientes" value={search} onChange={e => setSearch(e.target.value)} placeholder="Buscar por nome ou e-mail…"/></div><QueryState query={query} empty={query.data?.length === 0}>{list.length ? <div className="table-wrap"><table><thead><tr><th>Cliente</th><th>E-mail</th><th>Total de chamados</th><th>Último contato</th><th>CSAT</th><th/></tr></thead><tbody>{list.map(c => <tr key={c.id}><td><button className="table-link" onClick={() => setSelected(c)}>{c.nome}</button></td><td>{c.email}</td><td><b>{c.totalTickets}</b></td><td>{dateTime(c.ultimoContato)}</td><td>{c.csat == null ? 'Sem avaliação' : `${number(c.csat, 1)} / 5`}</td><td><Button variant="ghost" onClick={() => setSelected(c)}><ArrowUpRight size={16}/> Histórico</Button></td></tr>)}</tbody></table></div> : <Empty message="Nenhum cliente corresponde à busca."/>}</QueryState></Panel>{selected && !ticket && <ClientHistory cliente={selected} onClose={() => setSelected(null)} onTicket={setTicket}/>} {ticket && <TicketDrawer id={ticket} onClose={() => setTicket(null)}/>}</>;
}
