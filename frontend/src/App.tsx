import { lazy } from 'react';
import { Routes, Route, Link } from 'react-router-dom';
import { Layout } from './components/Layout';
const Dashboard = lazy(() => import('./pages/Dashboard').then(module => ({ default: module.Dashboard })));
const Tickets = lazy(() => import('./pages/Tickets').then(module => ({ default: module.Tickets })));
const Sla = lazy(() => import('./pages/Sla').then(module => ({ default: module.Sla })));
const Equipe = lazy(() => import('./pages/Equipe').then(module => ({ default: module.Equipe })));
const Clientes = lazy(() => import('./pages/Clientes').then(module => ({ default: module.Clientes })));
const Relatorios = lazy(() => import('./pages/Relatorios').then(module => ({ default: module.Relatorios })));
import { Empty } from './components/ui';
export function App() { return <Routes><Route element={<Layout/>}><Route index element={<Dashboard/>}/><Route path="tickets" element={<Tickets/>}/><Route path="sla" element={<Sla/>}/><Route path="equipe" element={<Equipe/>}/><Route path="clientes" element={<Clientes/>}/><Route path="relatorios" element={<Relatorios/>}/><Route path="*" element={<Empty title="Página não encontrada" message="Esse endereço não está disponível." action={<Link className="btn btn-primary" to="/">Voltar à Visão Geral</Link>}/>}/></Route></Routes>; }
