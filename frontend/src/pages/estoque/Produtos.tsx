import { useEffect, useState } from 'react'
import { Plus, PackageX, ArrowDownToLine } from 'lucide-react'
import { useEstoque } from '@/hooks/useEstoque'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Badge } from '@/components/ui/badge'
import { Dialog, DialogContent, DialogHeader, DialogTitle } from '@/components/ui/dialog'
import ProdutoForm from '@/components/estoque/ProdutoForm'
import EntradaEstoqueDialog from '@/components/estoque/EntradaEstoqueDialog'
import type { ProdutoResponse, CreateProdutoRequest } from '@/types/estoque'

export default function Produtos() {
  const { produtos, categorias, loading, listProdutos, listCategorias, createCategoria, createProduto, entradaEstoque } = useEstoque()
  const [busca, setBusca] = useState('')
  const [modalAberto, setModalAberto] = useState(false)
  const [produtoEntrada, setProdutoEntrada] = useState<ProdutoResponse | null>(null)

  useEffect(() => { listProdutos(); listCategorias() }, [listProdutos, listCategorias])

  async function handleCreate(data: CreateProdutoRequest) {
    await createProduto(data)
    setModalAberto(false)
  }

  const produtosFiltrados = produtos.filter(p =>
    p.nome.toLowerCase().includes(busca.toLowerCase()) ||
    (p.codigoBarras?.includes(busca) ?? false))

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between">
        <h1 className="text-2xl font-bold">Produtos</h1>
        <Button onClick={() => setModalAberto(true)}>
          <Plus size={16} className="mr-2" /> Novo Produto
        </Button>
      </div>

      <Input
        placeholder="Buscar por nome ou código de barras..."
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
                <th className="px-4 py-3 text-right font-medium">Estoque</th>
                <th className="px-4 py-3 text-left font-medium">Status</th>
                <th className="px-4 py-3" />
              </tr>
            </thead>
            <tbody>
              {produtosFiltrados.map(p => (
                <tr key={p.id} className="border-b">
                  <td className="px-4 py-3 font-medium">{p.nome}</td>
                  <td className="px-4 py-3 text-muted-foreground">{p.categoriaNome}</td>
                  <td className="px-4 py-3 text-right">
                    {p.precoVenda.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' })}
                  </td>
                  <td className="px-4 py-3 text-right">{p.estoqueAtual}</td>
                  <td className="px-4 py-3">
                    {p.estoqueBaixo ? (
                      <Badge variant="destructive" className="gap-1">
                        <PackageX size={12} /> Estoque baixo
                      </Badge>
                    ) : (
                      <Badge variant="secondary">OK</Badge>
                    )}
                  </td>
                  <td className="px-4 py-3">
                    <Button size="sm" variant="outline" onClick={() => setProdutoEntrada(p)}>
                      <ArrowDownToLine size={14} className="mr-1" /> Entrada
                    </Button>
                  </td>
                </tr>
              ))}
              {produtosFiltrados.length === 0 && (
                <tr>
                  <td colSpan={6} className="px-4 py-8 text-center text-muted-foreground">
                    Nenhum produto encontrado
                  </td>
                </tr>
              )}
            </tbody>
          </table>
        </div>
      )}

      <Dialog open={modalAberto} onOpenChange={setModalAberto}>
        <DialogContent className="max-w-lg">
          <DialogHeader><DialogTitle>Novo Produto</DialogTitle></DialogHeader>
          <ProdutoForm
            categorias={categorias}
            onSubmit={handleCreate}
            onCancel={() => setModalAberto(false)}
            onCreateCategoria={createCategoria}
          />
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
    </div>
  )
}
