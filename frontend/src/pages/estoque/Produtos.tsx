import { useEffect, useState } from 'react'
import { Plus, PackageX, ArrowDownToLine, Pencil, Trash2 } from 'lucide-react'
import { useEstoque } from '@/hooks/useEstoque'
import { api } from '@/services/api'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Badge } from '@/components/ui/badge'
import { Dialog, DialogContent, DialogHeader, DialogTitle } from '@/components/ui/dialog'
import ProdutoCriarForm from '@/components/estoque/ProdutoCriarForm'
import ProdutoEditForm from '@/components/estoque/ProdutoEditForm'
import EntradaEstoqueDialog from '@/components/estoque/EntradaEstoqueDialog'
import { useConfirm } from '@/hooks/useConfirm'
import { toast } from '@/hooks/useToast'
import type { ProdutoResponse, CreateProdutoRequest, UpdateProdutoRequest } from '@/types/estoque'
import { KpiRow } from '@/components/ui/KpiRow'

export default function Produtos() {
  const { produtos, categorias, loading, listProdutos, listCategorias, createCategoria, createProduto, updateProduto, entradaEstoque } = useEstoque()
  const [busca, setBusca] = useState('')
  const [modalCriar, setModalCriar] = useState(false)
  const [produtoEditando, setProdutoEditando] = useState<ProdutoResponse | null>(null)
  const [produtoEntrada, setProdutoEntrada] = useState<ProdutoResponse | null>(null)
  const { confirm, ConfirmDialogNode } = useConfirm()

  useEffect(() => { listProdutos(); listCategorias() }, [listProdutos, listCategorias])

  async function handleCreate(data: CreateProdutoRequest) {
    try {
      await createProduto(data)
      setModalCriar(false)
    } catch (e) {
      toast.error(e instanceof Error ? e.message : 'Erro ao salvar produto')
    }
  }

  async function handleUpdate(id: string, data: UpdateProdutoRequest) {
    try {
      await updateProduto(id, data)
      setProdutoEditando(null)
    } catch (e) {
      toast.error(e instanceof Error ? e.message : 'Erro ao salvar produto')
    }
  }

  async function handleDelete(id: string, nome: string) {
    const ok = await confirm({ title: `Excluir "${nome}"?`, description: 'Esta ação não pode ser desfeita.' })
    if (!ok) return
    try {
      await api.delete(`/api/produtos/${id}`)
      await listProdutos()
    } catch (e) {
      toast.error(e instanceof Error ? e.message : 'Erro ao excluir produto')
    }
  }

  const produtosFiltrados = produtos
    .filter(p => p.tipo === 'Produto')
    .filter(p =>
      p.nome.toLowerCase().includes(busca.toLowerCase()) ||
      (p.codigoBarras?.includes(busca) ?? false))

  return (
    <div className="space-y-4">
      <div className="flex flex-wrap items-center justify-between gap-2">
        <h1 className="text-2xl font-bold">Produtos</h1>
        <Button onClick={() => setModalCriar(true)}>
          <Plus size={16} className="mr-2" /> Novo Produto
        </Button>
      </div>

      {(() => {
        const prods = produtos.filter(p => p.tipo === 'Produto')
        const fmt = (v: number) => v.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' })
        return (
          <KpiRow items={[
            { label: 'Total produtos', value: String(prods.length) },
            { label: 'Ativos', value: String(prods.filter(p => p.ativo).length), color: 'text-green-600 dark:text-green-400' },
            { label: 'Estoque baixo', value: String(prods.filter(p => p.estoqueBaixo && p.ativo).length), color: prods.some(p => p.estoqueBaixo && p.ativo) ? 'text-red-600 dark:text-red-400' : '' },
            { label: 'Valor em estoque', value: fmt(prods.reduce((s, p) => s + p.estoqueAtual * p.custoMedio, 0)) },
          ]} />
        )
      })()}

      <Input
        placeholder="Buscar por nome ou código de barras..."
        value={busca}
        onChange={e => setBusca(e.target.value)}
        className="max-w-sm"
      />

      {loading ? (
        <p className="text-muted-foreground">Carregando...</p>
      ) : produtosFiltrados.length === 0 ? (
        <p className="text-center text-muted-foreground py-12">Nenhum produto encontrado</p>
      ) : (
        <>
          {/* Mobile: card list */}
          <div className="md:hidden space-y-2">
            {produtosFiltrados.map(p => (
              <div key={p.id} className="rounded-lg border bg-card p-4 space-y-2">
                <div className="flex items-start justify-between gap-2">
                  <div className="min-w-0 flex-1">
                    <p className="font-medium truncate">{p.nome}</p>
                    <p className="text-sm text-muted-foreground">{p.categoriaNome}</p>
                  </div>
                  {!p.ativo ? (
                    <Badge variant="outline">Inativo</Badge>
                  ) : p.estoqueBaixo ? (
                    <Badge variant="destructive" className="gap-1 shrink-0"><PackageX size={11} /> Baixo</Badge>
                  ) : (
                    <Badge variant="secondary">OK</Badge>
                  )}
                </div>
                <div className="flex items-center justify-between text-sm">
                  <span className="text-muted-foreground">Estoque: <strong className="text-foreground">{p.estoqueAtual}</strong></span>
                  <span className="font-semibold">{p.precoVenda.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' })}</span>
                </div>
                <div className="flex gap-1 pt-1">
                  <Button size="sm" variant="outline" className="flex-1" onClick={() => setProdutoEditando(p)}>
                    <Pencil size={13} className="mr-1" /> Editar
                  </Button>
                  <Button size="sm" variant="outline" className="flex-1" onClick={() => setProdutoEntrada(p)}>
                    <ArrowDownToLine size={13} className="mr-1" /> Entrada
                  </Button>
                  <Button size="sm" variant="outline" className="text-destructive hover:text-destructive"
                    onClick={() => handleDelete(p.id, p.nome)}>
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
                  <th className="px-4 py-3 text-right font-medium">Estoque</th>
                  <th className="px-4 py-3 text-left font-medium">Status</th>
                  <th className="px-4 py-3" />
                </tr>
              </thead>
              <tbody>
                {produtosFiltrados.map(p => (
                  <tr key={p.id} className="border-b last:border-b-0">
                    <td className="px-4 py-3 font-medium">{p.nome}</td>
                    <td className="px-4 py-3 text-muted-foreground">{p.categoriaNome}</td>
                    <td className="px-4 py-3 text-right">
                      {p.precoVenda.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' })}
                    </td>
                    <td className="px-4 py-3 text-right">{p.estoqueAtual}</td>
                    <td className="px-4 py-3">
                      {!p.ativo ? (
                        <Badge variant="outline">Inativo</Badge>
                      ) : p.estoqueBaixo ? (
                        <Badge variant="destructive" className="gap-1">
                          <PackageX size={12} /> Estoque baixo
                        </Badge>
                      ) : (
                        <Badge variant="secondary">OK</Badge>
                      )}
                    </td>
                    <td className="px-4 py-3">
                      <div className="flex gap-1 justify-end">
                        <Button size="sm" variant="outline" onClick={() => setProdutoEditando(p)}>
                          <Pencil size={14} />
                        </Button>
                        <Button size="sm" variant="outline" onClick={() => setProdutoEntrada(p)}>
                          <ArrowDownToLine size={14} className="mr-1" /> Entrada
                        </Button>
                        <Button size="sm" variant="outline" className="text-destructive hover:text-destructive"
                          onClick={() => handleDelete(p.id, p.nome)}>
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
          <DialogHeader><DialogTitle>Novo Produto</DialogTitle></DialogHeader>
          <ProdutoCriarForm
            categorias={categorias}
            onSubmit={handleCreate}
            onCancel={() => setModalCriar(false)}
            onCreateCategoria={createCategoria}
          />
        </DialogContent>
      </Dialog>

      <Dialog open={!!produtoEditando} onOpenChange={open => { if (!open) setProdutoEditando(null) }}>
        <DialogContent className="max-w-lg">
          <DialogHeader><DialogTitle>Editar Produto</DialogTitle></DialogHeader>
          {produtoEditando && (
            <ProdutoEditForm
              key={produtoEditando.id}
              produto={produtoEditando}
              categorias={categorias}
              onSubmit={handleUpdate}
              onCancel={() => setProdutoEditando(null)}
            />
          )}
        </DialogContent>
      </Dialog>

      <EntradaEstoqueDialog
        produto={produtoEntrada}
        open={!!produtoEntrada}
        onClose={() => setProdutoEntrada(null)}
        onConfirm={async (id, data) => {
          await entradaEstoque({ produtoId: id, ...data })
          setProdutoEntrada(null)
        }}
      />

      {ConfirmDialogNode}
    </div>
  )
}
