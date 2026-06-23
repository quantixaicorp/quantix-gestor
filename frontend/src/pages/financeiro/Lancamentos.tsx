import { useMemo, useCallback, useEffect, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { ChevronRight, ChevronDown, Plus, Trash2, Pencil, ExternalLink } from 'lucide-react'
import { useFinanceiro } from '@/hooks/useFinanceiro'
import { useAuth } from '@/contexts/AuthContext'
import { Button } from '@/components/ui/button'
import { Badge } from '@/components/ui/badge'
import { Dialog, DialogContent, DialogHeader, DialogTitle } from '@/components/ui/dialog'
import LancamentoForm from '@/components/financeiro/LancamentoForm'
import { useConfirm } from '@/hooks/useConfirm'
import { toast } from '@/hooks/useToast'
import { api } from '@/services/api'
import type { CreateLancamentoRequest, LancamentoResponse, LancamentoResumo, UpdateLancamentoRequest } from '@/types/financeiro'
import type { ParcelamentoResponse } from '@/types/parcelamentos'
import { KpiRow } from '@/components/ui/KpiRow'
import { useCategoriasLancamento } from '@/hooks/useCategoriasLancamento'

// ── helpers ──────────────────────────────────────────────────────────────────

const tipoVariant = (tipo: string) => tipo === 'Receita' ? 'secondary' : 'destructive'
const statusVariant = (s: string, vencido: boolean) =>
  s === 'Pago' ? 'secondary' :
  s === 'Cancelado' ? 'outline' :
  vencido ? 'destructive' : 'outline'

const fmt = (v: number) => v.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' })
const fmtDate = (d: string) => new Date(d).toLocaleDateString('pt-BR')

// Extrai o título do grupo a partir da descrição da primeira parcela.
// "Compra #42 - Parcela 2/3" → "Compra #42"
function grupoDescricao(parcelas: LancamentoResponse[]): string {
  const desc = [...parcelas].sort((a, b) =>
    (a.numeroParcela ?? 0) - (b.numeroParcela ?? 0))[0]?.descricao ?? ''
  const m = desc.match(/^(.+)\s[-–]\s(?:Parcela\s)?\d+\/\d+$/)
  return m ? m[1] : desc
}

function grupoStats(parcelas: LancamentoResponse[]) {
  const total = parcelas.reduce((s, p) => s + p.valor, 0)
  const pagas = parcelas.filter(p => p.status === 'Pago').length
  const pendentes = parcelas.filter(p => p.status === 'Pendente').length
  const vencidas = parcelas.filter(p => p.vencido && p.status === 'Pendente').length
  const n = parcelas.length
  return { total, pagas, pendentes, vencidas, n }
}

function grupoStatusVariant(stats: ReturnType<typeof grupoStats>): 'secondary' | 'destructive' | 'outline' {
  if (stats.pagas === stats.n) return 'secondary'
  if (stats.vencidas > 0) return 'destructive'
  return 'outline'
}

// ── component ─────────────────────────────────────────────────────────────────

type RenderItem =
  | { kind: 'individual'; lancamento: LancamentoResponse }
  | { kind: 'group'; parcelamentoId: string; parcelas: LancamentoResponse[] }

export default function Lancamentos() {
  const { lancamentos, loading, list, create, pagar, remove, update, fetchResumo } = useFinanceiro()
  const { isAdmin } = useAuth()
  const navigate = useNavigate()
  const { list: listCategorias } = useCategoriasLancamento()

  const [modalAberto, setModalAberto] = useState(false)
  const [pagando, setPagando] = useState<string | null>(null)
  const [excluindo, setExcluindo] = useState<string | null>(null)
  const [resumo, setResumo] = useState<LancamentoResumo | null>(null)
  const [editandoLanc, setEditandoLanc] = useState<LancamentoResponse | null>(null)
  const [filtroTipo, setFiltroTipo] = useState('')
  const [filtroCategoria, setFiltroCategoria] = useState('')
  const [filtroStatus, setFiltroStatus] = useState('')
  const [todasCategorias, setTodasCategorias] = useState<string[]>([])
  const [expandedGroups, setExpandedGroups] = useState<Set<string>>(new Set())
  // cache parcelamento → compraId para o link "Ver compra"
  const [parcelamentosCache, setParcelamentosCache] = useState<Record<string, ParcelamentoResponse>>({})
  const { confirm, ConfirmDialogNode } = useConfirm()

  useEffect(() => {
    void list()
    void fetchResumo().then(setResumo).catch(() => {})
    void listCategorias().then(cats => setTodasCategorias(cats.map(c => c.nome).sort())).catch(() => {})
  }, [list, fetchResumo, listCategorias])

  // ── agrupamento ──────────────────────────────────────────────────────────

  // Mapeia parcelamentoId → todas as parcelas (sem filtro de status)
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

  // Itens de render: grupos e individuais, respeitando filtros
  const renderItems = useMemo<RenderItem[]>(() => {
    const seenGroups = new Set<string>()
    const items: RenderItem[] = []

    for (const l of lancamentos) {
      const matchesFiltro = (x: LancamentoResponse) =>
        (!filtroTipo || x.tipo === filtroTipo) &&
        (!filtroCategoria || x.categoria === filtroCategoria) &&
        (!filtroStatus || x.status === filtroStatus)

      if (l.parcelamentoId) {
        if (seenGroups.has(l.parcelamentoId)) continue
        const parcelas = allGroups.get(l.parcelamentoId) ?? []
        // Mostra o grupo se ao menos uma parcela bate no filtro
        if (parcelas.some(matchesFiltro)) {
          seenGroups.add(l.parcelamentoId)
          items.push({ kind: 'group', parcelamentoId: l.parcelamentoId, parcelas })
        }
      } else if (matchesFiltro(l)) {
        items.push({ kind: 'individual', lancamento: l })
      }
    }

    return items
  }, [lancamentos, allGroups, filtroTipo, filtroCategoria, filtroStatus])

  // ── toggle grupo + lazy fetch compraId ───────────────────────────────────

  const toggleGroup = useCallback(async (pid: string) => {
    setExpandedGroups(prev => {
      const next = new Set(prev)
      next.has(pid) ? next.delete(pid) : next.add(pid)
      return next
    })
    if (!parcelamentosCache[pid]) {
      try {
        const p = await api.get<ParcelamentoResponse>(`/api/parcelamentos/${pid}`)
        setParcelamentosCache(prev => ({ ...prev, [pid]: p }))
      } catch { /* ignora — sem link "Ver compra" */ }
    }
  }, [parcelamentosCache])

  // ── ações ─────────────────────────────────────────────────────────────────

  async function handleCreate(data: CreateLancamentoRequest) {
    await create(data)
  }

  function handleAllCreated(count: number) {
    setModalAberto(false)
    toast.success(count > 1 ? `${count} parcelas criadas` : 'Lançamento criado')
    void list()
    void fetchResumo().then(setResumo).catch(() => {})
  }

  async function handleEditLanc(data: CreateLancamentoRequest) {
    if (!editandoLanc) return
    try {
      await update(editandoLanc.id, data as UpdateLancamentoRequest)
      setEditandoLanc(null)
      toast.success('Lançamento atualizado')
    } catch (e) {
      toast.error(e instanceof Error ? e.message : 'Erro ao atualizar')
    }
  }

  async function handleExcluir(l: LancamentoResponse) {
    const ok = await confirm({
      title: 'Excluir lançamento?',
      description: `Esta ação não pode ser desfeita. ${l.descricao} — ${fmt(l.valor)}`,
    })
    if (!ok) return
    setExcluindo(l.id)
    try {
      await remove(l.id)
      toast.success('Lançamento excluído')
      void list()
      void fetchResumo().then(setResumo).catch(() => {})
    } catch (e) {
      toast.error(e instanceof Error ? e.message : 'Erro ao excluir')
    } finally { setExcluindo(null) }
  }

  async function handleExcluirGrupo(parcelas: LancamentoResponse[], descricao: string) {
    const ok = await confirm({
      title: `Excluir ${parcelas.length} parcelas?`,
      description: `Todas as parcelas de "${descricao}" serão removidas permanentemente.`,
      variant: 'destructive',
    })
    if (!ok) return
    setExcluindo(parcelas[0].id)
    try {
      for (const item of parcelas) await remove(item.id)
      toast.success(`${parcelas.length} parcelas excluídas`)
      void list()
      void fetchResumo().then(setResumo).catch(() => {})
    } catch (e) {
      toast.error(e instanceof Error ? e.message : 'Erro ao excluir')
    } finally { setExcluindo(null) }
  }

  async function handlePagar(l: LancamentoResponse) {
    const ok = await confirm({
      title: 'Confirmar pagamento?',
      description: `${fmt(l.valor)} — ${l.descricao}`,
    })
    if (!ok) return
    setPagando(l.id)
    try {
      await pagar(l.id, { dataPagamento: new Date().toISOString() })
      toast.success('Pagamento registrado')
      void list()
      void fetchResumo().then(setResumo).catch(() => {})
    } catch (e) {
      toast.error(e instanceof Error ? e.message : 'Erro ao registrar pagamento')
    } finally { setPagando(null) }
  }

  // ── render helpers ────────────────────────────────────────────────────────

  function renderAcoesParcela(l: LancamentoResponse) {
    return (
      <div className="flex items-center gap-1 justify-end">
        {l.status === 'Pendente' && (
          <Button size="sm" variant="outline" disabled={pagando === l.id}
            onClick={() => void handlePagar(l)}>
            {pagando === l.id ? '...' : l.tipo === 'Receita' ? 'Receber' : 'Pagar'}
          </Button>
        )}
        {l.status === 'Pendente' && !l.vendaId && (
          <Button size="sm" variant="ghost" onClick={() => setEditandoLanc(l)}>
            <Pencil size={14} />
          </Button>
        )}
        {isAdmin && !l.vendaId && (
          <Button size="sm" variant="ghost" disabled={excluindo === l.id}
            onClick={() => void handleExcluir(l)}
            className="text-destructive hover:text-destructive hover:bg-destructive/10">
            <Trash2 size={14} />
          </Button>
        )}
      </div>
    )
  }

  // ── render ────────────────────────────────────────────────────────────────

  return (
    <div className="space-y-4">
      <div className="flex flex-wrap items-center justify-between gap-2">
        <h1 className="text-2xl font-bold">Lançamentos</h1>
        <Button onClick={() => setModalAberto(true)}>
          <Plus size={16} className="mr-2" /> Novo Lançamento
        </Button>
      </div>

      {resumo && (
        <KpiRow items={[
          { label: 'Receitas pagas', value: fmt(resumo.totalReceitasMes), color: 'text-green-600 dark:text-green-400' },
          { label: 'Despesas pagas', value: fmt(resumo.totalDespesasMes), color: 'text-red-600 dark:text-red-400' },
          { label: 'Saldo do mês', value: fmt(resumo.saldoMes), color: resumo.saldoMes >= 0 ? 'text-green-600 dark:text-green-400' : 'text-red-600 dark:text-red-400' },
          { label: 'Pendentes', value: fmt(resumo.totalPendente), color: 'text-yellow-600 dark:text-yellow-400' },
        ]} />
      )}

      <div className="rounded-xl border bg-card px-4 py-3">
        <div className="flex flex-wrap gap-2">
          <select value={filtroTipo} onChange={e => setFiltroTipo(e.target.value)}
            className="h-8 rounded-md border border-input bg-transparent px-3 text-sm">
            <option value="">Todos os tipos</option>
            <option value="Receita">Receita</option>
            <option value="Despesa">Despesa</option>
          </select>
          <select value={filtroStatus} onChange={e => setFiltroStatus(e.target.value)}
            className="h-8 rounded-md border border-input bg-transparent px-3 text-sm">
            <option value="">Todos os status</option>
            <option value="Pendente">Pendente</option>
            <option value="Pago">Pago</option>
            <option value="Cancelado">Cancelado</option>
          </select>
          <select value={filtroCategoria} onChange={e => setFiltroCategoria(e.target.value)}
            className="h-8 rounded-md border border-input bg-transparent px-3 text-sm">
            <option value="">Todas as categorias</option>
            {todasCategorias.map(c => <option key={c} value={c}>{c}</option>)}
          </select>
          {(filtroTipo || filtroCategoria || filtroStatus) && (
            <button onClick={() => { setFiltroTipo(''); setFiltroCategoria(''); setFiltroStatus('') }}
              className="h-8 px-3 text-xs text-muted-foreground hover:text-foreground rounded-md border border-input">
              Limpar filtros
            </button>
          )}
        </div>
      </div>

      {loading ? (
        <p className="text-muted-foreground">Carregando...</p>
      ) : renderItems.length === 0 ? (
        <p className="text-center text-muted-foreground py-12">Nenhum lançamento encontrado</p>
      ) : (
        <>
          {/* ── Mobile ── */}
          <div className="md:hidden space-y-2">
            {renderItems.map(item => {
              if (item.kind === 'individual') {
                const l = item.lancamento
                return (
                  <div key={l.id} className="rounded-lg border bg-card p-4 space-y-2">
                    <div className="flex items-start justify-between gap-2">
                      <div className="min-w-0 flex-1">
                        <p className="font-medium truncate">{l.descricao}</p>
                        <p className="text-xs text-muted-foreground">{l.categoria}</p>
                      </div>
                      <div className="flex flex-col items-end gap-1 shrink-0">
                        <Badge variant={tipoVariant(l.tipo)}>{l.tipo}</Badge>
                        <Badge variant={statusVariant(l.status, l.vencido)}>
                          {l.vencido && l.status === 'Pendente' ? 'Vencida' : l.status}
                        </Badge>
                      </div>
                    </div>
                    <div className="flex items-center justify-between text-sm">
                      <span className="text-muted-foreground">Vence: {fmtDate(l.dataVencimento)}</span>
                      <span className={`font-semibold ${l.tipo === 'Receita' ? 'text-green-600 dark:text-green-400' : 'text-destructive'}`}>
                        {fmt(l.valor)}
                      </span>
                    </div>
                    {(l.status === 'Pendente' || (isAdmin && !l.vendaId)) && (
                      <div className="flex gap-1 pt-1 flex-wrap">
                        {l.status === 'Pendente' && (
                          <Button size="sm" variant="outline" className="flex-1"
                            disabled={pagando === l.id}
                            onClick={() => void handlePagar(l)}>
                            {pagando === l.id ? '...' : l.tipo === 'Receita' ? 'Receber' : 'Pagar'}
                          </Button>
                        )}
                        {l.status === 'Pendente' && !l.vendaId && (
                          <Button size="sm" variant="ghost" onClick={() => setEditandoLanc(l)}>
                            <Pencil size={13} />
                          </Button>
                        )}
                        {isAdmin && !l.vendaId && (
                          <Button size="sm" variant="ghost" disabled={excluindo === l.id}
                            onClick={() => void handleExcluir(l)}
                            className="text-destructive hover:text-destructive hover:bg-destructive/10">
                            <Trash2 size={13} />
                          </Button>
                        )}
                      </div>
                    )}
                  </div>
                )
              }

              // grupo
              const { parcelamentoId, parcelas } = item
              const expanded = expandedGroups.has(parcelamentoId)
              const desc = grupoDescricao(parcelas)
              const stats = grupoStats(parcelas)
              const cached = parcelamentosCache[parcelamentoId]

              return (
                <div key={parcelamentoId} className="rounded-lg border bg-card overflow-hidden">
                  {/* cabeçalho do grupo */}
                  <button
                    className="w-full flex items-center gap-2 p-4 text-left hover:bg-muted/30"
                    onClick={() => void toggleGroup(parcelamentoId)}>
                    {expanded ? <ChevronDown size={16} className="shrink-0 text-muted-foreground" /> : <ChevronRight size={16} className="shrink-0 text-muted-foreground" />}
                    <div className="flex-1 min-w-0">
                      <p className="font-medium truncate">{desc}</p>
                      <p className="text-xs text-muted-foreground">
                        {stats.pagas}/{stats.n} pagas · {fmt(stats.total)}
                      </p>
                    </div>
                    <Badge variant={grupoStatusVariant(stats)} className="shrink-0">
                      {stats.pagas === stats.n ? 'Quitado' : stats.vencidas > 0 ? 'Vencido' : `${stats.n - stats.pagas} pendente${stats.n - stats.pagas > 1 ? 's' : ''}`}
                    </Badge>
                  </button>

                  {/* parcelas expandidas */}
                  {expanded && (
                    <div className="border-t divide-y">
                      {[...parcelas]
                        .sort((a, b) => (a.numeroParcela ?? 0) - (b.numeroParcela ?? 0))
                        .map(l => (
                          <div key={l.id} className="p-3 pl-6 space-y-1">
                            <div className="flex items-center justify-between gap-2">
                              <div className="min-w-0 flex-1">
                                <p className="text-sm font-medium truncate">{l.descricao}</p>
                                <p className="text-xs text-muted-foreground">Vence: {fmtDate(l.dataVencimento)}</p>
                              </div>
                              <div className="flex items-center gap-2 shrink-0">
                                <span className={`text-sm font-semibold ${l.tipo === 'Receita' ? 'text-green-600 dark:text-green-400' : 'text-destructive'}`}>{fmt(l.valor)}</span>
                                <Badge variant={statusVariant(l.status, l.vencido)}>
                                  {l.vencido && l.status === 'Pendente' ? 'Vencida' : l.status}
                                </Badge>
                              </div>
                            </div>
                            <div className="flex gap-1">
                              {l.status === 'Pendente' && (
                                <Button size="sm" variant="outline" disabled={pagando === l.id}
                                  onClick={() => void handlePagar(l)}>
                                  {pagando === l.id ? '...' : l.tipo === 'Receita' ? 'Receber' : 'Pagar'}
                                </Button>
                              )}
                              {l.status === 'Pendente' && !l.vendaId && (
                                <Button size="sm" variant="ghost" onClick={() => setEditandoLanc(l)}>
                                  <Pencil size={13} />
                                </Button>
                              )}
                              {isAdmin && !l.vendaId && (
                                <Button size="sm" variant="ghost" disabled={excluindo === l.id}
                                  onClick={() => void handleExcluir(l)}
                                  className="text-destructive hover:text-destructive hover:bg-destructive/10">
                                  <Trash2 size={13} />
                                </Button>
                              )}
                            </div>
                          </div>
                        ))}
                      <div className="flex items-center justify-between p-3 pl-6 bg-muted/20">
                        {cached?.compraId ? (
                          <Button size="sm" variant="ghost" className="gap-1 text-xs"
                            onClick={() => navigate(`/compras/${cached.compraId}`)}>
                            <ExternalLink size={12} /> Ver compra
                          </Button>
                        ) : <span />}
                        {isAdmin && (
                          <Button size="sm" variant="ghost"
                            className="text-destructive hover:text-destructive hover:bg-destructive/10 gap-1 text-xs"
                            onClick={() => void handleExcluirGrupo(parcelas, desc)}>
                            <Trash2 size={12} /> Excluir todas
                          </Button>
                        )}
                      </div>
                    </div>
                  )}
                </div>
              )
            })}
          </div>

          {/* ── Desktop ── */}
          <div className="hidden md:block overflow-x-auto rounded-md border">
            <table className="w-full text-sm">
              <thead>
                <tr className="border-b bg-muted/50">
                  <th className="px-4 py-3 text-left font-medium">Descrição</th>
                  <th className="px-4 py-3 text-left font-medium">Tipo</th>
                  <th className="px-4 py-3 text-left font-medium">Categoria</th>
                  <th className="px-4 py-3 text-left font-medium">Vencimento</th>
                  <th className="px-4 py-3 text-right font-medium">Valor</th>
                  <th className="px-4 py-3 text-left font-medium">Status</th>
                  <th className="px-4 py-3" />
                </tr>
              </thead>
              <tbody>
                {renderItems.map(item => {
                  if (item.kind === 'individual') {
                    const l = item.lancamento
                    return (
                      <tr key={l.id} className="border-b hover:bg-muted/20">
                        <td className="px-4 py-3 font-medium">{l.descricao}</td>
                        <td className="px-4 py-3"><Badge variant={tipoVariant(l.tipo)}>{l.tipo}</Badge></td>
                        <td className="px-4 py-3 text-muted-foreground">{l.categoria}</td>
                        <td className="px-4 py-3 text-muted-foreground">{fmtDate(l.dataVencimento)}</td>
                        <td className="px-4 py-3 text-right font-medium">{fmt(l.valor)}</td>
                        <td className="px-4 py-3">
                          <Badge variant={statusVariant(l.status, l.vencido)}>
                            {l.vencido && l.status === 'Pendente' ? 'Vencida' : l.status}
                          </Badge>
                        </td>
                        <td className="px-4 py-3">{renderAcoesParcela(l)}</td>
                      </tr>
                    )
                  }

                  // grupo
                  const { parcelamentoId, parcelas } = item
                  const expanded = expandedGroups.has(parcelamentoId)
                  const desc = grupoDescricao(parcelas)
                  const stats = grupoStats(parcelas)
                  const cached = parcelamentosCache[parcelamentoId]

                  return (
                    <>
                      {/* linha do cabeçalho do grupo */}
                      <tr key={parcelamentoId}
                        className="border-b bg-muted/10 hover:bg-muted/30 cursor-pointer"
                        onClick={() => void toggleGroup(parcelamentoId)}>
                        <td className="px-4 py-3" colSpan={2}>
                          <div className="flex items-center gap-2 font-medium">
                            {expanded
                              ? <ChevronDown size={15} className="text-muted-foreground shrink-0" />
                              : <ChevronRight size={15} className="text-muted-foreground shrink-0" />}
                            {desc}
                            <span className="text-xs text-muted-foreground font-normal">
                              {stats.pagas}/{stats.n} pagas
                            </span>
                          </div>
                        </td>
                        <td className="px-4 py-3 text-muted-foreground text-xs">Parcelamento</td>
                        <td className="px-4 py-3 text-muted-foreground text-xs">
                          {fmtDate(parcelas[0].dataVencimento)}
                        </td>
                        <td className="px-4 py-3 text-right font-medium">{fmt(stats.total)}</td>
                        <td className="px-4 py-3">
                          <Badge variant={grupoStatusVariant(stats)}>
                            {stats.pagas === stats.n ? 'Quitado' : stats.vencidas > 0 ? 'Vencido' : `${stats.n - stats.pagas}/${stats.n} pend.`}
                          </Badge>
                        </td>
                        <td className="px-4 py-3">
                          <div className="flex items-center gap-1 justify-end" onClick={e => e.stopPropagation()}>
                            {cached?.compraId && (
                              <Button size="sm" variant="ghost" className="gap-1 text-xs h-7"
                                onClick={() => navigate(`/compras/${cached.compraId}`)}>
                                <ExternalLink size={12} /> Ver compra
                              </Button>
                            )}
                            {isAdmin && (
                              <Button size="sm" variant="ghost"
                                className="text-destructive hover:text-destructive hover:bg-destructive/10 h-7"
                                title="Excluir todas as parcelas"
                                onClick={() => void handleExcluirGrupo(parcelas, desc)}>
                                <Trash2 size={13} />
                              </Button>
                            )}
                          </div>
                        </td>
                      </tr>

                      {/* linhas das parcelas (expandido) */}
                      {expanded && [...parcelas]
                        .sort((a, b) => (a.numeroParcela ?? 0) - (b.numeroParcela ?? 0))
                        .map(l => (
                          <tr key={l.id} className="border-b bg-muted/5 hover:bg-muted/20">
                            <td className="py-2.5" colSpan={2}>
                              <div className="flex items-center gap-2 pl-10 pr-4">
                                <span className="w-1.5 h-1.5 rounded-full bg-muted-foreground/40 shrink-0" />
                                <span className="font-medium text-sm">{l.descricao}</span>
                              </div>
                            </td>
                            <td className="px-4 py-2.5 text-muted-foreground">{l.categoria}</td>
                            <td className="px-4 py-2.5 text-muted-foreground">{fmtDate(l.dataVencimento)}</td>
                            <td className="px-4 py-2.5 text-right font-medium">{fmt(l.valor)}</td>
                            <td className="px-4 py-2.5">
                              <Badge variant={statusVariant(l.status, l.vencido)}>
                                {l.vencido && l.status === 'Pendente' ? 'Vencida' : l.status}
                              </Badge>
                            </td>
                            <td className="px-4 py-2.5">{renderAcoesParcela(l)}</td>
                          </tr>
                        ))}
                    </>
                  )
                })}
              </tbody>
            </table>
          </div>
        </>
      )}

      <Dialog open={modalAberto} onOpenChange={setModalAberto}>
        <DialogContent className="max-w-lg">
          <DialogHeader><DialogTitle>Novo Lançamento</DialogTitle></DialogHeader>
          <LancamentoForm
            allowParcelamento
            onSubmit={handleCreate}
            onAllCreated={handleAllCreated}
            onCancel={() => setModalAberto(false)}
          />
        </DialogContent>
      </Dialog>

      <Dialog open={!!editandoLanc} onOpenChange={open => { if (!open) setEditandoLanc(null) }}>
        <DialogContent className="max-w-lg">
          <DialogHeader><DialogTitle>Editar Lançamento</DialogTitle></DialogHeader>
          {editandoLanc && (
            <LancamentoForm
              key={editandoLanc.id}
              defaultValues={{
                tipo: editandoLanc.tipo,
                descricao: editandoLanc.descricao,
                valor: editandoLanc.valor.toString(),
                dataVencimento: editandoLanc.dataVencimento.slice(0, 10),
                categoria: editandoLanc.categoria,
                observacao: editandoLanc.observacao ?? undefined,
              }}
              onSubmit={handleEditLanc}
              onCancel={() => setEditandoLanc(null)}
            />
          )}
        </DialogContent>
      </Dialog>

      {ConfirmDialogNode}
    </div>
  )
}
