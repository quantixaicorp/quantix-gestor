// frontend/src/pages/financeiro/ConciliacaoRevisao.tsx
import { useEffect, useState } from 'react'
import { useParams, useNavigate } from 'react-router-dom'
import { useConciliacao } from '@/hooks/useConciliacao'
import { Button } from '@/components/ui/button'
import { Badge } from '@/components/ui/badge'
import { toast } from '@/hooks/useToast'
import { CheckCircle2, XCircle, EyeOff, Undo2, ArrowLeft, Trash2 } from 'lucide-react'
import type { BankStatementItemResponse } from '@/types/conciliacao'

type Tab = 'pendentes' | 'conciliados' | 'novos'

const fmt = (v: number) => Math.abs(v).toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' })
const fmtDate = (s: string) => new Date(s + 'T12:00:00').toLocaleDateString('pt-BR')

function ItemCard({ item, onConfirm, onReject, onIgnore, onUndo, tab }: {
  item: BankStatementItemResponse
  tab: Tab
  onConfirm?: () => void
  onReject?: () => void
  onIgnore?: () => void
  onUndo?: () => void
}) {
  const r = item.reconciliation
  return (
    <div className="rounded-lg border bg-card p-4 space-y-2">
      <div className="flex items-start justify-between gap-2">
        <div className="flex-1 min-w-0">
          <p className="text-sm font-medium truncate">{item.description}</p>
          <p className="text-xs text-muted-foreground">
            {fmtDate(item.date)} · {item.amount > 0 ? '+' : ''}{fmt(item.amount)}
          </p>
        </div>
        {tab === 'pendentes' && r && (
          <Badge variant="outline" className="text-xs shrink-0">
            {r.confidenceScore}% confiança
          </Badge>
        )}
      </div>

      {r && (
        <div className="rounded-md bg-muted/50 px-3 py-2 text-xs space-y-0.5">
          <p className="font-medium text-muted-foreground">Lançamento sugerido</p>
          <p>{r.transactionDescription}</p>
          <p className="text-muted-foreground">
            {fmtDate(r.transactionDueDate)} · {fmt(r.transactionAmount)}
            {r.createdByImport && ' · gerado automaticamente'}
          </p>
        </div>
      )}

      <div className="flex gap-2">
        {tab === 'pendentes' && (
          <>
            <Button size="sm" variant="outline" className="gap-1" onClick={onConfirm}>
              <CheckCircle2 size={13} /> Confirmar
            </Button>
            <Button size="sm" variant="ghost" className="gap-1 text-muted-foreground" onClick={onReject}>
              <XCircle size={13} /> Rejeitar
            </Button>
            <Button size="sm" variant="ghost" className="gap-1 text-muted-foreground" onClick={onIgnore}>
              <EyeOff size={13} /> Ignorar
            </Button>
          </>
        )}
        {tab === 'conciliados' && (
          <Button size="sm" variant="ghost" className="gap-1 text-muted-foreground" onClick={onUndo}>
            <Undo2 size={13} /> Desfazer
          </Button>
        )}
        {tab === 'novos' && r?.createdByImport && (
          <Button size="sm" variant="ghost" className="gap-1 text-muted-foreground" onClick={onReject}>
            <Trash2 size={13} /> Excluir lançamento
          </Button>
        )}
      </div>
    </div>
  )
}

export default function ConciliacaoRevisao() {
  const { id } = useParams<{ id: string }>()
  const navigate = useNavigate()
  const { getStatementItems, manualMatch, undoMatch, ignoreItem } = useConciliacao()
  const [items, setItems] = useState<BankStatementItemResponse[]>([])
  const [loading, setLoading] = useState(true)
  const [tab, setTab] = useState<Tab>('pendentes')

  async function load() {
    if (!id) return
    setLoading(true)
    try { setItems(await getStatementItems(id)) }
    catch { toast.error('Erro ao carregar itens') }
    finally { setLoading(false) }
  }

  useEffect(() => { void load() }, [id])

  async function handleConfirm(item: BankStatementItemResponse) {
    if (!item.reconciliation) return
    try {
      await manualMatch(item.id, item.reconciliation.transactionId)
      await load()
    } catch { toast.error('Erro ao confirmar') }
  }

  async function handleReject(item: BankStatementItemResponse) {
    if (!item.reconciliation) return
    try {
      await undoMatch(item.reconciliation.id)
      await load()
    } catch { toast.error('Erro ao rejeitar') }
  }

  async function handleIgnore(item: BankStatementItemResponse) {
    try { await ignoreItem(item.id); await load() }
    catch { toast.error('Erro ao ignorar') }
  }

  async function handleUndo(item: BankStatementItemResponse) {
    if (!item.reconciliation) return
    try { await undoMatch(item.reconciliation.id); await load() }
    catch { toast.error('Erro ao desfazer') }
  }

  const pendentes = items.filter(i => i.status === 'PendingReview')
  const conciliados = items.filter(i => i.status === 'AutoConciliated')
  const novos = items.filter(i => i.status === 'Unmatched' && i.reconciliation?.createdByImport)

  const tabItems = tab === 'pendentes' ? pendentes : tab === 'conciliados' ? conciliados : novos

  return (
    <div className="space-y-4">
      <div className="flex items-center gap-3">
        <Button variant="ghost" size="icon" onClick={() => navigate('/financeiro/conciliacao')}>
          <ArrowLeft size={16} />
        </Button>
        <h1 className="text-xl font-bold">Revisão de Extrato</h1>
      </div>

      <div className="flex gap-1 border-b">
        {([
          { key: 'pendentes' as Tab, label: 'Pendentes', count: pendentes.length },
          { key: 'conciliados' as Tab, label: 'Conciliados', count: conciliados.length },
          { key: 'novos' as Tab, label: 'Novos Lançamentos', count: novos.length },
        ] as const).map(t => (
          <button
            key={t.key}
            onClick={() => setTab(t.key)}
            className={`px-4 py-2 text-sm font-medium border-b-2 transition-colors ${
              tab === t.key
                ? 'border-primary text-primary'
                : 'border-transparent text-muted-foreground hover:text-foreground'
            }`}
          >
            {t.label}
            {t.count > 0 && (
              <span className="ml-1.5 rounded-full bg-muted px-1.5 py-0.5 text-xs">{t.count}</span>
            )}
          </button>
        ))}
      </div>

      {loading ? (
        <p className="text-sm text-muted-foreground">Carregando...</p>
      ) : tabItems.length === 0 ? (
        <p className="text-sm text-muted-foreground py-6 text-center">
          {tab === 'pendentes' ? 'Nenhum item pendente de revisão.' :
           tab === 'conciliados' ? 'Nenhum item conciliado ainda.' :
           'Nenhum lançamento gerado automaticamente.'}
        </p>
      ) : (
        <div className="space-y-3">
          {tabItems.map(item => (
            <ItemCard
              key={item.id}
              item={item}
              tab={tab}
              onConfirm={() => handleConfirm(item)}
              onReject={() => handleReject(item)}
              onIgnore={() => handleIgnore(item)}
              onUndo={() => handleUndo(item)}
            />
          ))}
        </div>
      )}
    </div>
  )
}
