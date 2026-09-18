import { useEffect, useState } from 'react'
import { useConciliacao } from '@/hooks/useConciliacao'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { toast } from '@/hooks/useToast'
import { Trash2 } from 'lucide-react'
import type { BankAccountResponse } from '@/types/conciliacao'

export default function ContasBancarias() {
  const { listAccounts, createAccount, deleteAccount } = useConciliacao()
  const [accounts, setAccounts] = useState<BankAccountResponse[]>([])
  const [loading, setLoading] = useState(true)
  const [name, setName] = useState('')
  const [bankName, setBankName] = useState('')
  const [accountNumber, setAccountNumber] = useState('')
  const [agency, setAgency] = useState('')
  const [saving, setSaving] = useState(false)

  async function load() {
    setLoading(true)
    try { setAccounts(await listAccounts()) }
    catch { toast.error('Erro ao carregar contas') }
    finally { setLoading(false) }
  }

  useEffect(() => { void load() }, [])

  async function handleCreate(e: React.FormEvent) {
    e.preventDefault()
    if (!name.trim() || !bankName.trim()) return
    setSaving(true)
    try {
      await createAccount({ name: name.trim(), bankName: bankName.trim(), accountNumber: accountNumber.trim() || undefined, agency: agency.trim() || undefined })
      setName(''); setBankName(''); setAccountNumber(''); setAgency('')
      await load()
      toast.success('Conta criada')
    } catch { toast.error('Erro ao criar conta') }
    finally { setSaving(false) }
  }

  async function handleDelete(id: string) {
    try { await deleteAccount(id); await load(); toast.success('Conta removida') }
    catch { toast.error('Erro ao remover conta') }
  }

  return (
    <div className="max-w-2xl space-y-6">
      <h1 className="text-2xl font-bold">Contas Bancárias</h1>

      <div className="rounded-xl border bg-card p-6 space-y-4">
        <h2 className="font-semibold text-sm">Nova Conta</h2>
        <form onSubmit={handleCreate} className="grid grid-cols-2 gap-3">
          <div className="space-y-1 col-span-2 sm:col-span-1">
            <Label>Nome da conta *</Label>
            <Input value={name} onChange={e => setName(e.target.value)} placeholder="Ex: Conta Corrente BB" required />
          </div>
          <div className="space-y-1 col-span-2 sm:col-span-1">
            <Label>Banco *</Label>
            <Input value={bankName} onChange={e => setBankName(e.target.value)} placeholder="Ex: Banco do Brasil" required />
          </div>
          <div className="space-y-1">
            <Label>Agência</Label>
            <Input value={agency} onChange={e => setAgency(e.target.value)} placeholder="0001" />
          </div>
          <div className="space-y-1">
            <Label>Conta</Label>
            <Input value={accountNumber} onChange={e => setAccountNumber(e.target.value)} placeholder="12345-6" />
          </div>
          <div className="col-span-2">
            <Button type="submit" disabled={saving}>{saving ? '...' : 'Adicionar Conta'}</Button>
          </div>
        </form>
      </div>

      <div className="rounded-xl border bg-card divide-y">
        {loading ? (
          <p className="p-4 text-sm text-muted-foreground">Carregando...</p>
        ) : accounts.length === 0 ? (
          <p className="p-4 text-sm text-muted-foreground">Nenhuma conta cadastrada.</p>
        ) : accounts.map(a => (
          <div key={a.id} className="flex items-center justify-between px-4 py-3">
            <div>
              <p className="font-medium text-sm">{a.name}</p>
              <p className="text-xs text-muted-foreground">{a.bankName}{a.agency ? ` · Ag. ${a.agency}` : ''}{a.accountNumber ? ` · Cc. ${a.accountNumber}` : ''}</p>
            </div>
            <Button size="icon" variant="ghost" className="h-8 w-8 text-muted-foreground hover:text-destructive"
              onClick={() => handleDelete(a.id)}>
              <Trash2 size={14} />
            </Button>
          </div>
        ))}
      </div>
    </div>
  )
}
