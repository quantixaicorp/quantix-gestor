import { useEffect, useState } from 'react'
import { Plus, Search, Pencil, Trash2 } from 'lucide-react'
import { useFornecedores } from '@/hooks/useFornecedores'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Dialog, DialogContent, DialogHeader, DialogTitle } from '@/components/ui/dialog'
import FornecedorForm from '@/components/fornecedores/FornecedorForm'
import { toast } from '@/hooks/useToast'
import type { FornecedorResponse, CreateFornecedorRequest } from '@/types/fornecedores'

export default function Fornecedores() {
  const { fornecedores, loading, list, create, update, remove } = useFornecedores()
  const [busca, setBusca] = useState('')
  const [modalAberto, setModalAberto] = useState(false)
  const [editando, setEditando] = useState<FornecedorResponse | null>(null)
  const [confirmandoDelete, setConfirmandoDelete] = useState<FornecedorResponse | null>(null)

  useEffect(() => { void list() }, [list])

  async function handleCreate(data: CreateFornecedorRequest) {
    try {
      await create(data)
      setModalAberto(false)
      toast.success('Fornecedor cadastrado!')
    } catch (e) {
      toast.error(e instanceof Error ? e.message : 'Erro ao salvar')
    }
  }

  async function handleUpdate(data: CreateFornecedorRequest) {
    if (!editando) return
    try {
      await update(editando.id, data)
      setEditando(null)
      toast.success('Fornecedor atualizado!')
    } catch (e) {
      toast.error(e instanceof Error ? e.message : 'Erro ao salvar')
    }
  }

  async function handleDelete() {
    if (!confirmandoDelete) return
    try {
      await remove(confirmandoDelete.id)
      setConfirmandoDelete(null)
      toast.success('Fornecedor removido!')
    } catch (e) {
      toast.error(e instanceof Error ? e.message : 'Erro ao remover')
    }
  }

  const filtrados = fornecedores.filter(f =>
    f.nome.toLowerCase().includes(busca.toLowerCase()) ||
    (f.cnpjCpf ?? '').includes(busca))

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between">
        <h1 className="text-2xl font-bold">Fornecedores</h1>
        <Button onClick={() => setModalAberto(true)}>
          <Plus size={16} className="mr-2" /> Novo Fornecedor
        </Button>
      </div>

      <div className="relative max-w-sm">
        <Search size={16} className="absolute left-3 top-1/2 -translate-y-1/2 text-muted-foreground" />
        <Input
          className="pl-9"
          placeholder="Buscar por nome ou CNPJ/CPF..."
          value={busca}
          onChange={e => setBusca(e.target.value)}
        />
      </div>

      {loading ? (
        <p className="text-muted-foreground">Carregando...</p>
      ) : (
        <div className="rounded-md border">
          <table className="w-full text-sm">
            <thead>
              <tr className="border-b bg-muted/50">
                <th className="px-4 py-3 text-left font-medium">Nome</th>
                <th className="px-4 py-3 text-left font-medium">CNPJ / CPF</th>
                <th className="px-4 py-3 text-left font-medium">Telefone</th>
                <th className="px-4 py-3 text-left font-medium">Cidade / UF</th>
                <th className="px-4 py-3 text-left font-medium">Contato</th>
                <th className="px-4 py-3 text-left font-medium w-24">Ações</th>
              </tr>
            </thead>
            <tbody>
              {filtrados.map(f => (
                <tr key={f.id} className="border-b hover:bg-muted/30">
                  <td className="px-4 py-3 font-medium">{f.nome}</td>
                  <td className="px-4 py-3 text-muted-foreground">{f.cnpjCpf ?? '—'}</td>
                  <td className="px-4 py-3 text-muted-foreground">{f.telefone ?? '—'}</td>
                  <td className="px-4 py-3 text-muted-foreground">
                    {f.cidade && f.uf ? `${f.cidade} / ${f.uf}` : f.cidade ?? '—'}
                  </td>
                  <td className="px-4 py-3 text-muted-foreground">{f.contato ?? '—'}</td>
                  <td className="px-4 py-3">
                    <div className="flex gap-1">
                      <Button size="icon" variant="ghost" onClick={() => setEditando(f)}>
                        <Pencil size={14} />
                      </Button>
                      <Button size="icon" variant="ghost" className="text-destructive hover:text-destructive" onClick={() => setConfirmandoDelete(f)}>
                        <Trash2 size={14} />
                      </Button>
                    </div>
                  </td>
                </tr>
              ))}
              {filtrados.length === 0 && (
                <tr>
                  <td colSpan={6} className="px-4 py-8 text-center text-muted-foreground">
                    Nenhum fornecedor cadastrado
                  </td>
                </tr>
              )}
            </tbody>
          </table>
        </div>
      )}

      {/* Modal: novo fornecedor */}
      <Dialog open={modalAberto} onOpenChange={setModalAberto}>
        <DialogContent className="max-w-lg max-h-[90vh] overflow-y-auto">
          <DialogHeader><DialogTitle>Novo Fornecedor</DialogTitle></DialogHeader>
          <FornecedorForm onSubmit={handleCreate} onCancel={() => setModalAberto(false)} />
        </DialogContent>
      </Dialog>

      {/* Modal: editar fornecedor */}
      <Dialog open={!!editando} onOpenChange={open => { if (!open) setEditando(null) }}>
        <DialogContent className="max-w-lg max-h-[90vh] overflow-y-auto">
          <DialogHeader><DialogTitle>Editar Fornecedor</DialogTitle></DialogHeader>
          {editando && (
            <FornecedorForm
              defaultValues={{
                nome: editando.nome,
                cnpjCpf: editando.cnpjCpf ?? '',
                telefone: editando.telefone ?? '',
                email: editando.email ?? '',
                contato: editando.contato ?? '',
                logradouro: editando.logradouro ?? '',
                cidade: editando.cidade ?? '',
                uf: editando.uf ?? '',
                cep: editando.cep ?? '',
                observacoes: editando.observacoes ?? '',
              }}
              onSubmit={handleUpdate}
              onCancel={() => setEditando(null)}
            />
          )}
        </DialogContent>
      </Dialog>

      {/* Modal: confirmar exclusão */}
      <Dialog open={!!confirmandoDelete} onOpenChange={open => { if (!open) setConfirmandoDelete(null) }}>
        <DialogContent className="max-w-sm">
          <DialogHeader><DialogTitle>Excluir fornecedor?</DialogTitle></DialogHeader>
          <p className="text-sm text-muted-foreground">
            Tem certeza que deseja excluir <strong>{confirmandoDelete?.nome}</strong>? Esta ação não pode ser desfeita.
          </p>
          <div className="flex justify-end gap-2 pt-2">
            <Button variant="outline" onClick={() => setConfirmandoDelete(null)}>Cancelar</Button>
            <Button variant="destructive" onClick={() => void handleDelete()}>Excluir</Button>
          </div>
        </DialogContent>
      </Dialog>
    </div>
  )
}
