import { Trash2 } from 'lucide-react'
import { Input } from '@/components/ui/input'
import { Button } from '@/components/ui/button'
import type { ItemCarrinho } from '@/types/vendas'

interface Props {
  itens: ItemCarrinho[]
  desconto: number
  onChangeQuantidade: (produtoId: string, quantidade: number) => void
  onRemover: (produtoId: string) => void
  onChangeDesconto: (valor: number) => void
}

export default function ResumoPedido({
  itens, desconto, onChangeQuantidade, onRemover, onChangeDesconto
}: Props) {
  const subtotal = itens.reduce((s, i) => s + i.precoUnitario * i.quantidade, 0)
  const total = subtotal - desconto

  const fmt = (v: number) => v.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' })

  return (
    <div className="space-y-3">
      {itens.length === 0 ? (
        <p className="text-center py-8 text-muted-foreground text-sm">
          Adicione produtos usando a busca ao lado
        </p>
      ) : (
        <div className="divide-y rounded-md border">
          {itens.map(item => (
            <div key={item.produtoId} className="flex items-center gap-3 px-3 py-2">
              <div className="flex-1 min-w-0">
                <p className="text-sm font-medium truncate">{item.produtoNome}</p>
                <p className="text-xs text-muted-foreground">{fmt(item.precoUnitario)} un.</p>
              </div>
              <Input
                type="number"
                min="0.01"
                step="0.01"
                value={item.quantidade}
                onChange={e => onChangeQuantidade(item.produtoId, Number(e.target.value))}
                className="w-20 h-8 text-center"
              />
              <span className="text-sm font-medium w-24 text-right">
                {fmt(item.precoUnitario * item.quantidade)}
              </span>
              <Button
                size="icon"
                variant="ghost"
                onClick={() => onRemover(item.produtoId)}
                className="h-8 w-8 text-muted-foreground hover:text-destructive"
              >
                <Trash2 size={14} />
              </Button>
            </div>
          ))}
        </div>
      )}

      <div className="space-y-2 pt-2 border-t">
        <div className="flex justify-between text-sm">
          <span className="text-muted-foreground">Subtotal</span>
          <span>{fmt(subtotal)}</span>
        </div>
        <div className="flex items-center gap-2">
          <span className="text-sm text-muted-foreground">Desconto (R$)</span>
          <Input
            type="number" min="0" step="0.01"
            value={desconto}
            onChange={e => onChangeDesconto(Number(e.target.value))}
            className="h-8 w-28 ml-auto"
          />
        </div>
        <div className="flex justify-between font-semibold text-lg pt-1 border-t">
          <span>Total</span>
          <span className="text-primary">{fmt(total)}</span>
        </div>
      </div>
    </div>
  )
}
