import { useEffect, useState } from 'react'
import { Plus, Trash2, Pencil, Layers2 } from 'lucide-react'
import { useFinanceiro } from '@/hooks/useFinanceiro'
import { useAuth } from '@/contexts/AuthContext'
import { Button } from '@/components/ui/button'
import { Badge } from '@/components/ui/badge'
import { Dialog, DialogContent, DialogHeader, DialogTitle } from '@/components/ui/dialog'
import LancamentoForm from '@/components/financeiro/LancamentoForm'
import { useConfirm } from '@/hooks/useConfirm'
import { toast } from '@/hooks/useToast'
import type { CreateLancamentoRequest, LancamentoResponse, LancamentoResumo, UpdateLancamentoRequest } from '@/types/financeiro'
import { KpiRow } from '@/components/ui/KpiRow'
import { useCategoriasLancamento } from '@/hooks/useCategoriasLancamento'

function parseGrupoParcelamento(descricao: string): { base: string; total: number } | null {
  const m = descricao.match(/^(.+)\s(\d+)\/(\d+)$/)
  if (!m) return null
  const total = parseInt(m[3])
  return total >= 2 ? { base: m[1], total } : null
}

function getLancamentosGrupo(todos: LancamentoResponse[], l: LancamentoResponse): LancamentoResponse[] {
  const g = parseGrupoParcelamento(l.descricao)
  if (!g) return []
  return todos.filter(item => {
    if (item.vendaId) return false
    const ig = parseGrupoParcelamento(item.descricao)
    return ig && ig.base === g.base && ig.total === g.total && item.tipo === l.tipo
  })
}

const tipoVariant = (tipo: string) => tipo === 'Receita' ? 'secondary' : 'destructive'
const statusVariant = (s: string, vencido: boolean) =>
  s === 'Pago' ? 'secondary' :
  s === 'Cancelado' ? 'outline' :
  vencido ? 'destructive' : 'outline'

const fmt = (v: number) => v.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' })
const fmtDate = (d: string) => new Date(d).toLocaleDateString('pt-BR')

export default function Lancamentos() {
  const { lancamentos, loading, list, create, pagar, remove, update, fetchResumo } = useFinanceiro()
  const { isAdmin } = useAuth()
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
  const { confirm, ConfirmDialogNode } = useConfirm()

  useEffect(() => {
    void list()
    void fetchResumo().then(setResumo).catch(() => {})
    void listCategorias().then(cats => setTodasCategorias(cats.map(c => c.nome).sort())).catch(() => {})
  }, [list, fetchResumo, listCategorias])

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
    } catch (e) {
      toast.error(e instanceof Error ? e.message : 'Erro ao excluir')
    } finally { setExcluindo(null) }
  }

  async function handleExcluirGrupo(l: LancamentoResponse) {
    const grupo = getLancamentosGrupo(lancamentos, l)
    if (grupo.length === 0) return
    const g = parseGrupoParcelamento(l.descricao)!
    const ok = await confirm({
      title: `Excluir ${grupo.length} parcelas?`,
      description: `Todas as parcelas de "${g.base}" serão removidas permanentemente.`,
      variant: 'destructive',
    })
    if (!ok) return
    setExcluindo(l.id)
    try {
      for (const item of grupo) await remove(item.id)
      toast.success(`${grupo.length} parcelas excluídas`)
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
    } catch (e) {
      toast.error(e instanceof Error ? e.message : 'Erro ao registrar pagamento')
    } finally { setPagando(null) }
  }

  const lancamentosFiltrados = lancamentos.filter(l =>
    (!filtroTipo || l.tipo === filtroTipo) &&
    (!filtroCategoria || l.categoria === filtroCategoria) &&
    (!filtroStatus || l.status === filtroStatus)
  )

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

      {loading ? <p className="text-muted-foreground">Carregando...</p> : lancamentosFiltrados.length === 0 ? (
        <p className="text-center text-muted-foreground py-12">Nenhum lançamento encontrado</p>
      ) : (
        <>
          {/* Mobile: card list */}
          <div className="md:hidden space-y-2">
            {lancamentosFiltrados.map(l => (
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
                        className="text-destructive hover:text-destructive hover:bg-destructive/10"
                        title="Excluir esta parcela">
                        <Trash2 size={13} />
                      </Button>
                    )}
                    {isAdmin && !l.vendaId && getLancamentosGrupo(lancamentos, l).length > 1 && (
                      <Button size="sm" variant="ghost" disabled={!!excluindo}
                        onClick={() => void handleExcluirGrupo(l)}
                        className="text-destructive hover:text-destructive hover:bg-destructive/10 gap-1"
                        title="Excluir todo o parcelamento">
                        <Layers2 size={13} />
                        <span className="text-[11px]">Grupo</span>
                      </Button>
                    )}
                  </div>
                )}
              </div>
            ))}
          </div>

          {/* Desktop: table */}
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
                {lancamentosFiltrados.map(l => (
                  <tr key={l.id} className="border-b">
                    <td className="px-4 py-3 font-medium">{l.descricao}</td>
                    <td className="px-4 py-3">
                      <Badge variant={tipoVariant(l.tipo)}>{l.tipo}</Badge>
                    </td>
                    <td className="px-4 py-3 text-muted-foreground">{l.categoria}</td>
                    <td className="px-4 py-3 text-muted-foreground">{fmtDate(l.dataVencimento)}</td>
                    <td className="px-4 py-3 text-right font-medium">{fmt(l.valor)}</td>
                    <td className="px-4 py-3">
                      <Badge variant={statusVariant(l.status, l.vencido)}>
                        {l.vencido && l.status === 'Pendente' ? 'Vencida' : l.status}
                      </Badge>
                    </td>
                    <td className="px-4 py-3">
                      <div className="flex items-center gap-1">
                        {l.status === 'Pendente' && (
                          <Button size="sm" variant="outline"
                            disabled={pagando === l.id}
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
                          <Button size="sm" variant="ghost"
                            disabled={excluindo === l.id}
                            onClick={() => void handleExcluir(l)}
                            className="text-destructive hover:text-destructive hover:bg-destructive/10"
                            title="Excluir esta parcela">
                            <Trash2 size={14} />
                          </Button>
                        )}
                        {isAdmin && !l.vendaId && getLancamentosGrupo(lancamentos, l).length > 1 && (
                          <Button size="sm" variant="ghost"
                            disabled={!!excluindo}
                            onClick={() => void handleExcluirGrupo(l)}
                            className="text-destructive hover:text-destructive hover:bg-destructive/10 gap-1"
                            title="Excluir todo o parcelamento">
                            <Layers2 size={14} />
                            <span className="text-xs">Grupo</span>
                          </Button>
                        )}
                      </div>
                    </td>
                  </tr>
                ))}
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
                tipo: editandoLanc.tipo as 'Receita' | 'Despesa',
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
