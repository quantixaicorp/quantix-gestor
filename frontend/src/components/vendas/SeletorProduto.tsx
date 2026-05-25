import { useState, useEffect } from 'react'
import { Plus, Search } from 'lucide-react'
import { Input } from '@/components/ui/input'
import { Button } from '@/components/ui/button'
import { useEstoque } from '@/hooks/useEstoque'
import type { ItemCarrinho } from '@/types/vendas'

interface Props {
  onAdd: (item: ItemCarrinho) => void
}

export default function SeletorProduto({ onAdd }: Props) {
  const { produtos, listProdutos } = useEstoque()
  const [busca, setBusca] = useState('')

  useEffect(() => { listProdutos() }, [listProdutos])

  const filtrados = produtos
    .filter(p => p.ativo && (
      p.nome.toLowerCase().includes(busca.toLowerCase()) ||
      (p.codigoBarras?.includes(busca) ?? false)))
    .slice(0, 20)

  function handleAdd(produtoId: string) {
    const p = produtos.find(x => x.id === produtoId)!
    onAdd({
      produtoId: p.id,
      produtoNome: p.nome,
      precoUnitario: p.precoVenda,
      quantidade: 1,
      desconto: 0,
      total: p.precoVenda,
    })
    setBusca('')
  }

  return (
    <div className="space-y-3">
      <div className="relative">
        <Search size={16} className="absolute left-3 top-1/2 -translate-y-1/2 text-muted-foreground" />
        <Input
          className="pl-9"
          placeholder="Buscar produto por nome ou código..."
          value={busca}
          onChange={e => setBusca(e.target.value)}
          autoFocus
        />
      </div>

      {busca && (
        <div className="rounded-md border divide-y max-h-64 overflow-y-auto">
          {filtrados.length === 0 ? (
            <p className="px-4 py-3 text-sm text-muted-foreground">Nenhum produto encontrado</p>
          ) : filtrados.map(p => (
            <div key={p.id} className="flex items-center justify-between px-4 py-2">
              <div>
                <p className="text-sm font-medium">{p.nome}</p>
                <p className="text-xs text-muted-foreground">
                  {p.precoVenda.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' })}
                  {p.estoqueBaixo && <span className="ml-2 text-destructive">• Estoque baixo ({p.estoqueAtual})</span>}
                </p>
              </div>
              <Button size="sm" variant="outline" onClick={() => handleAdd(p.id)}>
                <Plus size={14} />
              </Button>
            </div>
          ))}
        </div>
      )}
    </div>
  )
}
