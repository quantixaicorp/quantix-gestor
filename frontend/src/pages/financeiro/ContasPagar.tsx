import { useEffect, useState, useCallback, useMemo } from 'react'
import { AlertTriangle, ChevronDown, ChevronRight } from 'lucide-react'
import { useFinanceiro } from '@/hooks/useFinanceiro'
import { useCategoriasLancamento } from '@/hooks/useCategoriasLancamento'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { KpiRow } from '@/components/ui/KpiRow'
import { useConfirm } from '@/hooks/useConfirm'
import type { LancamentoResponse } from '@/types/financeiro'

const fmt = (v: number) => v.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' })
const fmtDate = (d: string) => new Date(d + (d.length === 10 ? 'T12:00:00Z' : '')).toLocaleDateString('pt-BR')

type RenderItem =
  | { kind: 'individual'; lancamento: LancamentoResponse }
  | { kind: 'group'; parcelamentoId: string; parcelas: LancamentoResponse[] }

export default function ContasPagar() {
  const { lancamentos, loading, list, pagar } = useFinanceiro()
  const { list: listCategorias } = useCategoriasLancamento()
  const [pagando, setPagando] = useState<string | null>(null)
  const [categorias, setCategorias] = useState<string[]>([])
  const [filtroCategoria, setFiltroCategoria] = useState('')
  const [filtroDe, setFiltroDe] = useState('')
  const [filtroAte, setFiltroAte] = useState('')
  const [expandedGroups, setExpandedGroups] = useState<Set<string>>(new Set())
  const { confirm, ConfirmDialogNode } = useConfirm()

  const reload = useCallback(() => {
    void list({ tipo: 'Despesa', status: 'Pendente' })
  }, [list])

  useEffect(() => {
    reload()
    void listCategorias('Despesa').then(cs => setCategorias(cs.map(c => c.nome).sort())).catch(() => {})
  }, [reload, listCategorias])

  const toggleGroup = useCallback((pid: string) => {
    setExpandedGroups(prev => {
      const next = new Set(prev)
      next.has(pid) ? next.delete(pid) : next.add(pid)
      return next
    })
  }, [])

  const allGroups = useMemo(() => {
    const map = new Map<string, LancamentoResponse[]>()
    for (const l of lancamentos) {
      if (!l.parcelamentoId) continue
      const arr = map.get(l.parcelamentoId) ?? []
      arr.push(l)
      map.set(l.parcelamentoId, arr)
    }
    return map
  }, [lancamentos])

  const matchesFiltro = useCallback((l: LancamentoResponse) => {
    if (filtroCategoria && l.categoria !== filtroCategoria) return false
    if (filtroDe && l.dataVencimento < filtroDe) return false
    if (filtroAte && l.dataVencimento > filtroAte + 'T23:59:59') return false
    return true
  }, [filtroCategoria, filtroDe, filtroAte])

  const renderItems = useMemo<RenderItem[]>(() => {
    const seenGroups = new Set<string>()
    const items: RenderItem[] = []
    for (const l of lancamentos) {
      if (l.parcelamentoId) {
        if (seenGroups.has(l.parcelamentoId)) continue
        const parcelas = allGroups.get(l.parcelamentoId) ?? []
        if (parcelas.some(matchesFiltro)) {
          seenGroups.add(l.parcelamentoId)
          items.push({ kind: 'group', parcelamentoId: l.parcelamentoId, parcelas })
        }
      } else if (matchesFiltro(l)) {
        items.push({ kind: 'individual', lancamento: l })
      }
    }
    return items
  }, [lancamentos, allGroups, matchesFiltro])

  // KPIs calculados sobre lancamentos individuais (todos os valores pendentes filtrados)
  const allFiltered = useMemo(() => lancamentos.filter(matchesFiltro), [lancamentos, matchesFiltro])
  const vencidasCount = allFiltered.filter(l => l.vencido).length
  const totalPendente = allFiltered.reduce((s, l) => s + l.valor, 0)
  const totalVencido = allFiltered.filter(l => l.vencido).reduce((s, l) => s + l.valor, 0)
  const totalProximo = allFiltered.filter(l => !l.vencido).reduce((s, l) => s + l.valor, 0)

  async function handlePagar(l: LancamentoResponse) {
    const ok = await confirm({
      title: 'Confirmar pagamento?',
      description: `${fmt(l.valor)} — ${l.descricao}`,
    })
    if (!ok) return
    setPagando(l.id)
    try {
      await pagar(l.id, { dataPagamento: new Date().toISOString() })
      reload()
    } finally { setPagando(null) }
  }

  function grupoDescricao(parcelas: LancamentoResponse[]) {
    const d = parcelas[0]?.descricao ?? ''
    return d.replace(/\s*-?\s*[Pp]arcela\s+\d+\/\d+$/, '').replace(/\s+\d+\/\d+$/, '').trim()
  }

  function grupoStats(parcelas: LancamentoResponse[]) {
    const pagas = parcelas.filter(p => p.status === 'Pago').length
    const pendentes = parcelas.filter(p => p.status === 'Pendente')
    const vencidas = pendentes.filter(p => p.vencido).length
    const total = pendentes.reduce((s, p) => s + p.valor, 0)
    return { n: parcelas.length, pagas, vencidas, total, pendentes: pendentes.length }
  }

  return (
    <div className="space-y-4">
      <div className="flex flex-wrap items-center justify-between gap-2">
        <h1 className="text-2xl font-bold">Contas a Pagar</h1>
      </div>

      <KpiRow items={[
        { label: 'Total pendente', value: fmt(totalPendente), color: 'text-foreground' },
        { label: 'Vencidas', value: fmt(totalVencido), color: 'text-destructive' },
        { label: 'A vencer', value: fmt(totalProximo), color: 'text-yellow-600 dark:text-yellow-400' },
        { label: 'Qtd contas', value: String(allFiltered.length), color: 'text-muted-foreground' },
      ]} />

      {vencidasCount > 0 && (
        <div className="rounded-md border border-destructive/50 bg-destructive/5 p-3 flex gap-2">
          <AlertTriangle size={18} className="text-destructive mt-0.5 shrink-0" />
          <p className="text-sm text-destructive">
            <strong>{vencidasCount} conta(s) vencida(s)</strong> totalizando {fmt(totalVencido)}.
            Regularize o quanto antes.
          </p>
        </div>
      )}

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

      {loading ? <p className="text-muted-foreground">Carregando...</p> : renderItems.length === 0 ? (
        <p className="text-center text-muted-foreground py-12">Nenhuma conta a pagar</p>
      ) : (
        <div className="space-y-2">
          {renderItems.map(item => {
            if (item.kind === 'individual') {
              const l = item.lancamento
              return (
                <div key={l.id} className={`rounded-lg border bg-card p-4 space-y-2 ${l.vencido ? 'border-destructive/40 bg-destructive/5' : ''}`}>
                  <div className="flex items-start justify-between gap-2">
                    <p className="font-medium truncate flex-1">{l.descricao}</p>
                    <div className="flex items-center gap-2 shrink-0">
                      {l.vencido && <Badge variant="destructive" className="text-xs">Vencida</Badge>}
                    </div>
                  </div>
                  <div className="flex items-center justify-between text-sm">
                    <span className="text-muted-foreground">{l.categoria}</span>
                    <span className="font-semibold">{fmt(l.valor)}</span>
                  </div>
                  <div className="flex items-center justify-between">
                    <span className="text-xs text-muted-foreground">Vence: {fmtDate(l.dataVencimento)}</span>
                    <Button size="sm" variant="outline" disabled={pagando === l.id} onClick={() => void handlePagar(l)}>
                      {pagando === l.id ? '...' : 'Pagar'}
                    </Button>
                  </div>
                </div>
              )
            }

            const { parcelamentoId, parcelas } = item
            const expanded = expandedGroups.has(parcelamentoId)
            const desc = grupoDescricao(parcelas)
            const stats = grupoStats(parcelas)
            const temVencida = stats.vencidas > 0

            return (
              <div key={parcelamentoId} className={`rounded-lg border bg-card overflow-hidden ${temVencida ? 'border-destructive/40' : ''}`}>
                <button
                  className="w-full flex items-center gap-2 p-4 text-left hover:bg-muted/30"
                  onClick={() => toggleGroup(parcelamentoId)}>
                  {expanded
                    ? <ChevronDown size={16} className="shrink-0 text-muted-foreground" />
                    : <ChevronRight size={16} className="shrink-0 text-muted-foreground" />}
                  <div className="flex-1 min-w-0">
                    <p className="font-medium truncate">{desc}</p>
                    <p className="text-xs text-muted-foreground">
                      {stats.pagas}/{stats.n} pagas · {fmt(stats.total)} pendente
                    </p>
                  </div>
                  <Badge variant={temVencida ? 'destructive' : 'outline'} className="shrink-0">
                    {temVencida ? `${stats.vencidas} vencida${stats.vencidas > 1 ? 's' : ''}` : `${stats.pendentes} pendente${stats.pendentes > 1 ? 's' : ''}`}
                  </Badge>
                </button>

                {expanded && (
                  <div className="border-t divide-y">
                    {[...parcelas]
                      .sort((a, b) => (a.numeroParcela ?? 0) - (b.numeroParcela ?? 0))
                      .map(l => (
                        <div key={l.id} className={`p-3 pl-6 space-y-1 ${l.vencido ? 'bg-destructive/5' : ''}`}>
                          <div className="flex items-center justify-between gap-2">
                            <div className="min-w-0 flex-1">
                              <p className="text-sm font-medium truncate">{l.descricao}</p>
                              <p className="text-xs text-muted-foreground">Vence: {fmtDate(l.dataVencimento)}</p>
                            </div>
                            <div className="flex items-center gap-2 shrink-0">
                              <span className="text-sm font-semibold">{fmt(l.valor)}</span>
                              <Badge variant={l.status === 'Pago' ? 'default' : l.vencido ? 'destructive' : 'outline'}>
                                {l.status === 'Pago' ? 'Pago' : l.vencido ? 'Vencida' : 'Pendente'}
                              </Badge>
                            </div>
                          </div>
                          {l.status === 'Pendente' && (
                            <Button size="sm" variant="outline" disabled={pagando === l.id}
                              onClick={() => void handlePagar(l)}>
                              {pagando === l.id ? '...' : 'Pagar'}
                            </Button>
                          )}
                        </div>
                      ))}
                  </div>
                )}
              </div>
            )
          })}
        </div>
      )}
      {ConfirmDialogNode}
    </div>
  )
}
