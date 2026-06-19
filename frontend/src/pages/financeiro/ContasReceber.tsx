import { useEffect, useState, useCallback } from 'react'
import { useFinanceiro } from '@/hooks/useFinanceiro'
import { useCategoriasLancamento } from '@/hooks/useCategoriasLancamento'
import { Badge } from '@/components/ui/badge'
import { KpiRow } from '@/components/ui/KpiRow'

const fmt = (v: number) => v.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' })
const fmtDate = (d: string) => new Date(d).toLocaleDateString('pt-BR')

export default function ContasReceber() {
  const { lancamentos, loading, list } = useFinanceiro()
  const { list: listCategorias } = useCategoriasLancamento()
  const [categorias, setCategorias] = useState<string[]>([])
  const [filtroCategoria, setFiltroCategoria] = useState('')
  const [filtroDe, setFiltroDe] = useState('')
  const [filtroAte, setFiltroAte] = useState('')

  const reload = useCallback(() => {
    void list({ tipo: 'Receita', status: 'Pendente' })
  }, [list])

  useEffect(() => {
    reload()
    void listCategorias('Receita').then(cs => setCategorias(cs.map(c => c.nome).sort())).catch(() => {})
  }, [reload, listCategorias])

  const filtered = lancamentos.filter(l => {
    if (filtroCategoria && l.categoria !== filtroCategoria) return false
    if (filtroDe && l.dataVencimento < filtroDe) return false
    if (filtroAte && l.dataVencimento > filtroAte + 'T23:59:59') return false
    return true
  })

  const vencidas = filtered.filter(l => l.vencido)
  const aVencer = filtered.filter(l => !l.vencido)
  const total = filtered.reduce((s, l) => s + l.valor, 0)
  const totalVencido = vencidas.reduce((s, l) => s + l.valor, 0)
  const totalAVencer = aVencer.reduce((s, l) => s + l.valor, 0)

  return (
    <div className="space-y-4">
      <div className="flex flex-wrap items-center justify-between gap-2">
        <h1 className="text-2xl font-bold">Contas a Receber</h1>
      </div>

      <KpiRow items={[
        { label: 'Total a receber', value: fmt(total), color: 'text-primary' },
        { label: 'Vencidas', value: fmt(totalVencido), color: 'text-destructive' },
        { label: 'A vencer', value: fmt(totalAVencer), color: 'text-green-600 dark:text-green-400' },
        { label: 'Qtd contas', value: String(filtered.length), color: 'text-muted-foreground' },
      ]} />

      {/* Filtros */}
      <div className="rounded-xl border bg-card px-4 py-3">
        <div className="flex flex-wrap gap-2">
          <select value={filtroCategoria} onChange={e => setFiltroCategoria(e.target.value)}
            className="h-8 rounded-md border border-input bg-transparent px-3 text-sm">
            <option value="">Todas as categorias</option>
            {categorias.map(c => <option key={c} value={c}>{c}</option>)}
          </select>
          <div className="flex items-center gap-1">
            <label className="text-xs text-muted-foreground">De</label>
            <input type="date" value={filtroDe} onChange={e => setFiltroDe(e.target.value)}
              className="h-8 rounded-md border border-input bg-transparent px-2 text-sm" />
          </div>
          <div className="flex items-center gap-1">
            <label className="text-xs text-muted-foreground">Até</label>
            <input type="date" value={filtroAte} onChange={e => setFiltroAte(e.target.value)}
              className="h-8 rounded-md border border-input bg-transparent px-2 text-sm" />
          </div>
          {(filtroCategoria || filtroDe || filtroAte) && (
            <button onClick={() => { setFiltroCategoria(''); setFiltroDe(''); setFiltroAte('') }}
              className="h-8 px-3 text-xs text-muted-foreground hover:text-foreground rounded-md border border-input">
              Limpar
            </button>
          )}
        </div>
      </div>

      {loading ? <p className="text-muted-foreground">Carregando...</p> : filtered.length === 0 ? (
        <p className="text-center text-muted-foreground py-12">Nenhuma conta a receber</p>
      ) : (
        <>
          {/* Mobile: card list */}
          <div className="md:hidden space-y-2">
            {filtered.map(l => (
              <div key={l.id} className="rounded-lg border bg-card p-4 space-y-2">
                <div className="flex items-start justify-between gap-2">
                  <p className="font-medium truncate flex-1">{l.descricao}</p>
                  <Badge variant={l.vencido ? 'destructive' : 'outline'} className="shrink-0">
                    {l.vencido ? 'Vencida' : 'Pendente'}
                  </Badge>
                </div>
                <div className="flex items-center justify-between text-sm">
                  <span className="text-muted-foreground">{l.categoria}</span>
                  <span className="font-semibold text-primary">{fmt(l.valor)}</span>
                </div>
                <p className="text-xs text-muted-foreground">Vence: {fmtDate(l.dataVencimento)}</p>
              </div>
            ))}
          </div>

          {/* Desktop: table */}
          <div className="hidden md:block overflow-x-auto rounded-md border">
            <table className="w-full text-sm">
              <thead>
                <tr className="border-b bg-muted/50">
                  <th className="px-4 py-3 text-left font-medium">Descrição</th>
                  <th className="px-4 py-3 text-left font-medium">Categoria</th>
                  <th className="px-4 py-3 text-left font-medium">Vencimento</th>
                  <th className="px-4 py-3 text-right font-medium">Valor</th>
                  <th className="px-4 py-3 text-left font-medium">Status</th>
                </tr>
              </thead>
              <tbody>
                {filtered.map(l => (
                  <tr key={l.id} className="border-b">
                    <td className="px-4 py-3 font-medium">{l.descricao}</td>
                    <td className="px-4 py-3 text-muted-foreground">{l.categoria}</td>
                    <td className="px-4 py-3 text-muted-foreground">{fmtDate(l.dataVencimento)}</td>
                    <td className="px-4 py-3 text-right font-medium">{fmt(l.valor)}</td>
                    <td className="px-4 py-3">
                      <Badge variant={l.vencido ? 'destructive' : 'outline'}>
                        {l.vencido ? 'Vencida' : 'Pendente'}
                      </Badge>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </>
      )}
    </div>
  )
}
