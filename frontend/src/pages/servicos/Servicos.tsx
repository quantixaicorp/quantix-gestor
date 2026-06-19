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
import { KpiRow } from '@/components/ui/KpiRow'

export default function Servicos() {
  const { categorias, listCategorias, createCategoria, deleteCategoria, createProduto } = useEstoque()
  const [servicos, setServicos] = useState<ProdutoResponse[]>([])
  const [loading, setLoading] = useState(false)
  const [busca, setBusca] = useState('')
  const [filtroCategoria, setFiltroCategoria] = useState('')
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
      await carregar()
      setEditando(null)
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

  async function handleDeleteCategoria(id: string, nome: string) {
    const ok = await confirm({
      title: `Excluir categoria "${nome}"?`,
      description: 'Serviços vinculados a esta categoria ficarão sem categoria.',
      variant: 'destructive',
    })
    if (!ok) return
    try {
      await deleteCategoria(id)
      toast.success('Categoria excluída')
    } catch (e) {
      toast.error(e instanceof Error ? e.message : 'Erro ao excluir categoria')
    }
  }

  const filtrados = servicos.filter(s =>
    s.nome.toLowerCase().includes(busca.toLowerCase()) &&
    (!filtroCategoria || s.categoriaId === filtroCategoria)
  )

  return (
    <div className="space-y-4">
      <div className="flex flex-wrap items-center justify-between gap-2">
        <h1 className="text-2xl font-bold">Serviços</h1>
        <Button onClick={() => setModalCriar(true)}>
          <Plus size={16} className="mr-2" /> Novo Serviço
        </Button>
      </div>

      <KpiRow items={[
        { label: 'Total', value: String(servicos.length) },
        { label: 'Ativos', value: String(servicos.filter(s => s.ativo).length), color: 'text-green-600 dark:text-green-400' },
        { label: 'Inativos', value: String(servicos.filter(s => !s.ativo).length), color: 'text-muted-foreground' },
        { label: 'Duração média', value: servicos.filter(s => s.duracaoMinutos).length
          ? `${Math.round(servicos.filter(s => s.duracaoMinutos).reduce((a, s) => a + (s.duracaoMinutos ?? 0), 0) / servicos.filter(s => s.duracaoMinutos).length)} min`
          : '—' },
      ]} />

      <div className="flex flex-wrap gap-2 items-center">
        <Input
          placeholder="Buscar serviço por nome..."
          value={busca}
          onChange={e => setBusca(e.target.value)}
          className="max-w-xs"
        />
        <select value={filtroCategoria} onChange={e => setFiltroCategoria(e.target.value)}
          className="h-9 rounded-md border border-input bg-transparent px-3 text-sm">
          <option value="">Todas as categorias</option>
          {categorias.map(c => <option key={c.id} value={c.id}>{c.nome}</option>)}
        </select>
        {filtroCategoria && (
          <button onClick={() => setFiltroCategoria('')}
            className="h-9 px-3 text-xs text-muted-foreground hover:text-foreground rounded-md border border-input">
            Limpar filtro
          </button>
        )}
      </div>

      {categorias.length > 0 && (
        <div className="rounded-xl border bg-card px-4 py-3">
          <p className="text-xs font-semibold uppercase tracking-wide text-muted-foreground mb-2">Categorias</p>
          <div className="flex flex-wrap gap-2">
            {categorias.map(c => (
              <div key={c.id} className="flex items-center gap-1 rounded-full bg-muted px-3 py-1 text-sm">
                {c.nome}
                <button
                  onClick={() => void handleDeleteCategoria(c.id, c.nome)}
                  className="ml-1 text-muted-foreground hover:text-destructive transition-colors">
                  <Trash2 size={12} />
                </button>
              </div>
            ))}
          </div>
        </div>
      )}

      {loading ? (
        <p className="text-muted-foreground">Carregando...</p>
      ) : filtrados.length === 0 ? (
        <p className="text-center text-muted-foreground py-12">Nenhum serviço encontrado</p>
      ) : (
        <>
          {/* Mobile: card list */}
          <div className="md:hidden space-y-2">
            {filtrados.map(s => (
              <div key={s.id} className="rounded-lg border bg-card p-4 space-y-2">
                <div className="flex items-start justify-between gap-2">
                  <div className="min-w-0 flex-1">
                    <p className="font-medium truncate">{s.nome}</p>
                    <p className="text-sm text-muted-foreground">{s.categoriaNome}</p>
                  </div>
                  <Badge variant={s.ativo ? 'secondary' : 'outline'} className="shrink-0">
                    {s.ativo ? 'Ativo' : 'Inativo'}
                  </Badge>
                </div>
                <div className="flex items-center justify-between text-sm">
                  <span className="text-muted-foreground">{s.duracaoMinutos ? `${s.duracaoMinutos} min` : '—'}</span>
                  <span className="font-semibold">{s.precoVenda.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' })}</span>
                </div>
                <div className="flex gap-1 pt-1">
                  <Button size="sm" variant="outline" className="flex-1" onClick={() => setEditando(s)}>
                    <Pencil size={13} className="mr-1" /> Editar
                  </Button>
                  <Button size="sm" variant="outline" className="text-destructive hover:text-destructive"
                    onClick={() => handleDelete(s.id, s.nome)}>
                    <Trash2 size={13} />
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
              </tbody>
            </table>
          </div>
        </>
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
