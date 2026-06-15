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
  descricao: '',
  destinoCompra: 'EstoqueParaVenda',
  quantidade: 1,
  valorUnitario: 0,
  desconto: 0,
  freteRateado: 0,
  impostos: 0,
}

function calcTotal(item: ItemCompraRequest) {
  return item.quantidade * item.valorUnitario - item.desconto + item.freteRateado + item.impostos
}

interface Props {
  itens: ItemCompraRequest[]
  onChange: (itens: ItemCompraRequest[]) => void
  readonly?: boolean
}

export default function ItensCompraTable({ itens, onChange, readonly }: Props) {
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
              <tr key={idx} className="border-b">
                <td className="px-2 py-1">
                  {readonly
                    ? <span>{item.descricao}</span>
                    : <Input
                        value={item.descricao}
                        onChange={e => update(idx, 'descricao', e.target.value)}
                        className="h-8"
                        placeholder="Descrição do item"
                      />}
                </td>
                <td className="px-2 py-1">
                  {readonly
                    ? <span>{DESTINOS.find(d => d.value === item.destinoCompra)?.label}</span>
                    : <select
                        value={item.destinoCompra}
                        onChange={e => update(idx, 'destinoCompra', e.target.value)}
                        className="h-8 w-full rounded-md border border-input bg-background px-2 text-sm"
                      >
                        {DESTINOS.map(d => <option key={d.value} value={d.value}>{d.label}</option>)}
                      </select>}
                </td>
                <td className="px-2 py-1">
                  {readonly
                    ? <span className="block text-right">{item.quantidade}</span>
                    : <Input
                        type="number"
                        value={item.quantidade}
                        onChange={e => update(idx, 'quantidade', parseFloat(e.target.value) || 0)}
                        className="h-8 text-right"
                        min={0}
                      />}
                </td>
                <td className="px-2 py-1">
                  {readonly
                    ? <span className="block text-right">{item.valorUnitario.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' })}</span>
                    : <Input
                        type="number"
                        value={item.valorUnitario}
                        onChange={e => update(idx, 'valorUnitario', parseFloat(e.target.value) || 0)}
                        className="h-8 text-right"
                        min={0}
                        step={0.01}
                      />}
                </td>
                <td className="px-2 py-1">
                  {readonly
                    ? <span className="block text-right">{item.desconto.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' })}</span>
                    : <Input
                        type="number"
                        value={item.desconto}
                        onChange={e => update(idx, 'desconto', parseFloat(e.target.value) || 0)}
                        className="h-8 text-right"
                        min={0}
                        step={0.01}
                      />}
                </td>
                <td className="px-2 py-1">
                  {readonly
                    ? <span className="block text-right">{item.freteRateado.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' })}</span>
                    : <Input
                        type="number"
                        value={item.freteRateado}
                        onChange={e => update(idx, 'freteRateado', parseFloat(e.target.value) || 0)}
                        className="h-8 text-right"
                        min={0}
                        step={0.01}
                      />}
                </td>
                <td className="px-2 py-1">
                  {readonly
                    ? <span className="block text-right">{item.impostos.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' })}</span>
                    : <Input
                        type="number"
                        value={item.impostos}
                        onChange={e => update(idx, 'impostos', parseFloat(e.target.value) || 0)}
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
