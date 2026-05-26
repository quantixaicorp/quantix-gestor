import { Trash2, Minus, Plus } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import type { ItemCarrinho } from '@/types/vendas'

interface Props {
  itens: ItemCarrinho[]
  desconto: number
  onChangeQuantidade: (produtoId: string, quantidade: number) => void
  onRemover: (produtoId: string) => void
  onChangeDesconto: (valor: number) => void
}

const fmt = (v: number) => v.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' })

export default function ResumoPedido({ itens, desconto, onChangeQuantidade, onRemover, onChangeDesconto }: Props) {
  const subtotal = itens.reduce((s, i) => s + i.precoUnitario * i.quantidade, 0)
  const total = Math.max(0, subtotal - desconto)

  return (
    <div className="flex flex-col h-full">
      {/* Lista de itens */}
      <div className="flex-1 overflow-y-auto space-y-2 pr-1">
        {itens.length === 0 ? (
          <div className="flex flex-col items-center justify-center h-48 text-center">
            <div className="text-4xl mb-3 opacity-30">🛒</div>
            <p className="text-sm text-muted-foreground">Carrinho vazio</p>
            <p className="text-xs text-muted-foreground mt-1">Busque e adicione produtos acima</p>
          </div>
        ) : (
          itens.map(item => (
            <div key={item.produtoId}
              className="flex items-center gap-3 rounded-lg border bg-card px-3 py-2.5 shadow-sm">
              <div className="flex-1 min-w-0">
                <p className="text-sm font-medium truncate">{item.produtoNome}</p>
                <p className="text-xs text-muted-foreground">{fmt(item.precoUnitario)} / un.</p>
              </div>
              <div className="flex items-center gap-1">
                <Button size="icon" variant="outline"
                  className="h-7 w-7"
                  onClick={() => onChangeQuantidade(item.produtoId, item.quantidade - 1)}
                  disabled={item.quantidade <= 1}>
                  <Minus size={12} />
                </Button>
                <Input
                  type="number" min="0.01" step="0.01"
                  value={item.quantidade}
                  onChange={e => onChangeQuantidade(item.produtoId, Number(e.target.value))}
                  className="w-14 h-7 text-center text-sm px-1"
                />
                <Button size="icon" variant="outline"
                  className="h-7 w-7"
                  onClick={() => onChangeQuantidade(item.produtoId, item.quantidade + 1)}>
                  <Plus size={12} />
                </Button>
              </div>
              <span className="text-sm font-semibold w-20 text-right tabular-nums">
                {fmt(item.precoUnitario * item.quantidade)}
              </span>
              <Button size="icon" variant="ghost"
                className="h-7 w-7 text-muted-foreground hover:text-destructive shrink-0"
                onClick={() => onRemover(item.produtoId)}>
                <Trash2 size={13} />
              </Button>
            </div>
          ))
        )}
      </div>

      {/* Totais */}
      {itens.length > 0 && (
        <div className="mt-4 pt-4 border-t space-y-3">
          <div className="flex justify-between text-sm text-muted-foreground">
            <span>{itens.length} {itens.length === 1 ? 'item' : 'itens'}</span>
            <span>{fmt(subtotal)}</span>
          </div>
          <div className="flex items-center justify-between gap-3">
            <span className="text-sm text-muted-foreground shrink-0">Desconto (R$)</span>
            <Input type="number" min="0" step="0.01" value={desconto}
              onChange={e => onChangeDesconto(Number(e.target.value))}
              className="h-8 w-28 text-right" />
          </div>
          <div className="flex justify-between items-center pt-2 border-t">
            <span className="font-semibold text-base">Total</span>
            <span className="text-2xl font-bold text-primary tabular-nums">{fmt(total)}</span>
          </div>
        </div>
      )}
    </div>
  )
}
