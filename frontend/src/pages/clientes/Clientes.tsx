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
    await update(editando.id, data)
    setEditando(null)
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
      ) : (
        <div className="overflow-x-auto rounded-md border">
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
                <tr key={c.id} className="border-b hover:bg-muted/30 cursor-pointer">
                  <td className="px-4 py-3 font-medium">{c.nome}</td>
                  <td className="px-4 py-3 text-muted-foreground">{c.whatsapp}</td>
                  <td className="px-4 py-3 text-muted-foreground">{c.email ?? '—'}</td>
                  <td className="px-4 py-3 text-muted-foreground">
                    {new Date(c.dataCadastro).toLocaleDateString('pt-BR')}
                  </td>
                  <td className="px-4 py-3">
                    <div className="flex gap-1">
                      <Button size="sm" variant="ghost"
                        onClick={e => { e.stopPropagation(); setEditando(c) }}>
                        <Pencil size={14} />
                      </Button>
                      <Button size="sm" variant="ghost" className="text-destructive hover:text-destructive hover:bg-destructive/10"
                        onClick={e => { e.stopPropagation(); void handleRemove(c) }}>
                        <Trash2 size={14} />
                      </Button>
                    </div>
                  </td>
                </tr>
              ))}
              {filtrados.length === 0 && (
                <tr>
                  <td colSpan={5} className="px-4 py-8 text-center text-muted-foreground">
                    Nenhum cliente encontrado
                  </td>
                </tr>
              )}
            </tbody>
          </table>
        </div>
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
