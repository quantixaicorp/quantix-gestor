import { createBrowserRouter } from 'react-router-dom'
import AppLayout from '@/components/layout/AppLayout'
import Auth from '@/pages/Auth'
import AuthCallback from '@/pages/AuthCallback'
import Dashboard from '@/pages/Dashboard'
import Produtos from '@/pages/estoque/Produtos'
import Movimentacoes from '@/pages/estoque/Movimentacoes'
import Clientes from '@/pages/clientes/Clientes'
import NovaVenda from '@/pages/vendas/NovaVenda'
import Historico from '@/pages/vendas/Historico'
import Lancamentos from '@/pages/financeiro/Lancamentos'
import ContasPagar from '@/pages/financeiro/ContasPagar'
import ContasReceber from '@/pages/financeiro/ContasReceber'
import Relatorios from '@/pages/relatorios/Relatorios'
import Orcamentos from '@/pages/orcamentos/Orcamentos'
import NovoOrcamento from '@/pages/orcamentos/NovoOrcamento'
import DetalheOrcamento from '@/pages/orcamentos/DetalheOrcamento'
import Agendamentos from '@/pages/agendamentos/Agendamentos'
import NovoAgendamento from '@/pages/agendamentos/NovoAgendamento'
import DetalheAgendamento from '@/pages/agendamentos/DetalheAgendamento'
import Profissionais from '@/pages/profissionais/Profissionais'
import DisponibilidadeProfissional from '@/pages/profissionais/DisponibilidadeProfissional'

export const router = createBrowserRouter([
  { path: '/auth', element: <Auth /> },
  { path: '/auth/callback', element: <AuthCallback /> },
  {
    element: <AppLayout />,
    children: [
      { path: '/', element: <Dashboard /> },
      { path: '/vendas', element: <Historico /> },
      { path: '/vendas/nova', element: <NovaVenda /> },
      { path: '/orcamentos', element: <Orcamentos /> },
      { path: '/orcamentos/novo', element: <NovoOrcamento /> },
      { path: '/orcamentos/:id', element: <DetalheOrcamento /> },
      { path: '/agendamentos', element: <Agendamentos /> },
      { path: '/agendamentos/novo', element: <NovoAgendamento /> },
      { path: '/agendamentos/:id', element: <DetalheAgendamento /> },
      { path: '/profissionais', element: <Profissionais /> },
      { path: '/profissionais/:id/disponibilidade', element: <DisponibilidadeProfissional /> },
      { path: '/estoque', element: <Produtos /> },
      { path: '/estoque/movimentacoes', element: <Movimentacoes /> },
      { path: '/financeiro', element: <Lancamentos /> },
      { path: '/financeiro/pagar', element: <ContasPagar /> },
      { path: '/financeiro/receber', element: <ContasReceber /> },
      { path: '/clientes', element: <Clientes /> },
      { path: '/relatorios', element: <Relatorios /> },
    ],
  },
])
