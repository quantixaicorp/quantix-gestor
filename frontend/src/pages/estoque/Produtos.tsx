import { useEffect, useState } from 'react'
import { Plus, PackageX, ArrowDownToLine, Pencil } from 'lucide-react'
import { useEstoque } from '@/hooks/useEstoque'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Badge } from '@/components/ui/badge'
import { Dialog, DialogContent, DialogHeader, DialogTitle } from '@/components/ui/dialog'
import ProdutoCriarForm from '@/components/estoque/ProdutoCriarForm'
import ServicoCriarForm from '@/components/estoque/ServicoCriarForm'
import ProdutoEditForm from '@/components/estoque/ProdutoEditForm'
import EntradaEstoqueDialog from '@/components/estoque/EntradaEstoqueDialog'
import type { ProdutoResponse, CreateProdutoRequest, UpdateProdutoRequest } from '@/types/estoque'

type Aba = 'produtos' | 'servicos'

export default function Produtos() {
  const { produtos, categorias, loading, listProdutos, listCategorias, createCategoria, createProduto, updateProduto, entradaEstoque } = useEstoque()
  const [aba, setAba] = useState<Aba>('produtos')
  const [busca, setBusca] = useState('')
  const [modalCriar, setModalCriar] = useState(false)
  const [produtoEditando, setProdutoEditando] = useState<ProdutoResponse | null>(null)
  const [produtoEntrada, setProdutoEntrada] = useState<ProdutoResponse | null>(null)

  useEffect(() => { listProdutos(); listCategorias() }, [listProdutos, listCategorias])

  async function handleCreate(data: CreateProdutoRequest) {
    try {
      await createProduto(data)
      setModalCriar(false)
    } catch (e) {
      alert(e instanceof Error ? e.message : 'Erro ao salvar')
    }
  }

  async function handleUpdate(id: string, data: UpdateProdutoRequest) {
    try {
      await updateProduto(id, data)
      setProdutoEditando(null)
    } catch (e) {
      alert(e instanceof Error ? e.message : 'Erro ao salvar')
    }
  }

  const itensFiltrados = produtos
    .filter(p => p.tipo === (aba === 'produtos' ? 'Produto' : 'Servico'))
    .filter(p =>
      p.nome.toLowerCase().includes(busca.toLowerCase()) ||
      (p.codigoBarras?.includes(busca) ?? false))

  const isProduto = aba === 'produtos'

  return (
    <div className="space-y-4">
      {/* Cabeçalho */}
      <div className="flex items-center justify-between">
        <h1 className="text-2xl font-bold">Estoque</h1>
        <Button onClick={() => setModalCriar(true)}>
          <Plus size={16} className="mr-2" />
          {isProduto ? 'Novo Produto' : 'Novo Serviço'}
        </Button>
      </div>

      {/* Abas */}
      <div className="flex gap-1 border-b">
        {(['produtos', 'servicos'] as Aba[]).map(a => (
          <button
            key={a}
            onClick={() => { setAba(a); setBusca('') }}
            className={`px-4 py-2 text-sm font-medium border-b-2 transition-colors ${
              aba === a
                ? 'border-primary text-primary'
                : 'border-transparent text-muted-foreground hover:text-foreground'
            }`}
          >
            {a === 'produtos' ? '📦 Produtos' : '✂️ Serviços'}
          </button>
        ))}
      </div>

      <Input
        placeholder={isProduto ? 'Buscar produto por nome ou código...' : 'Buscar serviço por nome...'}
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
                {isProduto ? (
                  <th className="px-4 py-3 text-right font-medium">Estoque</th>
                ) : (
                  <th className="px-4 py-3 text-right font-medium">Duração</th>
                )}
                <th className="px-4 py-3 text-left font-medium">Status</th>
                <th className="px-4 py-3" />
              </tr>
            </thead>
            <tbody>
              {itensFiltrados.map(p => (
                <tr key={p.id} className="border-b last:border-b-0">
                  <td className="px-4 py-3 font-medium">{p.nome}</td>
                  <td className="px-4 py-3 text-muted-foreground">{p.categoriaNome}</td>
                  <td className="px-4 py-3 text-right">
                    {p.precoVenda.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' })}
                  </td>
                  {isProduto ? (
                    <td className="px-4 py-3 text-right">{p.estoqueAtual}</td>
                  ) : (
                    <td className="px-4 py-3 text-right text-muted-foreground">
                      {p.duracaoMinutos ? `${p.duracaoMinutos} min` : '—'}
                    </td>
                  )}
                  <td className="px-4 py-3">
                    {!p.ativo ? (
                      <Badge variant="secondary">Inativo</Badge>
                    ) : isProduto && p.estoqueBaixo ? (
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
                      {isProduto && (
                        <Button size="sm" variant="outline" onClick={() => setProdutoEntrada(p)}>
                          <ArrowDownToLine size={14} className="mr-1" /> Entrada
                        </Button>
                      )}
                    </div>
                  </td>
                </tr>
              ))}
              {itensFiltrados.length === 0 && (
                <tr>
                  <td colSpan={6} className="px-4 py-8 text-center text-muted-foreground">
                    {isProduto ? 'Nenhum produto encontrado' : 'Nenhum serviço encontrado'}
                  </td>
                </tr>
              )}
            </tbody>
          </table>
        </div>
      )}

      {/* Dialog criar */}
      <Dialog open={modalCriar} onOpenChange={setModalCriar}>
        <DialogContent className="max-w-lg">
          <DialogHeader>
            <DialogTitle>{isProduto ? 'Novo Produto' : 'Novo Serviço'}</DialogTitle>
          </DialogHeader>
          {isProduto ? (
            <ProdutoCriarForm
              categorias={categorias}
              onSubmit={handleCreate}
              onCancel={() => setModalCriar(false)}
              onCreateCategoria={createCategoria}
            />
          ) : (
            <ServicoCriarForm
              categorias={categorias}
              onSubmit={handleCreate}
              onCancel={() => setModalCriar(false)}
              onCreateCategoria={createCategoria}
            />
          )}
        </DialogContent>
      </Dialog>

      {/* Dialog editar */}
      <Dialog open={!!produtoEditando} onOpenChange={open => { if (!open) setProdutoEditando(null) }}>
        <DialogContent className="max-w-lg">
          <DialogHeader>
            <DialogTitle>{produtoEditando?.tipo === 'Servico' ? 'Editar Serviço' : 'Editar Produto'}</DialogTitle>
          </DialogHeader>
          {produtoEditando && (
            <ProdutoEditForm
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
    </div>
  )
}
