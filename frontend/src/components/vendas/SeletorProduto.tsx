import { useState, useEffect, useRef } from 'react'
import { ChevronDown, Search } from 'lucide-react'
import { useEstoque } from '@/hooks/useEstoque'
import type { ItemCarrinho } from '@/types/vendas'

interface Props {
  onAdd: (item: ItemCarrinho) => void
}

export default function SeletorProduto({ onAdd }: Props) {
  const { produtos, listProdutos } = useEstoque()
  const [busca, setBusca] = useState('')
  const [aberto, setAberto] = useState(false)
  const containerRef = useRef<HTMLDivElement>(null)

  useEffect(() => { listProdutos() }, [listProdutos])

  useEffect(() => {
    function handleClickFora(e: MouseEvent) {
      if (containerRef.current && !containerRef.current.contains(e.target as Node))
        setAberto(false)
    }
    document.addEventListener('mousedown', handleClickFora)
    return () => document.removeEventListener('mousedown', handleClickFora)
  }, [])

  const filtrados = produtos
    .filter(p => p.ativo && (
      !busca ||
      p.nome.toLowerCase().includes(busca.toLowerCase()) ||
      (p.codigoBarras?.includes(busca) ?? false)))
    .slice(0, 40)

  function selecionar(produtoId: string) {
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
    setAberto(false)
  }

  return (
    <div ref={containerRef} className="relative">
      <div
        className="flex h-9 w-full items-center rounded-md border border-input bg-transparent px-3 text-sm cursor-text gap-2"
        onClick={() => setAberto(true)}
      >
        <Search size={14} className="shrink-0 text-muted-foreground" />
        <input
          className="flex-1 bg-transparent outline-none placeholder:text-muted-foreground"
          placeholder="Buscar ou selecionar produto..."
          value={busca}
          onChange={e => { setBusca(e.target.value); setAberto(true) }}
          onFocus={() => setAberto(true)}
        />
        <ChevronDown size={14} className="shrink-0 text-muted-foreground" />
      </div>

      {aberto && (
        <div className="absolute z-50 mt-1 w-full rounded-md border bg-popover shadow-md max-h-72 overflow-y-auto">
          {filtrados.length === 0 ? (
            <p className="px-4 py-3 text-sm text-muted-foreground">Nenhum produto encontrado</p>
          ) : (
            filtrados.map(p => (
              <button
                key={p.id}
                type="button"
                className="w-full flex items-center justify-between px-4 py-2 text-left hover:bg-accent transition-colors border-b last:border-b-0"
                onMouseDown={e => { e.preventDefault(); selecionar(p.id) }}
              >
                <div>
                  <p className="text-sm font-medium">{p.nome}</p>
                  <p className="text-xs text-muted-foreground">
                    {p.precoVenda.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' })}
                    {p.codigoBarras && <span className="ml-2 opacity-60">{p.codigoBarras}</span>}
                    {p.estoqueBaixo && <span className="ml-2 text-destructive">• Estoque baixo ({p.estoqueAtual})</span>}
                  </p>
                </div>
                <span className="text-xs text-muted-foreground ml-4 shrink-0">
                  {p.tipo === 'Servico' ? '✂️' : '📦'}
                </span>
              </button>
            ))
          )}
        </div>
      )}
    </div>
  )
}
