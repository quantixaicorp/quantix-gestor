import { createBrowserRouter } from 'react-router-dom'
import AppLayout from '@/components/layout/AppLayout'
import ErrorPage from '@/components/ErrorPage'
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
import Categorias from '@/pages/financeiro/Categorias'
import Relatorios from '@/pages/relatorios/Relatorios'
import Orcamentos from '@/pages/orcamentos/Orcamentos'
import NovoOrcamento from '@/pages/orcamentos/NovoOrcamento'
import DetalheOrcamento from '@/pages/orcamentos/DetalheOrcamento'
import Agendamentos from '@/pages/agendamentos/Agendamentos'
import NovoAgendamento from '@/pages/agendamentos/NovoAgendamento'
import DetalheAgendamento from '@/pages/agendamentos/DetalheAgendamento'
import AgendaProfissional from '@/pages/agendamentos/AgendaProfissional'
import Profissionais from '@/pages/profissionais/Profissionais'
import DisponibilidadeProfissional from '@/pages/profissionais/DisponibilidadeProfissional'
import Servicos from '@/pages/servicos/Servicos'
import Fiscal from '@/pages/fiscal/Fiscal'
import Contratos from '@/pages/contratos/Contratos'
import NovoContrato from '@/pages/contratos/NovoContrato'
import DetalheContrato from '@/pages/contratos/DetalheContrato'
import ContratoTemplatesPage from '@/pages/contratos/ContratoTemplates'
import Cobrancas from '@/pages/cobrancas/Cobrancas'
import NovaCobranca from '@/pages/cobrancas/NovaCobranca'
import DetalheCobranca from '@/pages/cobrancas/DetalheCobranca'
import AgendamentoPublico from '@/pages/agendamento-publico/AgendamentoPublico'
import OrcamentoPublicoPage from '@/pages/orcamentos-publicos/OrcamentoPublico'
import AssinarSlug from '@/pages/publico/AssinarSlug'
import AssinarPlano from '@/pages/publico/AssinarPlano'
import ConfiguracaoEmpresa from '@/pages/configuracoes/ConfiguracaoEmpresa'
import AgendamentoPublicoConfig from '@/pages/configuracoes/AgendamentoPublicoConfig'
import Integracoes from '@/pages/configuracoes/Integracoes'
import Automacao from '@/pages/configuracoes/Automacao'
import Fornecedores from '@/pages/fornecedores/Fornecedores'
import Compras from '@/pages/compras/Compras'
import NovaCompra from '@/pages/compras/NovaCompra'
import DetalheCompra from '@/pages/compras/DetalheCompra'
import PedidosCompra from '@/pages/compras/PedidosCompra'
import NovoPedidoCompra from '@/pages/compras/NovoPedidoCompra'
import DetalhePedidoCompra from '@/pages/compras/DetalhePedidoCompra'
import DashboardCompras from '@/pages/compras/DashboardCompras'
import LogAutomacao from '@/pages/automacao/LogAutomacao'
import PlanosList from '@/pages/planos/PlanosList'
import PlanoWizard from '@/pages/planos/PlanoWizard'
import PlanoDetalhe from '@/pages/planos/PlanoDetalhe'

export const router = createBrowserRouter([
  { path: '/auth', element: <Auth />, errorElement: <ErrorPage /> },
  { path: '/auth/callback', element: <AuthCallback />, errorElement: <ErrorPage /> },
  { path: '/agendar/:slug', element: <AgendamentoPublico />, errorElement: <ErrorPage /> },
  { path: '/orcamento/:token', element: <OrcamentoPublicoPage />, errorElement: <ErrorPage /> },
  { path: '/assinar/:slug', element: <AssinarSlug />, errorElement: <ErrorPage /> },
  { path: '/assinar/:slug/:planoId', element: <AssinarPlano />, errorElement: <ErrorPage /> },
  {
    element: <AppLayout />,
    errorElement: <ErrorPage />,
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
      { path: '/agenda', element: <AgendaProfissional /> },
      { path: '/profissionais', element: <Profissionais /> },
      { path: '/profissionais/:id/disponibilidade', element: <DisponibilidadeProfissional /> },
      { path: '/estoque', element: <Produtos /> },
      { path: '/estoque/movimentacoes', element: <Movimentacoes /> },
      { path: '/servicos', element: <Servicos /> },
      { path: '/financeiro', element: <Lancamentos /> },
      { path: '/financeiro/pagar', element: <ContasPagar /> },
      { path: '/financeiro/receber', element: <ContasReceber /> },
      { path: '/financeiro/categorias', element: <Categorias /> },
      { path: '/clientes', element: <Clientes /> },
      { path: '/fornecedores', element: <Fornecedores /> },
      { path: '/compras', element: <Compras /> },
      { path: '/compras/nova', element: <NovaCompra /> },
      { path: '/compras/pedidos', element: <PedidosCompra /> },
      { path: '/compras/pedidos/novo', element: <NovoPedidoCompra /> },
      { path: '/compras/pedidos/:id', element: <DetalhePedidoCompra /> },
      { path: '/compras/dashboard', element: <DashboardCompras /> },
      { path: '/compras/:id', element: <DetalheCompra /> },
      { path: '/relatorios', element: <Relatorios /> },
      { path: '/fiscal', element: <Fiscal /> },
      { path: '/contratos', element: <Contratos /> },
      { path: '/contratos/novo', element: <NovoContrato /> },
      { path: '/contratos/templates', element: <ContratoTemplatesPage /> },
      { path: '/contratos/:id', element: <DetalheContrato /> },
      { path: '/cobrancas', element: <Cobrancas /> },
      { path: '/cobrancas/nova', element: <NovaCobranca /> },
      { path: '/cobrancas/:id', element: <DetalheCobranca /> },
      { path: '/configuracoes/empresa', element: <ConfiguracaoEmpresa /> },
      { path: '/configuracoes/agendamento-publico', element: <AgendamentoPublicoConfig /> },
      { path: '/configuracoes/integracoes', element: <Integracoes /> },
      { path: '/configuracoes/automacao', element: <Automacao /> },
      { path: '/automacao/log', element: <LogAutomacao /> },
      { path: '/planos', element: <PlanosList /> },
      { path: '/planos/novo', element: <PlanoWizard /> },
      { path: '/planos/:id', element: <PlanoDetalhe /> },
    ],
  },
])
