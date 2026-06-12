import { useEffect, useState } from 'react'
import { Plus, Search, Pencil, Trash2 } from 'lucide-react'
import { useClientes } from '@/hooks/useClientes'
import { useConfirm } from '@/hooks/useConfirm'
import { toast } from '@/hooks/useToast'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Dialog, DialogContent, DialogHeader, DialogTitle } from '@/components/ui/dialog'
import ClienteForm from '@/components/clientes/ClienteForm'
import type { CreateClienteRequest, UpdateClienteRequest, ClienteResponse } from '@/types/clientes'
import { KpiRow } from '@/components/ui/KpiRow'

export default function Clientes() {
  const { clientes, loading, list, create, update, remove } = useClientes()
  const [busca, setBusca] = useState('')
  const [modalAberto, setModalAberto] = useState(false)
  const [editando, setEditando] = useState<ClienteResponse | null>(null)
  const { confirm, ConfirmDialogNode } = useConfirm()

  useEffect(() => { list() }, [list])

  async function handleCreate(data: CreateClienteRequest) {
    await create(data)
    setModalAberto(false)
  }

  async function handleEdit(data: UpdateClienteRequest) {
    if (!editando) return
    try {
      await update(editando.id, data)
      setEditando(null)
      toast.success('Cliente atualizado')
    } catch (e) {
      toast.error(e instanceof Error ? e.message : 'Erro ao atualizar cliente')
    }
  }

  async function handleRemove(c: ClienteResponse) {
    const ok = await confirm({
      title: 'Excluir cliente?',
      description: `${c.nome} será removido permanentemente.`,
      variant: 'destructive',
    })
    if (!ok) return
    try {
      await remove(c.id)
      toast.success('Cliente excluído')
    } catch (e) {
      toast.error(e instanceof Error ? e.message : 'Erro ao excluir')
    }
  }

  const filtrados = clientes.filter(c =>
    c.nome.toLowerCase().includes(busca.toLowerCase()) ||
    c.whatsapp.includes(busca))

  return (
    <div className="space-y-4">
      <div className="flex flex-wrap items-center justify-between gap-2">
        <h1 className="text-2xl font-bold">Clientes</h1>
        <Button onClick={() => setModalAberto(true)}>
          <Plus size={16} className="mr-2" /> Novo Cliente
        </Button>
      </div>

      <KpiRow items={[
        { label: 'Total de clientes', value: String(clientes.length) },
        { label: 'Novos este mês', value: String(clientes.filter(c => {
          const d = new Date(c.dataCadastro); const n = new Date()
          return d.getMonth() === n.getMonth() && d.getFullYear() === n.getFullYear()
        }).length) },
        { label: 'Com e-mail', value: String(clientes.filter(c => c.email).length) },
        { label: 'Com WhatsApp', value: String(clientes.filter(c => c.whatsapp).length) },
      ]} />

      <div className="relative max-w-sm">
        <Search size={16} className="absolute left-3 top-1/2 -translate-y-1/2 text-muted-foreground" />
        <Input
          className="pl-9"
          placeholder="Buscar por nome ou WhatsApp..."
          value={busca}
          onChange={e => setBusca(e.target.value)}
        />
      </div>

      {loading ? (
        <p className="text-muted-foreground">Carregando...</p>
      ) : filtrados.length === 0 ? (
        <p className="text-center text-muted-foreground py-12">Nenhum cliente encontrado</p>
      ) : (
        <>
          {/* Mobile: card list */}
          <div className="md:hidden space-y-2">
            {filtrados.map(c => (
              <div key={c.id} className="rounded-lg border bg-card p-4 flex items-start justify-between gap-3">
                <div className="min-w-0 flex-1 space-y-1">
                  <p className="font-medium truncate">{c.nome}</p>
                  <p className="text-sm text-muted-foreground">{c.whatsapp}</p>
                  {c.email && <p className="text-sm text-muted-foreground truncate">{c.email}</p>}
                  <p className="text-xs text-muted-foreground">
                    Cadastrado em {new Date(c.dataCadastro).toLocaleDateString('pt-BR')}
                  </p>
                </div>
                <div className="flex gap-1 shrink-0">
                  <Button size="sm" variant="ghost" onClick={() => setEditando(c)}>
                    <Pencil size={14} />
                  </Button>
                  <Button size="sm" variant="ghost" className="text-destructive hover:text-destructive hover:bg-destructive/10"
                    onClick={() => void handleRemove(c)}>
                    <Trash2 size={14} />
                  </Button>
                </div>
              </div>
            ))}
          </div>

          {/* Desktop: table */}
          <div className="hidden md:block overflow-x-auto rounded-md border">
            <table className="w-full text-sm">
              <thead>
                <tr className="border-b bg-muted/50">
                  <th className="px-4 py-3 text-left font-medium">Nome</th>
                  <th className="px-4 py-3 text-left font-medium">WhatsApp</th>
                  <th className="px-4 py-3 text-left font-medium">E-mail</th>
                  <th className="px-4 py-3 text-left font-medium">Cadastrado em</th>
                  <th className="px-4 py-3" />
                </tr>
              </thead>
              <tbody>
                {filtrados.map(c => (
                  <tr key={c.id} className="border-b hover:bg-muted/30">
                    <td className="px-4 py-3 font-medium">{c.nome}</td>
                    <td className="px-4 py-3 text-muted-foreground">{c.whatsapp}</td>
                    <td className="px-4 py-3 text-muted-foreground">{c.email ?? '—'}</td>
                    <td className="px-4 py-3 text-muted-foreground">
                      {new Date(c.dataCadastro).toLocaleDateString('pt-BR')}
                    </td>
                    <td className="px-4 py-3">
                      <div className="flex gap-1">
                        <Button size="sm" variant="ghost" onClick={() => setEditando(c)}>
                          <Pencil size={14} />
                        </Button>
                        <Button size="sm" variant="ghost" className="text-destructive hover:text-destructive hover:bg-destructive/10"
                          onClick={() => void handleRemove(c)}>
                          <Trash2 size={14} />
                        </Button>
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
        <DialogContent>
          <DialogHeader><DialogTitle>Novo Cliente</DialogTitle></DialogHeader>
          <ClienteForm onSubmit={handleCreate} onCancel={() => setModalAberto(false)} />
        </DialogContent>
      </Dialog>

      <Dialog open={!!editando} onOpenChange={open => { if (!open) setEditando(null) }}>
        <DialogContent>
          <DialogHeader><DialogTitle>Editar Cliente</DialogTitle></DialogHeader>
          {editando && (
            <ClienteForm
              key={editando.id}
              defaultValues={{
                nome: editando.nome,
                whatsapp: editando.whatsapp,
                email: editando.email ?? '',
                observacoes: editando.observacoes ?? '',
              }}
              onSubmit={handleEdit}
              onCancel={() => setEditando(null)}
            />
          )}
        </DialogContent>
      </Dialog>

      {ConfirmDialogNode}
    </div>
  )
}
