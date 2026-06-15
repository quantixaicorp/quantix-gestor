import {
  TrendingUp, ShoppingCart, PackageX, AlertTriangle,
  ArrowUpCircle, Wallet, TrendingDown, DollarSign, BarChart3,
  Users, UserPlus, Package, Percent, PiggyBank,
  Calendar, CheckCircle, XCircle, FileText, Building2,
  Bell, CreditCard, ClipboardList, RefreshCw, Star,
  UserX, Boxes,
} from 'lucide-react'
import { Link } from 'react-router-dom'
import type {
  DashboardResponse, ModulosDashboardResponse, DashboardExtrasResponse, WidgetId,
} from '@/types/dashboard'
import KpiCard from './KpiCard'
import GraficoVendas from './GraficoVendas'
import GraficoFluxo from './GraficoFluxo'
import GraficoDespesasCategoria from './GraficoDespesasCategoria'
import TopProdutos from './TopProdutos'
import TopClientes from './TopClientes'
import GraficoVendasFormaPgto from './GraficoVendasFormaPgto'
import TabelaUltimasVendas from './TabelaUltimasVendas'
import GraficoReceitasCategoria from './GraficoReceitasCategoria'
import GraficoFluxoAnual from './GraficoFluxoAnual'
import TabelaContasVencidas from './TabelaContasVencidas'
import TabelaProximosVencimentos from './TabelaProximosVencimentos'
import GraficoDistribuicaoCategorias from './GraficoDistribuicaoCategorias'
import TabelaEstoqueBaixo from './TabelaEstoqueBaixo'
import TabelaAgendaHoje from './TabelaAgendaHoje'
import GraficoAgendaStatus from './GraficoAgendaStatus'
import GraficoOcupacaoProfissional from './GraficoOcupacaoProfissional'
import TabelaContratosVencendo from './TabelaContratosVencendo'
import GraficoAgingCobrancas from './GraficoAgingCobrancas'
import TabelaCobrancasVencidas from './TabelaCobrancasVencidas'
import GraficoOrcamentosStatus from './GraficoOrcamentosStatus'
import GraficoEvolucaoAssinaturas from './GraficoEvolucaoAssinaturas'

const fmt = (v: number | null | undefined) =>
  (v ?? 0).toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' })

const fmtN = (v: number | null | undefined) =>
  (v ?? 0).toLocaleString('pt-BR')

const fmtPct = (v: number | null | undefined) =>
  `${(v ?? 0).toLocaleString('pt-BR', { maximumFractionDigits: 1 })}%`

export interface WidgetMeta {
  id: WidgetId
  label: string
  description: string
}

export const ALL_WIDGETS: WidgetMeta[] = [
  // Financeiro
  { id: 'kpi-saldo-mes', label: 'Saldo do Mês', description: 'Receitas menos despesas no mês atual' },
  { id: 'kpi-receitas-mes', label: 'Receitas do Mês', description: 'Total de receitas pagas no mês' },
  { id: 'kpi-despesas-mes', label: 'Despesas do Mês', description: 'Total de despesas pagas no mês' },
  { id: 'kpi-contas-receber', label: 'A Receber', description: 'Contas a receber pendentes' },
  { id: 'kpi-contas-vencidas', label: 'Contas Vencidas', description: 'Despesas vencidas não pagas' },
  { id: 'kpi-contas-pagar-proximas', label: 'A Pagar (7 dias)', description: 'Despesas vencendo nos próximos 7 dias' },
  { id: 'kpi-inadimplencia', label: 'Inadimplência', description: 'Percentual de receitas em atraso' },
  { id: 'kpi-saldo-projetado', label: 'Saldo Projetado', description: 'Recebimentos previstos menos obrigações' },
  { id: 'grafico-fluxo-caixa', label: 'Fluxo de Caixa', description: 'Receitas e despesas pagas no mês' },
  { id: 'grafico-despesas-categoria', label: 'Despesas por Categoria', description: 'Distribuição de despesas por categoria' },
  { id: 'grafico-receitas-categoria', label: 'Receitas por Categoria', description: 'Distribuição de receitas por categoria' },
  { id: 'grafico-fluxo-anual', label: 'Fluxo Anual', description: 'Receitas, despesas e saldo dos últimos 12 meses' },
  { id: 'tabela-contas-vencidas', label: 'Contas Vencidas (Detalhe)', description: 'Lista de contas a pagar vencidas' },
  { id: 'tabela-proximos-vencimentos', label: 'Próximos Vencimentos', description: 'Contas vencendo nos próximos 7 dias' },
  // Vendas
  { id: 'kpi-vendas-hoje', label: 'Vendido Hoje', description: 'Total de vendas concluídas hoje' },
  { id: 'kpi-vendas-mes', label: 'Vendido no Mês', description: 'Total de vendas concluídas no mês' },
  { id: 'kpi-qtd-vendas-mes', label: 'Quantidade de Vendas', description: 'Número de vendas realizadas no mês' },
  { id: 'kpi-ticket-medio', label: 'Ticket Médio', description: 'Valor médio por venda no mês' },
  { id: 'kpi-lucro-estimado', label: 'Lucro Estimado', description: 'Lucro bruto estimado do mês' },
  { id: 'kpi-maior-venda-dia', label: 'Maior Venda do Dia', description: 'Maior valor de venda único registrado hoje' },
  { id: 'grafico-tendencia-vendas', label: 'Tendência de Vendas', description: 'Gráfico de vendas dos últimos 7 dias' },
  { id: 'grafico-vendas-forma-pgto', label: 'Vendas por Forma de Pagamento', description: 'Distribuição de vendas por forma de pagamento' },
  { id: 'tabela-top-produtos', label: 'Top Produtos', description: 'Produtos mais vendidos no mês' },
  { id: 'tabela-ultimas-vendas', label: 'Últimas Vendas', description: 'As 10 vendas mais recentes' },
  // Clientes
  { id: 'kpi-total-clientes', label: 'Total de Clientes', description: 'Número total de clientes cadastrados' },
  { id: 'kpi-clientes-novos-mes', label: 'Clientes Novos', description: 'Novos clientes cadastrados no mês' },
  { id: 'kpi-clientes-inativos', label: 'Clientes Inativos', description: 'Clientes sem compra nos últimos 90 dias' },
  { id: 'tabela-top-clientes', label: 'Top Clientes', description: 'Clientes com maior faturamento no mês' },
  // Estoque
  { id: 'kpi-valor-estoque', label: 'Valor em Estoque', description: 'Valor total dos produtos em estoque' },
  { id: 'kpi-produtos-ativos', label: 'Produtos Ativos', description: 'Total de produtos com estoque disponível' },
  { id: 'alerta-estoque-baixo', label: 'Alerta de Estoque', description: 'Produtos abaixo do estoque mínimo' },
  { id: 'grafico-distribuicao-categorias', label: 'Distribuição por Categorias', description: 'Valor em estoque distribuído por categoria' },
  { id: 'tabela-estoque-baixo', label: 'Estoque Baixo (Detalhe)', description: 'Lista detalhada de produtos com estoque baixo' },
  // Agendamentos
  { id: 'kpi-agendamentos-hoje', label: 'Agendamentos Hoje', description: 'Número de agendamentos para hoje' },
  { id: 'kpi-agendamentos-confirmados', label: 'Confirmados Hoje', description: 'Agendamentos confirmados ou concluídos hoje' },
  { id: 'kpi-agendamentos-cancelados', label: 'Cancelados no Mês', description: 'Agendamentos cancelados no mês' },
  { id: 'kpi-taxa-conclusao-agendamentos', label: 'Taxa de Conclusão', description: 'Percentual de agendamentos concluídos no mês' },
  { id: 'kpi-taxa-ocupacao', label: 'Taxa de Ocupação', description: 'Percentual de horários ocupados sobre capacidade total' },
  { id: 'grafico-agenda-status', label: 'Agenda por Status', description: 'Distribuição de agendamentos de hoje por status' },
  { id: 'grafico-ocupacao-profissional', label: 'Ocupação por Profissional', description: 'Total vs concluídos por profissional no mês' },
  { id: 'tabela-agenda-hoje', label: 'Agenda de Hoje', description: 'Lista de agendamentos do dia com horário e status' },
  // Contratos
  { id: 'kpi-contratos-ativos', label: 'Contratos Ativos', description: 'Número de contratos em vigor' },
  { id: 'kpi-mrr-contratos', label: 'MRR Contratos', description: 'Receita mensal recorrente de contratos' },
  { id: 'kpi-contratos-vencendo', label: 'Contratos Vencendo', description: 'Contratos que vencem em 30 dias' },
  { id: 'tabela-contratos-vencendo', label: 'Contratos Vencendo (Detalhe)', description: 'Lista de contratos próximos do vencimento' },
  // Cobranças
  { id: 'kpi-cobrancas-receber', label: 'Cobranças a Receber', description: 'Total de cobranças pendentes de pagamento' },
  { id: 'kpi-cobrancas-vencidas', label: 'Cobranças Vencidas (R$)', description: 'Valor total de cobranças em atraso' },
  { id: 'kpi-cobrancas-vencidas-count', label: 'Cobranças Vencidas (Qtd)', description: 'Quantidade de cobranças em atraso' },
  { id: 'grafico-aging-cobrancas', label: 'Aging de Cobranças', description: 'Distribuição de cobranças vencidas por faixa de atraso' },
  { id: 'tabela-cobrancas-vencidas', label: 'Cobranças Vencidas (Detalhe)', description: 'Lista de cobranças em atraso com dias de atraso' },
  // Orçamentos
  { id: 'kpi-orcamentos-abertos', label: 'Orçamentos Abertos', description: 'Orçamentos enviados ou aprovados aguardando' },
  { id: 'kpi-taxa-conversao-orcamentos', label: 'Taxa de Conversão', description: 'Percentual de orçamentos convertidos em vendas' },
  { id: 'kpi-pipeline', label: 'Pipeline de Vendas', description: 'Valor total dos orçamentos em aberto' },
  { id: 'grafico-orcamentos-status', label: 'Orçamentos por Status', description: 'Distribuição de orçamentos por status' },
  // Assinaturas
  { id: 'kpi-assinaturas-ativas', label: 'Assinaturas Ativas', description: 'Número de assinaturas em vigor' },
  { id: 'kpi-mrr-assinaturas', label: 'MRR Assinaturas', description: 'Receita mensal recorrente de assinaturas' },
  { id: 'kpi-churn-mes', label: 'Churn no Mês', description: 'Assinaturas canceladas ou expiradas no mês' },
  { id: 'kpi-novas-assinaturas', label: 'Novas Assinaturas', description: 'Assinaturas iniciadas no mês' },
  { id: 'grafico-evolucao-assinaturas', label: 'Evolução de Assinaturas', description: 'Ativas, novas e canceladas nos últimos 12 meses' },
]

export function renderWidget(
  id: WidgetId,
  data: DashboardResponse,
  modulos?: ModulosDashboardResponse | null,
  extras?: DashboardExtrasResponse | null,
): React.ReactNode {
  const { kpis } = data
  const saldoMes = kpis.totalReceitasMes - kpis.totalDespesasMes

  switch (id) {
    // ── Financeiro ──────────────────────────────────────────────────
    case 'kpi-saldo-mes':
      return <KpiCard titulo="Saldo do mês" valor={fmt(saldoMes)} icon={Wallet}
        cor={saldoMes >= 0 ? 'green' : 'red'} />
    case 'kpi-receitas-mes':
      return <KpiCard titulo="Receitas do mês" valor={fmt(kpis.totalReceitasMes)} icon={ArrowUpCircle} cor="green" />
    case 'kpi-despesas-mes':
      return <KpiCard titulo="Despesas do mês" valor={fmt(kpis.totalDespesasMes)} icon={DollarSign} />
    case 'kpi-contas-receber':
      return <KpiCard titulo="A receber (pendente)" valor={fmt(kpis.contasReceberPendentes)} icon={ArrowUpCircle} cor="green" />
    case 'kpi-contas-vencidas':
      return <KpiCard titulo="Contas vencidas" valor={fmt(kpis.contasPagarVencidas)} icon={AlertTriangle}
        cor={kpis.contasPagarVencidas > 0 ? 'red' : 'default'} />
    case 'kpi-contas-pagar-proximas':
      return <KpiCard titulo="A pagar (7 dias)" valor={fmt(kpis.contasPagarProximas7Dias)} icon={TrendingDown}
        cor={kpis.contasPagarProximas7Dias > 0 ? 'yellow' : 'default'} />
    case 'kpi-inadimplencia':
      return <KpiCard titulo="Inadimplência" valor={fmtPct(kpis.inadimplencia)} icon={Percent}
        cor={kpis.inadimplencia > 0 ? 'red' : 'default'} />
    case 'kpi-saldo-projetado':
      return <KpiCard titulo="Saldo projetado" valor={fmt(kpis.saldoProjetado)} icon={PiggyBank}
        cor={kpis.saldoProjetado >= 0 ? 'green' : 'red'} />
    case 'grafico-fluxo-caixa':
      return <GraficoFluxo dados={data.fluxoMes ?? []} />
    case 'grafico-despesas-categoria':
      return <GraficoDespesasCategoria dados={data.despesasPorCategoria ?? []} />
    case 'grafico-receitas-categoria':
      return <GraficoReceitasCategoria dados={extras?.receitasPorCategoria ?? []} />
    case 'grafico-fluxo-anual':
      return <GraficoFluxoAnual dados={extras?.fluxoAnual ?? []} />
    case 'tabela-contas-vencidas':
      return <TabelaContasVencidas dados={extras?.contasVencidas ?? []} />
    case 'tabela-proximos-vencimentos':
      return <TabelaProximosVencimentos dados={extras?.proximosVencimentos ?? []} />

    // ── Vendas ───────────────────────────────────────────────────────
    case 'kpi-vendas-hoje':
      return <KpiCard titulo="Vendido hoje" valor={fmt(kpis.totalVendidoHoje)} icon={ShoppingCart} cor="green" />
    case 'kpi-vendas-mes':
      return <KpiCard titulo="Vendido no mês" valor={fmt(kpis.totalVendidoMes)} icon={TrendingUp} />
    case 'kpi-qtd-vendas-mes':
      return <KpiCard titulo="Qtd. de vendas" valor={fmtN(kpis.qtyVendasMes)} icon={BarChart3} />
    case 'kpi-ticket-medio':
      return <KpiCard titulo="Ticket médio" valor={fmt(kpis.ticketMedio)} icon={Star} />
    case 'kpi-lucro-estimado':
      return <KpiCard titulo="Lucro estimado" valor={fmt(kpis.lucroEstimadoMes)} icon={BarChart3}
        cor={kpis.lucroEstimadoMes >= 0 ? 'green' : 'red'} />
    case 'kpi-maior-venda-dia':
      return <KpiCard titulo="Maior venda do dia" valor={fmt(extras?.maiorVendaDia)} icon={Star} cor="green" />
    case 'grafico-tendencia-vendas':
      return <GraficoVendas dados={data.vendasUltimos7Dias ?? []} />
    case 'grafico-vendas-forma-pgto':
      return <GraficoVendasFormaPgto dados={extras?.vendasPorFormaPgto ?? []} />
    case 'tabela-top-produtos':
      return <TopProdutos dados={data.topProdutos ?? []} />
    case 'tabela-ultimas-vendas':
      return <TabelaUltimasVendas dados={extras?.ultimasVendas ?? []} />

    // ── Clientes ─────────────────────────────────────────────────────
    case 'kpi-total-clientes':
      return <KpiCard titulo="Total de clientes" valor={fmtN(kpis.totalClientes)} icon={Users} />
    case 'kpi-clientes-novos-mes':
      return <KpiCard titulo="Clientes novos no mês" valor={fmtN(kpis.clientesNovosMes)} icon={UserPlus} cor="green" />
    case 'kpi-clientes-inativos':
      return <KpiCard titulo="Clientes inativos" valor={fmtN(extras?.clientesInativos)} icon={UserX}
        cor={(extras?.clientesInativos ?? 0) > 0 ? 'yellow' : 'default'} />
    case 'tabela-top-clientes':
      return <TopClientes dados={data.topClientes ?? []} />

    // ── Estoque ──────────────────────────────────────────────────────
    case 'kpi-valor-estoque':
      return <KpiCard titulo="Valor em estoque" valor={fmt(kpis.valorEstoque)} icon={Package} />
    case 'kpi-produtos-ativos':
      return <KpiCard titulo="Produtos ativos" valor={fmtN(extras?.produtosAtivos)} icon={Boxes} />
    case 'alerta-estoque-baixo':
      if (kpis.produtosEstoqueBaixo <= 0) return null
      return (
        <div className="flex items-center gap-2 rounded-md border border-yellow-500/50 bg-yellow-50 p-3 dark:bg-yellow-950/20">
          <PackageX size={18} className="text-yellow-600 shrink-0" />
          <p className="text-sm text-yellow-700 dark:text-yellow-400">
            <strong>{kpis.produtosEstoqueBaixo} produto(s)</strong> com estoque abaixo do mínimo.{' '}
            <Link to="/estoque" className="underline font-medium">Ver estoque →</Link>
          </p>
        </div>
      )
    case 'grafico-distribuicao-categorias':
      return <GraficoDistribuicaoCategorias dados={extras?.distribuicaoCategorias ?? []} />
    case 'tabela-estoque-baixo':
      return <TabelaEstoqueBaixo dados={extras?.estoqueBaixo ?? []} />

    // ── Agendamentos ─────────────────────────────────────────────────
    case 'kpi-agendamentos-hoje':
      return <KpiCard titulo="Agendamentos hoje" valor={fmtN(modulos?.agendamentos?.hoje)} icon={Calendar} />
    case 'kpi-agendamentos-confirmados':
      return <KpiCard titulo="Confirmados hoje" valor={fmtN(modulos?.agendamentos?.confirmadosHoje)} icon={CheckCircle} cor="green" />
    case 'kpi-agendamentos-cancelados':
      return <KpiCard titulo="Cancelados no mês" valor={fmtN(modulos?.agendamentos?.canceladosMes)} icon={XCircle}
        cor={(modulos?.agendamentos?.canceladosMes ?? 0) > 0 ? 'red' : 'default'} />
    case 'kpi-taxa-conclusao-agendamentos':
      return <KpiCard titulo="Taxa de conclusão" valor={fmtPct(modulos?.agendamentos?.taxaConclusaoMes)} icon={Percent}
        cor={(modulos?.agendamentos?.taxaConclusaoMes ?? 0) >= 80 ? 'green' : 'yellow'} />
    case 'kpi-taxa-ocupacao':
      return <KpiCard titulo="Taxa de ocupação" valor={fmtPct(modulos?.agendamentos?.taxaOcupacao)} icon={Percent}
        cor={(modulos?.agendamentos?.taxaOcupacao ?? 0) >= 70 ? 'green' : 'yellow'} />
    case 'grafico-agenda-status':
      return <GraficoAgendaStatus dados={modulos?.agendamentos?.porStatus ?? []} />
    case 'grafico-ocupacao-profissional':
      return <GraficoOcupacaoProfissional dados={modulos?.agendamentos?.porProfissional ?? []} />
    case 'tabela-agenda-hoje':
      return <TabelaAgendaHoje dados={modulos?.agendamentos?.agendaHoje ?? []} />

    // ── Contratos ────────────────────────────────────────────────────
    case 'kpi-contratos-ativos':
      return <KpiCard titulo="Contratos ativos" valor={fmtN(modulos?.contratos?.ativos)} icon={FileText} />
    case 'kpi-mrr-contratos':
      return <KpiCard titulo="MRR contratos" valor={fmt(modulos?.contratos?.mrr)} icon={TrendingUp} cor="green" />
    case 'kpi-contratos-vencendo':
      return <KpiCard titulo="Vencendo em 30 dias" valor={fmtN(modulos?.contratos?.vencendoEm30)} icon={Bell}
        cor={(modulos?.contratos?.vencendoEm30 ?? 0) > 0 ? 'yellow' : 'default'} />
    case 'tabela-contratos-vencendo':
      return <TabelaContratosVencendo dados={modulos?.contratos?.contratosVencendo ?? []} />

    // ── Cobranças ────────────────────────────────────────────────────
    case 'kpi-cobrancas-receber':
      return <KpiCard titulo="Cobranças a receber" valor={fmt(modulos?.cobrancas?.totalReceber)} icon={CreditCard} cor="green" />
    case 'kpi-cobrancas-vencidas':
      return <KpiCard titulo="Cobranças vencidas" valor={fmt(modulos?.cobrancas?.totalVencido)} icon={AlertTriangle}
        cor={(modulos?.cobrancas?.totalVencido ?? 0) > 0 ? 'red' : 'default'} />
    case 'kpi-cobrancas-vencidas-count':
      return <KpiCard titulo="Cobranças em atraso" valor={fmtN(modulos?.cobrancas?.vencidosCount)} icon={AlertTriangle}
        cor={(modulos?.cobrancas?.vencidosCount ?? 0) > 0 ? 'red' : 'default'} />
    case 'grafico-aging-cobrancas':
      return <GraficoAgingCobrancas dados={modulos?.cobrancas?.aging ?? []} />
    case 'tabela-cobrancas-vencidas':
      return <TabelaCobrancasVencidas dados={modulos?.cobrancas?.cobrancasVencidas ?? []} />

    // ── Orçamentos ───────────────────────────────────────────────────
    case 'kpi-orcamentos-abertos':
      return <KpiCard titulo="Orçamentos abertos" valor={fmtN(modulos?.orcamentos?.abertos)} icon={ClipboardList} />
    case 'kpi-taxa-conversao-orcamentos':
      return <KpiCard titulo="Taxa de conversão" valor={fmtPct(modulos?.orcamentos?.taxaConversao)} icon={Percent}
        cor={(modulos?.orcamentos?.taxaConversao ?? 0) >= 50 ? 'green' : 'yellow'} />
    case 'kpi-pipeline':
      return <KpiCard titulo="Pipeline de vendas" valor={fmt(modulos?.orcamentos?.valorPipeline)} icon={TrendingUp} />
    case 'grafico-orcamentos-status':
      return <GraficoOrcamentosStatus dados={modulos?.orcamentos?.porStatus ?? []} />

    // ── Assinaturas ──────────────────────────────────────────────────
    case 'kpi-assinaturas-ativas':
      return <KpiCard titulo="Assinaturas ativas" valor={fmtN(modulos?.assinaturas?.ativas)} icon={Building2} />
    case 'kpi-mrr-assinaturas':
      return <KpiCard titulo="MRR assinaturas" valor={fmt(modulos?.assinaturas?.mrr)} icon={TrendingUp} cor="green" />
    case 'kpi-churn-mes':
      return <KpiCard titulo="Churn no mês" valor={fmtN(modulos?.assinaturas?.canceladasMes)} icon={RefreshCw}
        cor={(modulos?.assinaturas?.canceladasMes ?? 0) > 0 ? 'red' : 'default'} />
    case 'kpi-novas-assinaturas':
      return <KpiCard titulo="Novas assinaturas" valor={fmtN(modulos?.assinaturas?.novasMes)} icon={UserPlus} cor="green" />
    case 'grafico-evolucao-assinaturas':
      return <GraficoEvolucaoAssinaturas dados={modulos?.assinaturas?.evolucao12Meses ?? []} />

    default:
      return null
  }
}

const WIDGET_CATEGORY: Partial<Record<WidgetId, string>> = {
  'kpi-saldo-mes': 'Financeiro', 'kpi-receitas-mes': 'Financeiro', 'kpi-despesas-mes': 'Financeiro',
  'kpi-contas-receber': 'Financeiro', 'kpi-contas-vencidas': 'Financeiro',
  'kpi-contas-pagar-proximas': 'Financeiro', 'kpi-inadimplencia': 'Financeiro',
  'kpi-saldo-projetado': 'Financeiro', 'grafico-fluxo-caixa': 'Financeiro',
  'grafico-despesas-categoria': 'Financeiro', 'grafico-receitas-categoria': 'Financeiro',
  'grafico-fluxo-anual': 'Financeiro', 'tabela-contas-vencidas': 'Financeiro',
  'tabela-proximos-vencimentos': 'Financeiro',
  'kpi-vendas-hoje': 'Vendas', 'kpi-vendas-mes': 'Vendas', 'kpi-qtd-vendas-mes': 'Vendas',
  'kpi-ticket-medio': 'Vendas', 'kpi-lucro-estimado': 'Vendas', 'kpi-maior-venda-dia': 'Vendas',
  'grafico-tendencia-vendas': 'Vendas', 'grafico-vendas-forma-pgto': 'Vendas',
  'tabela-top-produtos': 'Vendas', 'tabela-ultimas-vendas': 'Vendas',
  'kpi-total-clientes': 'Clientes', 'kpi-clientes-novos-mes': 'Clientes',
  'kpi-clientes-inativos': 'Clientes', 'tabela-top-clientes': 'Clientes',
  'kpi-valor-estoque': 'Estoque', 'kpi-produtos-ativos': 'Estoque',
  'alerta-estoque-baixo': 'Estoque', 'grafico-distribuicao-categorias': 'Estoque',
  'tabela-estoque-baixo': 'Estoque',
  'kpi-agendamentos-hoje': 'Agendamentos', 'kpi-agendamentos-confirmados': 'Agendamentos',
  'kpi-agendamentos-cancelados': 'Agendamentos', 'kpi-taxa-conclusao-agendamentos': 'Agendamentos',
  'kpi-taxa-ocupacao': 'Agendamentos', 'grafico-agenda-status': 'Agendamentos',
  'grafico-ocupacao-profissional': 'Agendamentos', 'tabela-agenda-hoje': 'Agendamentos',
  'kpi-contratos-ativos': 'Contratos', 'kpi-mrr-contratos': 'Contratos',
  'kpi-contratos-vencendo': 'Contratos', 'tabela-contratos-vencendo': 'Contratos',
  'kpi-cobrancas-receber': 'Cobranças', 'kpi-cobrancas-vencidas': 'Cobranças',
  'kpi-cobrancas-vencidas-count': 'Cobranças', 'grafico-aging-cobrancas': 'Cobranças',
  'tabela-cobrancas-vencidas': 'Cobranças',
  'kpi-orcamentos-abertos': 'Orçamentos', 'kpi-taxa-conversao-orcamentos': 'Orçamentos',
  'kpi-pipeline': 'Orçamentos', 'grafico-orcamentos-status': 'Orçamentos',
  'kpi-assinaturas-ativas': 'Assinaturas', 'kpi-mrr-assinaturas': 'Assinaturas',
  'kpi-churn-mes': 'Assinaturas', 'kpi-novas-assinaturas': 'Assinaturas',
  'grafico-evolucao-assinaturas': 'Assinaturas',
}

export interface WidgetSection {
  label: string
  kpis: WidgetId[]
  charts: WidgetId[]
  singles: WidgetId[]
}

export function groupWidgets(widgets: WidgetId[]): WidgetSection[] {
  const sections: WidgetSection[] = []
  const seen = new Map<string, WidgetSection>()

  for (const id of widgets) {
    const cat = WIDGET_CATEGORY[id] ?? 'Geral'
    if (!seen.has(cat)) {
      const s: WidgetSection = { label: cat, kpis: [], charts: [], singles: [] }
      seen.set(cat, s)
      sections.push(s)
    }
    const s = seen.get(cat)!
    if (id.startsWith('kpi-')) s.kpis.push(id)
    else if (id.startsWith('grafico-')) s.charts.push(id)
    else s.singles.push(id)
  }

  return sections
}
