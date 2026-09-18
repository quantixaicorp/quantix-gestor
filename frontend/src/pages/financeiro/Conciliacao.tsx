import { useEffect, useRef, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { useConciliacao } from '@/hooks/useConciliacao'
import { Button } from '@/components/ui/button'
import { Badge } from '@/components/ui/badge'
import { toast } from '@/hooks/useToast'
import { Upload, Trash2 } from 'lucide-react'
import type { BankAccountResponse, BankStatementListItem } from '@/types/conciliacao'

const fmtDate = (s: string) => new Date(s + 'T12:00:00').toLocaleDateString('pt-BR')

export default function Conciliacao() {
  const navigate = useNavigate()
  const { listAccounts, listStatements, deleteStatement, importStatement } = useConciliacao()
  const [accounts, setAccounts] = useState<BankAccountResponse[]>([])
  const [statements, setStatements] = useState<BankStatementListItem[]>([])
  const [selectedAccount, setSelectedAccount] = useState('')
  const [loading, setLoading] = useState(true)
  const [showModal, setShowModal] = useState(false)
  const [importAccount, setImportAccount] = useState('')
  const [importing, setImporting] = useState(false)
  const fileRef = useRef<HTMLInputElement>(null)

  async function load() {
    setLoading(true)
    try {
      const [accs, stmts] = await Promise.all([
        listAccounts(),
        listStatements(selectedAccount || undefined),
      ])
      setAccounts(accs)
      setStatements(stmts)
    } catch { toast.error('Erro ao carregar dados') }
    finally { setLoading(false) }
  }

  useEffect(() => { void load() }, [selectedAccount])

  async function handleImport() {
    const file = fileRef.current?.files?.[0]
    if (!file || !importAccount) return
    setImporting(true)
    try {
      const stmt = await importStatement(file, importAccount)
      setShowModal(false)
      toast.success(`${stmt.itemCount} transações importadas`)
      navigate(`/financeiro/conciliacao/${stmt.id}`)
    } catch (e) {
      toast.error(e instanceof Error ? e.message : 'Erro ao importar')
    } finally { setImporting(false) }
  }

  async function handleDelete(id: string) {
    try { await deleteStatement(id); await load(); toast.success('Extrato removido') }
    catch { toast.error('Erro ao remover extrato') }
  }

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between">
        <h1 className="text-2xl font-bold">Conciliação Bancária</h1>
        <Button onClick={() => setShowModal(true)}>
          <Upload size={15} className="mr-2" /> Importar Extrato
        </Button>
      </div>

      <div className="flex gap-3 items-center">
        <select
          value={selectedAccount}
          onChange={e => setSelectedAccount(e.target.value)}
          className="flex h-9 rounded-md border border-input bg-transparent px-3 py-1 text-sm"
        >
          <option value="">Todas as contas</option>
          {accounts.map(a => <option key={a.id} value={a.id}>{a.name}</option>)}
        </select>
      </div>

      <div className="rounded-xl border bg-card divide-y">
        {loading ? (
          <p className="p-4 text-sm text-muted-foreground">Carregando...</p>
        ) : statements.length === 0 ? (
          <p className="p-4 text-sm text-muted-foreground">Nenhum extrato importado.</p>
        ) : statements.map(s => (
          <div key={s.id} className="flex items-center justify-between px-4 py-3">
            <div className="flex-1 min-w-0">
              <div className="flex items-center gap-2 flex-wrap">
                <span className="font-medium text-sm truncate">{s.fileName}</span>
                <Badge variant="outline" className="text-xs">{s.format}</Badge>
              </div>
              <p className="text-xs text-muted-foreground mt-0.5">
                {s.bankAccountName} · {fmtDate(s.periodStart)} – {fmtDate(s.periodEnd)}
              </p>
              <div className="flex gap-2 mt-1 flex-wrap">
                {s.autoConciliated > 0 && <Badge variant="secondary">{s.autoConciliated} conciliados</Badge>}
                {s.pendingReview > 0 && <Badge variant="outline" className="border-yellow-400 text-yellow-600">{s.pendingReview} pendentes</Badge>}
                {s.unmatched > 0 && <Badge variant="outline">{s.unmatched} novos lançamentos</Badge>}
              </div>
            </div>
            <div className="flex gap-2 shrink-0">
              <Button size="sm" variant="outline" onClick={() => navigate(`/financeiro/conciliacao/${s.id}`)}>
                Revisar
              </Button>
              <Button size="icon" variant="ghost" className="h-8 w-8 text-muted-foreground hover:text-destructive"
                onClick={() => handleDelete(s.id)}>
                <Trash2 size={14} />
              </Button>
            </div>
          </div>
        ))}
      </div>

      {showModal && (
        <div className="fixed inset-0 bg-black/40 z-50 flex items-center justify-center p-4">
          <div className="bg-card rounded-xl border shadow-xl p-6 w-full max-w-sm space-y-4">
            <h2 className="font-semibold">Importar Extrato</h2>
            <div className="space-y-2">
              <label className="text-sm font-medium">Conta bancária</label>
              <select
                value={importAccount}
                onChange={e => setImportAccount(e.target.value)}
                className="flex h-9 w-full rounded-md border border-input bg-transparent px-3 py-1 text-sm"
              >
                <option value="">Selecionar...</option>
                {accounts.map(a => <option key={a.id} value={a.id}>{a.name}</option>)}
              </select>
            </div>
            <div className="space-y-2">
              <label className="text-sm font-medium">Arquivo (.ofx ou .csv)</label>
              <input ref={fileRef} type="file" accept=".ofx,.csv"
                className="text-sm file:mr-3 file:rounded-md file:border file:px-3 file:py-1 file:text-sm file:font-medium" />
            </div>
            <div className="flex gap-2 justify-end">
              <Button variant="ghost" onClick={() => setShowModal(false)}>Cancelar</Button>
              <Button onClick={handleImport} disabled={importing || !importAccount}>
                {importing ? '...' : 'Importar'}
              </Button>
            </div>
          </div>
        </div>
      )}
    </div>
  )
}
