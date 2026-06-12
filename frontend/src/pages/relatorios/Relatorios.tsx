import { useState, useEffect } from 'react'
import { Download } from 'lucide-react'
import { useRelatorios } from '@/hooks/useRelatorios'
import { Button } from '@/components/ui/button'
import FiltrosPeriodo from '@/components/relatorios/FiltrosPeriodo'
import AbaVisaoGeral from '@/components/relatorios/AbaVisaoGeral'
import AbaVendas from '@/components/relatorios/AbaVendas'
import AbaFinanceiro from '@/components/relatorios/AbaFinanceiro'
import AbaEstoque from '@/components/relatorios/AbaEstoque'
import { cn } from '@/lib/utils'

type Aba = 'visao-geral' | 'vendas' | 'financeiro' | 'estoque'
const ABAS: { id: Aba; label: string }[] = [
  { id: 'visao-geral', label: 'Visão Geral' },
  { id: 'vendas', label: 'Vendas' },
  { id: 'financeiro', label: 'Financeiro' },
  { id: 'estoque', label: 'Estoque' },
]

function exportarCSV(nome: string, linhas: string[][]) {
  const csv = linhas.map(l => l.join(';')).join('\n')
  const blob = new Blob(['﻿' + csv], { type: 'text/csv;charset=utf-8;' })
  const url = URL.createObjectURL(blob)
  const a = document.createElement('a')
  a.href = url; a.download = `${nome}.csv`; a.click()
  URL.revokeObjectURL(url)
}

export default function Relatorios() {
  const { kpis, vendas, financeiro, estoque, loading, loadKpis } = useRelatorios()
  const [aba, setAba] = useState<Aba>('visao-geral')
  const [periodo, setPeriodo] = useState({ de: '', ate: '' })

  const hoje = new Date().toISOString().split('T')[0]
  const inicioMes = `${new Date().getFullYear()}-${String(new Date().getMonth() + 1).padStart(2, '0')}-01`

  useEffect(() => {
    void loadKpis(inicioMes, hoje)
    setPeriodo({ de: inicioMes, ate: hoje })
  }, [])

  function handlePeriodo(de: string, ate: string) {
    setPeriodo({ de, ate })
    void loadKpis(de, ate)
  }

  function handleExportar() {
    if (aba === 'vendas' && vendas) {
      exportarCSV(`relatorio-vendas-${periodo.de}-${periodo.ate}`, [
        ['Data', 'Total', 'Quantidade'],
        ...vendas.tendencia.map(d => [d.data, d.total.toString(), d.quantidade.toString()]),
      ])
    } else if (aba === 'financeiro' && financeiro) {
      exportarCSV(`relatorio-financeiro-${periodo.de}-${periodo.ate}`, [
        ['Data', 'Receitas', 'Despesas', 'Saldo'],
        ...financeiro.fluxoPorDia.map(d => [d.data, d.receitas.toString(), d.despesas.toString(), d.saldo.toString()]),
      ])
    } else {
      window.print()
    }
  }

  return (
    <div className="space-y-4 print:space-y-2">
      <div className="flex items-center justify-between print:hidden">
        <h1 className="text-2xl font-bold">Relatórios</h1>
        <Button variant="outline" onClick={handleExportar}>
          <Download size={16} className="mr-2" />
          Exportar {aba === 'vendas' || aba === 'financeiro' ? 'CSV' : 'PDF'}
        </Button>
      </div>

      <div className="rounded-xl border bg-card p-4 space-y-3 print:hidden">
        <FiltrosPeriodo onChange={handlePeriodo} />
        <div className="flex gap-1 border-b">
          {ABAS.map(a => (
            <button key={a.id}
              onClick={() => setAba(a.id)}
              className={cn(
                'px-4 py-2 text-sm font-medium border-b-2 transition-colors',
                aba === a.id
                  ? 'border-primary text-primary'
                  : 'border-transparent text-muted-foreground hover:text-foreground'
              )}>
              {a.label}
            </button>
          ))}
        </div>
      </div>

      {loading ? (
        <div className="flex h-48 items-center justify-center">
          <p className="text-muted-foreground">Carregando relatórios...</p>
        </div>
      ) : (
        <>
          {aba === 'visao-geral' && kpis && <AbaVisaoGeral kpis={kpis} />}
          {aba === 'vendas' && vendas && <AbaVendas dados={vendas} />}
          {aba === 'financeiro' && financeiro && <AbaFinanceiro dados={financeiro} />}
          {aba === 'estoque' && estoque && <AbaEstoque dados={estoque} />}
        </>
      )}
    </div>
  )
}
