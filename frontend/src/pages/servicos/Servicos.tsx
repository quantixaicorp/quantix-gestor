import { useEffect, useState, useCallback } from 'react'
import { Plus, Pencil, Trash2 } from 'lucide-react'
import { api } from '@/services/api'
import { useEstoque } from '@/hooks/useEstoque'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Badge } from '@/components/ui/badge'
import { Dialog, DialogContent, DialogHeader, DialogTitle } from '@/components/ui/dialog'
import ServicoCriarForm from '@/components/estoque/ServicoCriarForm'
import ServicoEditForm from '@/components/estoque/ServicoEditForm'
import { useConfirm } from '@/hooks/useConfirm'
import { toast } from '@/hooks/useToast'
import type { ProdutoResponse, CreateProdutoRequest, UpdateProdutoRequest } from '@/types/estoque'

export default function Servicos() {
  const { categorias, listCategorias, createCategoria, createProduto } = useEstoque()
  const [servicos, setServicos] = useState<ProdutoResponse[]>([])
  const [loading, setLoading] = useState(false)
  const [busca, setBusca] = useState('')
  const [modalCriar, setModalCriar] = useState(false)
  const [editando, setEditando] = useState<ProdutoResponse | null>(null)
  const { confirm, ConfirmDialogNode } = useConfirm()

  const carregar = useCallback(async () => {
    setLoading(true)
    try {
      const data = await api.get<ProdutoResponse[]>('/api/produtos')
      setServicos(data.filter(p => p.tipo === 'Servico'))
    } catch (e) {
      toast.error(e instanceof Error ? e.message : 'Erro ao carregar serviços')
    } finally {
      setLoading(false)
    }
  }, [])

  useEffect(() => {
    void carregar()
    listCategorias()
  }, [carregar, listCategorias])

  async function handleCreate(data: CreateProdutoRequest) {
    try {
      await createProduto(data)
      setModalCriar(false)
      await carregar()
    } catch (e) {
      toast.error(e instanceof Error ? e.message : 'Erro ao salvar serviço')
    }
  }

  async function handleUpdate(id: string, data: UpdateProdutoRequest) {
    try {
      await api.put(`/api/produtos/${id}`, data)
      setEditando(null)
      await carregar()
    } catch (e) {
      toast.error(e instanceof Error ? e.message : 'Erro ao salvar serviço')
    }
  }

  async function handleDelete(id: string, nome: string) {
    const ok = await confirm({ title: `Excluir "${nome}"?`, description: 'Esta ação não pode ser desfeita.' })
    if (!ok) return
    try {
      await api.delete(`/api/produtos/${id}`)
      await carregar()
    } catch (e) {
      toast.error(e instanceof Error ? e.message : 'Erro ao excluir serviço')
    }
  }

  const filtrados = servicos.filter(s => s.nome.toLowerCase().includes(busca.toLowerCase()))

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between">
        <h1 className="text-2xl font-bold">Serviços</h1>
        <Button onClick={() => setModalCriar(true)}>
          <Plus size={16} className="mr-2" /> Novo Serviço
        </Button>
      </div>

      <Input
        placeholder="Buscar serviço por nome..."
        value={busca}
        onChange={e => setBusca(e.target.value)}
        className="max-w-sm"
      />

      {loading ? (
        <p className="text-muted-foreground">Carregando...</p>
      ) : (
        <div className="rounded-md border">
          <table className="w-full text-sm">
            <thead>
              <tr className="border-b bg-muted/50">
                <th className="px-4 py-3 text-left font-medium">Nome</th>
                <th className="px-4 py-3 text-left font-medium">Categoria</th>
                <th className="px-4 py-3 text-right font-medium">Preço</th>
                <th className="px-4 py-3 text-right font-medium">Duração</th>
                <th className="px-4 py-3 text-left font-medium">Status</th>
                <th className="px-4 py-3" />
              </tr>
            </thead>
            <tbody>
              {filtrados.map(s => (
                <tr key={s.id} className="border-b last:border-b-0">
                  <td className="px-4 py-3 font-medium">{s.nome}</td>
                  <td className="px-4 py-3 text-muted-foreground">{s.categoriaNome}</td>
                  <td className="px-4 py-3 text-right">
                    {s.precoVenda.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' })}
                  </td>
                  <td className="px-4 py-3 text-right text-muted-foreground">
                    {s.duracaoMinutos ? `${s.duracaoMinutos} min` : '—'}
                  </td>
                  <td className="px-4 py-3">
                    <Badge variant={s.ativo ? 'secondary' : 'outline'}>
                      {s.ativo ? 'Ativo' : 'Inativo'}
                    </Badge>
                  </td>
                  <td className="px-4 py-3">
                    <div className="flex gap-1 justify-end">
                      <Button size="sm" variant="outline" onClick={() => setEditando(s)}>
                        <Pencil size={14} />
                      </Button>
                      <Button size="sm" variant="outline" className="text-destructive hover:text-destructive"
                        onClick={() => handleDelete(s.id, s.nome)}>
                        <Trash2 size={14} />
                      </Button>
                    </div>
                  </td>
                </tr>
              ))}
              {filtrados.length === 0 && (
                <tr>
                  <td colSpan={6} className="px-4 py-8 text-center text-muted-foreground">
                    Nenhum serviço encontrado
                  </td>
                </tr>
              )}
            </tbody>
          </table>
        </div>
      )}

      <Dialog open={modalCriar} onOpenChange={setModalCriar}>
        <DialogContent className="max-w-lg">
          <DialogHeader><DialogTitle>Novo Serviço</DialogTitle></DialogHeader>
          <ServicoCriarForm
            categorias={categorias}
            onSubmit={handleCreate}
            onCancel={() => setModalCriar(false)}
            onCreateCategoria={createCategoria}
          />
        </DialogContent>
      </Dialog>

      <Dialog open={!!editando} onOpenChange={open => { if (!open) setEditando(null) }}>
        <DialogContent className="max-w-lg">
          <DialogHeader><DialogTitle>Editar Serviço</DialogTitle></DialogHeader>
          {editando && (
            <ServicoEditForm
              key={editando.id}
              servico={editando}
              categorias={categorias}
              onSubmit={handleUpdate}
              onCancel={() => setEditando(null)}
            />
          )}
        </DialogContent>
      </Dialog>

      {ConfirmDialogNode}
    </div>
  )
}
