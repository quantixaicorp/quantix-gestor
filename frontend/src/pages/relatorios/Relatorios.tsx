import { useState, useEffect } from 'react'
import { Download, Loader2 } from 'lucide-react'
import { useRelatorios } from '@/hooks/useRelatorios'
import { useRelatorioLayout } from '@/hooks/useRelatorioLayout'
import { Button } from '@/components/ui/button'
import FiltrosPeriodo from '@/components/relatorios/FiltrosPeriodo'
import AbaVisaoGeral from '@/components/relatorios/AbaVisaoGeral'
import AbaVendas from '@/components/relatorios/AbaVendas'
import AbaFinanceiro from '@/components/relatorios/AbaFinanceiro'
import AbaEstoque from '@/components/relatorios/AbaEstoque'
import AbaClientes from '@/components/relatorios/AbaClientes'
import AbaAgendamentos from '@/components/relatorios/AbaAgendamentos'
import AbaContratos from '@/components/relatorios/AbaContratos'
import AbaCobrancas from '@/components/relatorios/AbaCobrancas'
import AbaOrcamentos from '@/components/relatorios/AbaOrcamentos'
import AbaAssinaturas from '@/components/relatorios/AbaAssinaturas'
import AbaCurvaABC from '@/components/relatorios/AbaCurvaABC'
import AbaDRE from '@/components/relatorios/AbaDRE'
import AbaCompras from '@/components/relatorios/AbaCompras'
import AbaHistoricoClientes from '@/components/relatorios/AbaHistoricoClientes'
import { cn } from '@/lib/utils'
import { useAuth } from '@/contexts/AuthContext'
import type { RelatorioTabId } from '@/types/relatorios'

const TAB_META: Record<RelatorioTabId, string> = {
  'visao-geral': 'Visão Geral',
  'vendas': 'Vendas',
  'financeiro': 'Financeiro',
  'estoque': 'Estoque',
  'clientes': 'Clientes',
  'agendamentos': 'Agendamentos',
  'contratos': 'Contratos',
  'cobrancas': 'Cobranças',
  'orcamentos': 'Orçamentos',
  'assinaturas': 'Assinaturas',
  'curva-abc': 'Curva ABC',
  'dre': 'DRE',
  'compras': 'Compras',
  'historico-clientes': 'Histórico de Clientes',
}

function exportarCSV(nome: string, linhas: string[][]) {
  const csv = linhas.map(l => l.join(';')).join('\n')
  const blob = new Blob(['﻿' + csv], { type: 'text/csv;charset=utf-8;' })
  const url = URL.createObjectURL(blob)
  const a = document.createElement('a')
  a.href = url; a.download = `${nome}.csv`; a.click()
  URL.revokeObjectURL(url)
}

export default function Relatorios() {
  const { data, loadingTab, setPeriodo, loadTab } = useRelatorios()
  const { tabs, load: loadLayout } = useRelatorioLayout()
  const { enabledModules, modulesLoaded } = useAuth()
  // Sem módulo de vendas contratado, oculta KPIs e gráficos de vendas.
  // (vazio/não carregado = sem restrição, igual ao gating do menu)
  const hasVendas = !modulesLoaded || enabledModules.size === 0 || enabledModules.has('vendas')
  const visibleTabs = hasVendas ? tabs : tabs.filter(t => t !== 'vendas')
  const [aba, setAba] = useState<RelatorioTabId | null>(null)
  const [periodo, setPeriodoState] = useState({ de: '', ate: '' })
  const [tipoDataFinanceiro, setTipoDataFinanceiro] = useState('pagamento')

  const hoje = new Date().toISOString().split('T')[0]
  const inicioMes = `${new Date().getFullYear()}-${String(new Date().getMonth() + 1).padStart(2, '0')}-01`

  useEffect(() => { void loadLayout() }, [loadLayout])

  // Quando o layout carrega, ativa a primeira aba e carrega os dados
  useEffect(() => {
    if (visibleTabs.length > 0 && aba === null) {
      const primeiraAba = visibleTabs[0]
      setAba(primeiraAba)
      setPeriodo(inicioMes, hoje)
      setPeriodoState({ de: inicioMes, ate: hoje })
      void loadTab(primeiraAba)
    }
  }, [tabs, hasVendas])

  function handlePeriodo(de: string, ate: string) {
    setPeriodoState({ de, ate })
    setPeriodo(de, ate, tipoDataFinanceiro)
    if (aba) void loadTab(aba)
  }

  function handleTipoDataFinanceiro(v: string) {
    setTipoDataFinanceiro(v)
    setPeriodo(periodo.de, periodo.ate, v)
    void loadTab('financeiro')
  }

  function handleAba(nova: RelatorioTabId) {
    setAba(nova)
    if (nova !== 'compras' && nova !== 'historico-clientes') void loadTab(nova)
  }

  function handleExportar() {
    if (!aba) return
    if (aba === 'vendas' && data.vendas) {
      exportarCSV(`relatorio-vendas-${periodo.de}-${periodo.ate}`, [
        ['Data', 'Total', 'Quantidade'],
        ...data.vendas.tendencia.map(d => [d.data, d.total.toString(), d.quantidade.toString()]),
      ])
    } else if (aba === 'financeiro' && data.financeiro) {
      exportarCSV(`relatorio-financeiro-${periodo.de}-${periodo.ate}`, [
        ['Data', 'Receitas', 'Despesas', 'Saldo'],
        ...data.financeiro.fluxoPorDia.map(d => [d.data, d.receitas.toString(), d.despesas.toString(), d.saldo.toString()]),
      ])
    } else if (aba === 'clientes' && data.clientes) {
      exportarCSV(`relatorio-clientes-${periodo.de}-${periodo.ate}`, [
        ['#', 'Cliente', 'WhatsApp', 'Compras', 'Total'],
        ...data.clientes.topClientes.map((c, i) => [
          String(i + 1), c.nome, c.whatsapp, c.compras.toString(), c.totalGasto.toString(),
        ]),
      ])
    } else if (aba === 'agendamentos' && data.agendamentos) {
      exportarCSV(`relatorio-agendamentos-${periodo.de}-${periodo.ate}`, [
        ['Profissional', 'Total', 'Concluídos', 'Taxa de Conclusão'],
        ...data.agendamentos.porProfissional.map(p => [
          p.profissional, p.total.toString(), p.concluidos.toString(), `${p.taxaConclusao.toFixed(1)}%`,
        ]),
      ])
    } else if (aba === 'contratos' && data.contratos) {
      exportarCSV(`relatorio-contratos`, [
        ['Título', 'Cliente', 'Valor', 'Periodicidade', 'Vencimento', 'Status'],
        ...data.contratos.contratos.map(c => [
          c.titulo, c.clienteNome, c.valor.toString(), c.periodicidade, c.dataFim ?? '—', c.status,
        ]),
      ])
    } else if (aba === 'cobrancas' && data.cobrancas) {
      exportarCSV(`relatorio-cobrancas`, [
        ['Referência', 'Cliente', 'Valor', 'Vencimento', 'Status', 'Dias Atraso'],
        ...data.cobrancas.cobrancas.map(c => [
          c.referencia, c.clienteNome, c.valor.toString(), c.dataVencimento, c.status, c.diasAtraso.toString(),
        ]),
      ])
    } else if (aba === 'orcamentos' && data.orcamentos) {
      exportarCSV(`relatorio-orcamentos-${periodo.de}-${periodo.ate}`, [
        ['#', 'Título', 'Cliente', 'Total', 'Status', 'Criado em'],
        ...data.orcamentos.orcamentos.map(o => [
          String(o.numero), o.titulo, o.clienteNome, o.valorTotal.toString(), o.status, o.criadoEm,
        ]),
      ])
    } else if (aba === 'assinaturas' && data.assinaturas) {
      exportarCSV(`relatorio-assinaturas-${periodo.de}-${periodo.ate}`, [
        ['Cliente', 'Plano', 'Valor', 'Periodicidade', 'Início', 'Renovação', 'Status'],
        ...data.assinaturas.assinaturas.map(a => [
          a.clienteNome, a.plano, a.valor.toString(), a.periodicidade, a.dataInicio, a.dataRenovacao, a.status,
        ]),
      ])
    } else if (aba === 'curva-abc' && data['curva-abc']) {
      exportarCSV(`curva-abc-${periodo.de}-${periodo.ate}`, [
        ['Posição', 'Produto', 'Qtd Vendida', 'Total', '% Individual', '% Acumulado', 'Classe'],
        ...data['curva-abc'].produtos.itens.map((i, idx) => [
          String(idx + 1), i.nome, i.quantidade.toString(),
          i.total.toString(), i.percentual.toFixed(2), i.percentualAcumulado.toFixed(2), i.classe,
        ]),
      ])
    } else if (aba === 'dre' && data.dre) {
      const d = data.dre
      exportarCSV(`dre-${periodo.de}-${periodo.ate}`, [
        ['Descrição', 'Valor'],
        ['Receita Bruta Vendas', d.receitaBrutaVendas.toString()],
        ['Outras Receitas', d.outrasReceitas.toString()],
        ['Descontos Concedidos', `-${d.totalDescontos}`],
        ['Receita Líquida', d.receitaLiquida.toString()],
        ['CMV', `-${d.cmv}`],
        ['Lucro Bruto', d.lucroBruto.toString()],
        ['Margem Bruta (%)', d.margemBruta.toString()],
        ...d.despesasOperacionais.map(x => [x.descricao, `-${x.valor}`]),
        ['Total Despesas Operacionais', `-${d.totalDespesasOperacionais}`],
        ['Resultado Operacional', d.resultadoOperacional.toString()],
        ['Margem Operacional (%)', d.margemOperacional.toString()],
      ])
    } else {
      window.print()
    }
  }

  const isLoading = aba !== 'compras' && aba !== 'historico-clientes' && loadingTab === aba

  return (
    <div className="space-y-4 print:space-y-2">
      <div className="flex items-center justify-between print:hidden">
        <h1 className="text-2xl font-bold">Relatórios</h1>
        <Button variant="outline" onClick={handleExportar} disabled={isLoading}>
          <Download size={16} className="mr-2" />
          Exportar CSV
        </Button>
      </div>

      <div className="rounded-xl border bg-card p-4 space-y-3 print:hidden">
        <FiltrosPeriodo onChange={handlePeriodo} />
        <div className="flex gap-1 border-b overflow-x-auto">
          {visibleTabs.map(id => (
            <button
              key={id}
              onClick={() => handleAba(id)}
              className={cn(
                'px-4 py-2 text-sm font-medium border-b-2 transition-colors whitespace-nowrap',
                aba === id
                  ? 'border-primary text-primary'
                  : 'border-transparent text-muted-foreground hover:text-foreground'
              )}
            >
              {TAB_META[id] ?? id}
              {loadingTab === id && (
                <Loader2 size={12} className="inline ml-1.5 animate-spin" />
              )}
            </button>
          ))}
        </div>
      </div>

      <div className="min-h-48">
        {isLoading ? (
          <div className="flex h-48 items-center justify-center gap-2 text-muted-foreground">
            <Loader2 size={18} className="animate-spin" />
            <span>Carregando...</span>
          </div>
        ) : (
          <>
            {aba === 'visao-geral' && data['visao-geral'] && <AbaVisaoGeral kpis={data['visao-geral']} showVendas={hasVendas} />}
            {aba === 'vendas' && data.vendas && <AbaVendas dados={data.vendas} />}
            {aba === 'financeiro' && data.financeiro && <AbaFinanceiro dados={data.financeiro} tipoData={tipoDataFinanceiro} onChangeTipoData={handleTipoDataFinanceiro} />}
            {aba === 'estoque' && data.estoque && <AbaEstoque dados={data.estoque} />}
            {aba === 'clientes' && data.clientes && <AbaClientes dados={data.clientes} />}
            {aba === 'agendamentos' && data.agendamentos && <AbaAgendamentos dados={data.agendamentos} />}
            {aba === 'contratos' && data.contratos && <AbaContratos dados={data.contratos} />}
            {aba === 'cobrancas' && data.cobrancas && <AbaCobrancas dados={data.cobrancas} />}
            {aba === 'orcamentos' && data.orcamentos && <AbaOrcamentos dados={data.orcamentos} />}
            {aba === 'assinaturas' && data.assinaturas && <AbaAssinaturas dados={data.assinaturas} />}
            {aba === 'curva-abc' && data['curva-abc'] && (
              <AbaCurvaABC produtos={data['curva-abc'].produtos} clientes={data['curva-abc'].clientes} />
            )}
            {aba === 'dre' && data.dre && <AbaDRE dados={data.dre} />}
            {aba === 'compras' && <AbaCompras />}
            {aba === 'historico-clientes' && <AbaHistoricoClientes />}
          </>
        )}
      </div>
    </div>
  )
}
