import { useEffect, useState } from 'react'
import { ChevronDown, ChevronRight, Plus, Pencil, Trash2 } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { ConfirmDialog } from '@/components/ui/confirm-dialog'
import { toast } from '@/hooks/useToast'
import { useContabilidade } from '@/hooks/useContabilidade'
import type { ChartOfAccountResponse, AccountType } from '@/types/contabilidade'

const ACCOUNT_TYPE_LABELS: Record<AccountType, string> = {
  Ativo: 'Ativo',
  Passivo: 'Passivo',
  PatrimonioLiquido: 'Patrimônio Líquido',
  Receita: 'Receita',
  Despesa: 'Despesa',
}

function AccountRow({
  account,
  depth,
  onEdit,
  onDelete,
  onAddChild,
}: {
  account: ChartOfAccountResponse
  depth: number
  onEdit: (a: ChartOfAccountResponse) => void
  onDelete: (id: string) => void
  onAddChild: (parentId: string, type: AccountType) => void
}) {
  const [open, setOpen] = useState(depth < 2)
  const hasChildren = account.children.length > 0

  return (
    <>
      <tr className={account.isActive ? '' : 'opacity-40'}>
        <td className="py-1 px-2" style={{ paddingLeft: `${(depth + 1) * 16}px` }}>
          <div className="flex items-center gap-1">
            {hasChildren
              ? (
                <button onClick={() => setOpen(o => !o)}>
                  {open ? <ChevronDown size={14} /> : <ChevronRight size={14} />}
                </button>
              )
              : <span className="w-4" />}
            <span className="font-mono text-sm text-muted-foreground">{account.code}</span>
            <span className="text-sm ml-2">{account.name}</span>
            {!account.isActive && (
              <span className="ml-1 text-xs px-1.5 py-0.5 rounded-full bg-muted text-muted-foreground border">
                Inativo
              </span>
            )}
          </div>
        </td>
        <td className="py-1 px-2 text-xs text-muted-foreground">
          {ACCOUNT_TYPE_LABELS[account.type]}
        </td>
        <td className="py-1 px-2">
          <div className="flex gap-1">
            <Button size="icon" variant="ghost" className="h-6 w-6"
              onClick={() => onEdit(account)}>
              <Pencil size={12} />
            </Button>
            <Button size="icon" variant="ghost" className="h-6 w-6"
              onClick={() => onAddChild(account.id, account.type)}>
              <Plus size={12} />
            </Button>
            <Button size="icon" variant="ghost" className="h-6 w-6 text-destructive"
              onClick={() => onDelete(account.id)}>
              <Trash2 size={12} />
            </Button>
          </div>
        </td>
      </tr>
      {open && account.children.map(child => (
        <AccountRow key={child.id} account={child} depth={depth + 1}
          onEdit={onEdit} onDelete={onDelete} onAddChild={onAddChild} />
      ))}
    </>
  )
}

export default function PlanoDeContas() {
  const { listAccounts, createAccount, updateAccount, deleteAccount, loadTemplate } = useContabilidade()
  const [accounts, setAccounts] = useState<ChartOfAccountResponse[]>([])
  const [loading, setLoading] = useState(true)
  const [editing, setEditing] = useState<ChartOfAccountResponse | null>(null)
  const [adding, setAdding] = useState<{ parentId: string | null; type: AccountType } | null>(null)
  const [form, setForm] = useState({ code: '', name: '' })
  const [deleteId, setDeleteId] = useState<string | null>(null)

  const load = () => {
    setLoading(true)
    listAccounts()
      .then(setAccounts)
      .catch(() => toast.error('Erro ao carregar plano de contas'))
      .finally(() => setLoading(false))
  }

  useEffect(() => { load() }, [])

  const handleLoadTemplate = async () => {
    try {
      await loadTemplate()
      load()
      toast.success('Modelo padrão carregado com sucesso')
    } catch (e) {
      toast.error(e instanceof Error ? e.message : 'Erro ao carregar modelo')
    }
  }

  const handleSave = async () => {
    try {
      if (editing) {
        await updateAccount(editing.id, { code: form.code, name: form.name })
        toast.success('Conta atualizada')
      } else if (adding) {
        await createAccount({
          code: form.code,
          name: form.name,
          type: adding.type,
          parentId: adding.parentId,
        })
        toast.success('Conta criada')
      }
      setEditing(null)
      setAdding(null)
      setForm({ code: '', name: '' })
      load()
    } catch (e) {
      toast.error(e instanceof Error ? e.message : 'Erro ao salvar conta')
    }
  }

  const handleDelete = async (id: string) => {
    try {
      await deleteAccount(id)
      toast.success('Conta desativada')
      load()
    } catch (e) {
      toast.error(e instanceof Error ? e.message : 'Erro ao desativar conta')
    } finally {
      setDeleteId(null)
    }
  }

  const isEmpty = accounts.length === 0

  return (
    <div className="p-6 max-w-3xl mx-auto">
      <div className="flex items-center justify-between mb-4">
        <h1 className="text-2xl font-bold">Plano de Contas</h1>
        <div className="flex gap-2">
          {isEmpty && (
            <Button variant="outline" onClick={handleLoadTemplate}>
              Carregar Modelo Padrão
            </Button>
          )}
          <Button onClick={() => {
            setAdding({ parentId: null, type: 'Ativo' })
            setForm({ code: '', name: '' })
          }}>
            <Plus size={16} className="mr-1" /> Nova Conta
          </Button>
        </div>
      </div>

      {(editing || adding) && (
        <div className="border rounded-lg p-4 mb-4 bg-muted/30 flex gap-4 items-end">
          <div className="flex-1">
            <Label>Código</Label>
            <Input
              value={form.code}
              onChange={e => setForm(f => ({ ...f, code: e.target.value }))}
              placeholder="ex: 4.1.1"
              className="mt-1"
            />
          </div>
          <div className="flex-1">
            <Label>Nome</Label>
            <Input
              value={form.name}
              onChange={e => setForm(f => ({ ...f, name: e.target.value }))}
              placeholder="ex: Receita de Vendas"
              className="mt-1"
            />
          </div>
          <div className="flex gap-2">
            <Button onClick={handleSave}>Salvar</Button>
            <Button variant="outline" onClick={() => { setEditing(null); setAdding(null) }}>
              Cancelar
            </Button>
          </div>
        </div>
      )}

      {loading ? (
        <p className="text-muted-foreground">Carregando...</p>
      ) : isEmpty ? (
        <p className="text-muted-foreground text-center py-12">
          Nenhuma conta cadastrada. Clique em "Carregar Modelo Padrão" para começar.
        </p>
      ) : (
        <table className="w-full">
          <thead>
            <tr className="text-xs text-muted-foreground border-b">
              <th className="py-2 px-2 text-left">Código / Nome</th>
              <th className="py-2 px-2 text-left">Tipo</th>
              <th className="py-2 px-2" />
            </tr>
          </thead>
          <tbody>
            {accounts.map(a => (
              <AccountRow
                key={a.id}
                account={a}
                depth={0}
                onEdit={acc => { setEditing(acc); setForm({ code: acc.code, name: acc.name }) }}
                onDelete={id => setDeleteId(id)}
                onAddChild={(parentId, type) => {
                  setAdding({ parentId, type })
                  setForm({ code: '', name: '' })
                }}
              />
            ))}
          </tbody>
        </table>
      )}

      <ConfirmDialog
        open={deleteId !== null}
        title="Desativar conta"
        description="Esta conta será marcada como inativa. Deseja continuar?"
        confirmLabel="Desativar"
        variant="destructive"
        onConfirm={() => deleteId && handleDelete(deleteId)}
        onCancel={() => setDeleteId(null)}
      />
    </div>
  )
}
