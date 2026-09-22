import { useRef } from 'react'
import { Trash2, Plus } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import type { ItemCompraRequest, DestinoCompra } from '@/types/compras'

const DESTINOS: { value: DestinoCompra; label: string }[] = [
  { value: 'EstoqueParaVenda', label: 'Estoque para Venda' },
  { value: 'ConsumoInterno', label: 'Consumo Interno' },
  { value: 'AtivoImobilizado', label: 'Ativo Imobilizado' },
]

const EMPTY_ITEM: ItemCompraRequest = {
  description: '',
  destination: 'EstoqueParaVenda',
  quantity: 1,
  unitPrice: 0,
  discount: 0,
  allocatedFreight: 0,
  taxes: 0,
}

function calcTotal(item: ItemCompraRequest) {
  return item.quantity * item.unitPrice - item.discount + item.allocatedFreight + item.taxes
}

interface Props {
  itens: ItemCompraRequest[]
  onChange: (itens: ItemCompraRequest[]) => void
  readonly?: boolean
}

export default function ItensCompraTable({ itens, onChange, readonly }: Props) {
  const keysRef = useRef<WeakMap<ItemCompraRequest, string>>(new WeakMap())

  function keyFor(item: ItemCompraRequest) {
    let key = keysRef.current.get(item)
    if (!key) {
      key = crypto.randomUUID()
      keysRef.current.set(item, key)
    }
    return key
  }

  function add() {
    onChange([...itens, { ...EMPTY_ITEM }])
  }

  function remove(idx: number) {
    onChange(itens.filter((_, i) => i !== idx))
  }

  function update(idx: number, field: keyof ItemCompraRequest, value: string | number) {
    const next = itens.map((item, i) =>
      i === idx ? { ...item, [field]: value } : item
    )
    onChange(next)
  }

  const total = itens.reduce((acc, item) => acc + calcTotal(item), 0)

  return (
    <div className="space-y-3">
      <div className="overflow-x-auto rounded-md border">
        <table className="w-full text-sm">
          <thead>
            <tr className="border-b bg-muted/50">
              <th className="px-3 py-2 text-left font-medium min-w-[160px]">Descrição</th>
              <th className="px-3 py-2 text-left font-medium min-w-[150px]">Destino</th>
              <th className="px-3 py-2 text-right font-medium w-20">Qtd</th>
              <th className="px-3 py-2 text-right font-medium w-28">Valor Unit.</th>
              <th className="px-3 py-2 text-right font-medium w-24">Desconto</th>
              <th className="px-3 py-2 text-right font-medium w-24">Frete</th>
              <th className="px-3 py-2 text-right font-medium w-24">Impostos</th>
              <th className="px-3 py-2 text-right font-medium w-28">Total</th>
              {!readonly && <th className="px-3 py-2 w-10" />}
            </tr>
          </thead>
          <tbody>
            {itens.length === 0 && (
              <tr>
                <td colSpan={9} className="px-3 py-6 text-center text-muted-foreground">
                  Nenhum item adicionado
                </td>
              </tr>
            )}
            {itens.map((item, idx) => (
              <tr key={keyFor(item)} className="border-b">
                <td className="px-2 py-1">
                  {readonly
                    ? <span>{item.description}</span>
                    : <Input
                        value={item.description}
                        onChange={e => update(idx, 'description', e.target.value)}
                        className="h-8"
                        placeholder="Descrição do item"
                      />}
                </td>
                <td className="px-2 py-1">
                  {readonly
                    ? <span>{DESTINOS.find(d => d.value === item.destination)?.label}</span>
                    : <select
                        value={item.destination}
                        onChange={e => update(idx, 'destination', e.target.value)}
                        className="h-8 w-full rounded-md border border-input bg-background px-2 text-sm"
                      >
                        {DESTINOS.map(d => <option key={d.value} value={d.value}>{d.label}</option>)}
                      </select>}
                </td>
                <td className="px-2 py-1">
                  {readonly
                    ? <span className="block text-right">{item.quantity}</span>
                    : <Input
                        type="number"
                        value={item.quantity}
                        onChange={e => update(idx, 'quantity', parseFloat(e.target.value) || 0)}
                        className="h-8 text-right"
                        min={0}
                      />}
                </td>
                <td className="px-2 py-1">
                  {readonly
                    ? <span className="block text-right">{item.unitPrice.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' })}</span>
                    : <Input
                        type="number"
                        value={item.unitPrice}
                        onChange={e => update(idx, 'unitPrice', parseFloat(e.target.value) || 0)}
                        className="h-8 text-right"
                        min={0}
                        step={0.01}
                      />}
                </td>
                <td className="px-2 py-1">
                  {readonly
                    ? <span className="block text-right">{item.discount.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' })}</span>
                    : <Input
                        type="number"
                        value={item.discount}
                        onChange={e => update(idx, 'discount', parseFloat(e.target.value) || 0)}
                        className="h-8 text-right"
                        min={0}
                        step={0.01}
                      />}
                </td>
                <td className="px-2 py-1">
                  {readonly
                    ? <span className="block text-right">{item.allocatedFreight.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' })}</span>
                    : <Input
                        type="number"
                        value={item.allocatedFreight}
                        onChange={e => update(idx, 'allocatedFreight', parseFloat(e.target.value) || 0)}
                        className="h-8 text-right"
                        min={0}
                        step={0.01}
                      />}
                </td>
                <td className="px-2 py-1">
                  {readonly
                    ? <span className="block text-right">{item.taxes.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' })}</span>
                    : <Input
                        type="number"
                        value={item.taxes}
                        onChange={e => update(idx, 'taxes', parseFloat(e.target.value) || 0)}
                        className="h-8 text-right"
                        min={0}
                        step={0.01}
                      />}
                </td>
                <td className="px-3 py-1 text-right font-medium">
                  {calcTotal(item).toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' })}
                </td>
                {!readonly && (
                  <td className="px-2 py-1">
                    <Button
                      type="button"
                      size="icon"
                      variant="ghost"
                      className="h-8 w-8 text-destructive hover:text-destructive"
                      onClick={() => remove(idx)}
                    >
                      <Trash2 size={14} />
                    </Button>
                  </td>
                )}
              </tr>
            ))}
          </tbody>
          <tfoot>
            <tr className="border-t bg-muted/30">
              <td colSpan={readonly ? 7 : 7} className="px-3 py-2 text-right font-semibold text-sm">
                Total:
              </td>
              <td className="px-3 py-2 text-right font-bold">
                {total.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' })}
              </td>
              {!readonly && <td />}
            </tr>
          </tfoot>
        </table>
      </div>

      {!readonly && (
        <Button type="button" variant="outline" size="sm" onClick={add}>
          <Plus size={14} className="mr-1" /> Adicionar Item
        </Button>
      )}
    </div>
  )
}
