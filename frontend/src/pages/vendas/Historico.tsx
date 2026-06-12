import { useEffect, useState } from 'react'
import { Trash2, Pencil } from 'lucide-react'
import { useVendas } from '@/hooks/useVendas'
import { useClientes } from '@/hooks/useClientes'
import { useAuth } from '@/contexts/AuthContext'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Dialog, DialogContent, DialogHeader, DialogTitle } from '@/components/ui/dialog'
import { useConfirm } from '@/hooks/useConfirm'
import { toast } from '@/hooks/useToast'
import type { UpdateVendaRequest } from '@/types/vendas'
import { KpiRow } from '@/components/ui/KpiRow'

const statusVariant = (s: string): 'secondary' | 'destructive' | 'outline' =>
  s === 'Concluida' ? 'secondary' :
  s === 'Cancelada' ? 'destructive' : 'outline'

export default function Historico() {
  const { vendas, loading, list, cancelar, remove, update } = useVendas()
  const { clientes, list: listClientes } = useClientes()
  const { isAdmin } = useAuth()
  const [de, setDe] = useState('')
  const [ate, setAte] = useState('')
  const [status, setStatus] = useState('')
  const [cancelando, setCancelando] = useState<string | null>(null)
  const [excluindo, setExcluindo] = useState<string | null>(null)
  const [editandoVenda, setEditandoVenda] = useState<{
    id: string
    clienteId: string | null
    formaPagamento: string
    dataHora: string
  } | null>(null)
  const [salvandoEdit, setSalvandoEdit] = useState(false)
  const { confirm, ConfirmDialogNode } = useConfirm()

  useEffect(() => { void list() }, [list])
  useEffect(() => { void listClientes() }, [listClientes])

  function buscar() { list({ de: de || undefined, ate: ate || undefined, status: status || undefined }) }

  async function handleExcluir(id: string) {
    const ok = await confirm({
      title: 'Excluir venda do histórico?',
      description: 'O estoque será estornado e o lançamento financeiro removido. Esta ação não pode ser desfeita.',
      variant: 'destructive',
    })
    if (!ok) return
    setExcluindo(id)
    try {
      await remove(id)
      toast.success('Venda excluída')
    } catch (e) {
      toast.error(e instanceof Error ? e.message : 'Erro ao excluir')
    } finally { setExcluindo(null) }
  }

  async function handleSalvarEdit(req: UpdateVendaRequest) {
    if (!editandoVenda) return
    setSalvandoEdit(true)
    try {
      await update(editandoVenda.id, req)
      setEditandoVenda(null)
      toast.success('Venda atualizada')
    } catch (e) {
      toast.error(e instanceof Error ? e.message : 'Erro ao atualizar')
    } finally { setSalvandoEdit(false) }
  }

  async function handleCancelar(id: string) {
    const ok = await confirm({
      title: 'Cancelar esta venda?',
      description: 'O estoque será estornado.',
      variant: 'destructive',
    })
    if (!ok) return
    setCancelando(id)
    try {
      await cancelar(id)
      toast.success('Venda cancelada')
    } catch (e) {
      toast.error(e instanceof Error ? e.message : 'Erro ao cancelar')
    } finally { setCancelando(null) }
  }

  const fmt = (v: number) => v.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' })

  const concluidasTotal = vendas.filter(v => v.status === 'Concluida').reduce((s, v) => s + v.total, 0)
  const concluidas = vendas.filter(v => v.status === 'Concluida').length

  return (
    <div className="space-y-4">
      <h1 className="text-2xl font-bold">Histórico de Vendas</h1>

      <KpiRow items={[
        { label: 'Total vendas', value: String(vendas.length) },
        { label: 'Concluídas', value: String(concluidas), color: 'text-green-600 dark:text-green-400' },
        { label: 'Valor total', value: fmt(concluidasTotal), color: 'text-green-600 dark:text-green-400' },
        { label: 'Ticket médio', value: concluidas > 0 ? fmt(concluidasTotal / concluidas) : '—' },
      ]} />

      <div className="flex gap-3 flex-wrap items-end">
        <div className="grid gap-1">
          <label className="text-xs text-muted-foreground">De</label>
          <Input type="date" value={de} onChange={e => setDe(e.target.value)} className="h-8 w-36" />
        </div>
        <div className="grid gap-1">
          <label className="text-xs text-muted-foreground">Até</label>
          <Input type="date" value={ate} onChange={e => setAte(e.target.value)} className="h-8 w-36" />
        </div>
        <select value={status} onChange={e => setStatus(e.target.value)}
          className="h-8 rounded-md border border-input bg-transparent px-3 text-sm">
          <option value="">Todos os status</option>
          <option value="Concluida">Concluída</option>
          <option value="Cancelada">Cancelada</option>
        </select>
        <Button size="sm" onClick={buscar}>Filtrar</Button>
      </div>

      {loading ? (
        <p className="text-muted-foreground">Carregando...</p>
      ) : vendas.length === 0 ? (
        <p className="text-center text-muted-foreground py-12">Nenhuma venda encontrada</p>
      ) : (
        <>
          {/* Mobile: card list */}
          <div className="md:hidden space-y-2">
            {vendas.map(v => (
              <div key={v.id} className="rounded-lg border bg-card p-4 space-y-2">
                <div className="flex items-start justify-between gap-2">
                  <div className="min-w-0 flex-1">
                    <p className="font-medium truncate">{v.clienteNome ?? 'Balcão'}</p>
                    <p className="text-xs text-muted-foreground">{new Date(v.dataHora).toLocaleString('pt-BR')}</p>
                  </div>
                  <Badge variant={statusVariant(v.status)} className="shrink-0">{v.status}</Badge>
                </div>
                <div className="flex items-center justify-between text-sm">
                  <span className="text-muted-foreground">{v.formaPagamento}</span>
                  <span className="font-semibold">{fmt(v.total)}</span>
                </div>
                {(v.status === 'Concluida' || isAdmin) && (
                  <div className="flex gap-1 pt-1">
                    {v.status === 'Concluida' && (
                      <Button size="sm" variant="outline"
                        onClick={() => setEditandoVenda({ id: v.id, clienteId: v.clienteId, formaPagamento: v.formaPagamento, dataHora: v.dataHora })}>
                        <Pencil size={13} />
                      </Button>
                    )}
                    {v.status === 'Concluida' && (
                      <Button size="sm" variant="outline" className="flex-1 text-destructive hover:text-destructive"
                        disabled={cancelando === v.id}
                        onClick={() => handleCancelar(v.id)}>
                        {cancelando === v.id ? '...' : 'Cancelar'}
                      </Button>
                    )}
                    {isAdmin && (
                      <Button size="sm" variant="ghost" disabled={excluindo === v.id}
                        onClick={() => handleExcluir(v.id)}
                        className="text-destructive hover:text-destructive hover:bg-destructive/10">
                        {excluindo === v.id ? '...' : <Trash2 size={13} />}
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
                  <th className="px-4 py-3 text-left font-medium">Data</th>
                  <th className="px-4 py-3 text-left font-medium">Cliente</th>
                  <th className="px-4 py-3 text-left font-medium">Pagamento</th>
                  <th className="px-4 py-3 text-right font-medium">Total</th>
                  <th className="px-4 py-3 text-left font-medium">Status</th>
                  <th className="px-4 py-3" />
                </tr>
              </thead>
              <tbody>
                {vendas.map(v => (
                  <tr key={v.id} className="border-b">
                    <td className="px-4 py-3">{new Date(v.dataHora).toLocaleString('pt-BR')}</td>
                    <td className="px-4 py-3">{v.clienteNome ?? 'Balcão'}</td>
                    <td className="px-4 py-3 text-muted-foreground">{v.formaPagamento}</td>
                    <td className="px-4 py-3 text-right font-medium">{fmt(v.total)}</td>
                    <td className="px-4 py-3">
                      <Badge variant={statusVariant(v.status)}>{v.status}</Badge>
                    </td>
                    <td className="px-4 py-3">
                      <div className="flex items-center gap-2">
                        {v.status === 'Concluida' && (
                          <Button size="sm" variant="ghost"
                            onClick={() => setEditandoVenda({ id: v.id, clienteId: v.clienteId, formaPagamento: v.formaPagamento, dataHora: v.dataHora })}>
                            <Pencil size={14} />
                          </Button>
                        )}
                        {v.status === 'Concluida' && (
                          <Button size="sm" variant="ghost"
                            className="text-destructive hover:text-destructive"
                            disabled={cancelando === v.id}
                            onClick={() => handleCancelar(v.id)}>
                            {cancelando === v.id ? '...' : 'Cancelar'}
                          </Button>
                        )}
                        {isAdmin && (
                          <Button size="sm" variant="ghost"
                            disabled={excluindo === v.id}
                            onClick={() => handleExcluir(v.id)}
                            className="text-destructive hover:text-destructive hover:bg-destructive/10"
                            title="Excluir venda">
                            {excluindo === v.id ? '...' : <Trash2 size={14} />}
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
      {ConfirmDialogNode}
      {editandoVenda && (
        <Dialog open onOpenChange={open => { if (!open) setEditandoVenda(null) }}>
          <DialogContent className="max-w-sm">
            <DialogHeader><DialogTitle>Editar Venda</DialogTitle></DialogHeader>
            <div className="space-y-4">
              <div className="space-y-1.5">
                <Label>Cliente</Label>
                <select
                  value={editandoVenda.clienteId ?? ''}
                  onChange={e => setEditandoVenda(prev => prev ? { ...prev, clienteId: e.target.value || null } : prev)}
                  className="flex h-9 w-full rounded-md border border-input bg-transparent px-3 py-1 text-sm">
                  <option value="">Balcão (sem cliente)</option>
                  {clientes.map(c => <option key={c.id} value={c.id}>{c.nome}</option>)}
                </select>
              </div>
              <div className="space-y-1.5">
                <Label>Forma de pagamento</Label>
                <select
                  value={editandoVenda.formaPagamento}
                  onChange={e => setEditandoVenda(prev => prev ? { ...prev, formaPagamento: e.target.value } : prev)}
                  className="flex h-9 w-full rounded-md border border-input bg-transparent px-3 py-1 text-sm">
                  {[{ v: 'Pix', l: 'Pix' }, { v: 'Dinheiro', l: 'Dinheiro' }, { v: 'Cartao', l: 'Cartão' }, { v: 'Outro', l: 'Outro' }].map(f => <option key={f.v} value={f.v}>{f.l}</option>)}
                </select>
              </div>
              <div className="space-y-1.5">
                <Label>Data da venda</Label>
                <Input type="date"
                  value={editandoVenda.dataHora.slice(0, 10)}
                  onChange={e => setEditandoVenda(prev => prev ? { ...prev, dataHora: e.target.value + 'T12:00:00Z' } : prev)}
                  className="h-9" />
              </div>
              <div className="flex gap-2 pt-2">
                <Button disabled={salvandoEdit}
                  onClick={() => void handleSalvarEdit({ clienteId: editandoVenda.clienteId, formaPagamento: editandoVenda.formaPagamento, dataHora: editandoVenda.dataHora })}>
                  {salvandoEdit ? 'Salvando...' : 'Salvar'}
                </Button>
                <Button variant="outline" onClick={() => setEditandoVenda(null)}>Cancelar</Button>
              </div>
            </div>
          </DialogContent>
        </Dialog>
      )}
    </div>
  )
}
