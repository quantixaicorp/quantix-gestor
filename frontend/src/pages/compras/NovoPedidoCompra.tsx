import { useState, useEffect } from 'react'
import { useNavigate } from 'react-router-dom'
import { ArrowLeft } from 'lucide-react'
import { usePedidosCompra } from '@/hooks/usePedidosCompra'
import { useFornecedores } from '@/hooks/useFornecedores'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { toast } from '@/hooks/useToast'
import type { CreatePedidoCompraRequest, ItemPedidoCompraRequest } from '@/types/compras'
import { Trash2, Plus } from 'lucide-react'

const EMPTY_ITEM: ItemPedidoCompraRequest = {
  descricao: '',
  quantidade: 1,
  valorEstimado: 0,
}

export default function NovoPedidoCompra() {
  const navigate = useNavigate()
  const { create } = usePedidosCompra()
  const { fornecedores, list: listFornecedores } = useFornecedores()
  const [saving, setSaving] = useState(false)

  const [fornecedorId, setFornecedorId] = useState('')
  const [data, setData] = useState(new Date().toISOString().slice(0, 10))
  const [observacoes, setObservacoes] = useState('')
  const [itens, setItens] = useState<ItemPedidoCompraRequest[]>([{ ...EMPTY_ITEM }])

  useEffect(() => { void listFornecedores() }, [listFornecedores])

  function addItem() { setItens(prev => [...prev, { ...EMPTY_ITEM }]) }
  function removeItem(idx: number) { setItens(prev => prev.filter((_, i) => i !== idx)) }
  function updateItem(idx: number, field: keyof ItemPedidoCompraRequest, value: string | number) {
    setItens(prev => prev.map((item, i) => i === idx ? { ...item, [field]: value } : item))
  }

  const total = itens.reduce((acc, i) => acc + i.quantidade * i.valorEstimado, 0)

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault()
    if (!fornecedorId) { toast.error('Selecione um fornecedor'); return }
    if (itens.length === 0) { toast.error('Adicione ao menos um item'); return }

    setSaving(true)
    try {
      const req: CreatePedidoCompraRequest = { fornecedorId, data, observacoes: observacoes || undefined, itens }
      const pedido = await create(req)
      toast.success('Pedido criado!')
      navigate(`/compras/pedidos/${pedido.id}`)
    } catch (e) {
      toast.error(e instanceof Error ? e.message : 'Erro ao criar pedido')
    } finally {
      setSaving(false)
    }
  }

  return (
    <div className="space-y-4 max-w-3xl">
      <div className="flex items-center gap-3">
        <Button variant="ghost" size="icon" onClick={() => navigate('/compras/pedidos')}>
          <ArrowLeft size={18} />
        </Button>
        <h1 className="text-2xl font-bold">Novo Pedido de Compra</h1>
      </div>

      <form onSubmit={e => void handleSubmit(e)} className="space-y-4">
        <div className="rounded-xl border bg-card p-6 space-y-4">
          <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
            <div className="space-y-1">
              <Label>Fornecedor *</Label>
              <select
                value={fornecedorId}
                onChange={e => setFornecedorId(e.target.value)}
                className="w-full h-9 rounded-md border border-input bg-background px-3 text-sm"
                required
              >
                <option value="">Selecione...</option>
                {fornecedores.map(f => <option key={f.id} value={f.id}>{f.nome}</option>)}
              </select>
            </div>
            <div className="space-y-1">
              <Label>Data *</Label>
              <Input type="date" value={data} onChange={e => setData(e.target.value)} required />
            </div>
            <div className="sm:col-span-2 space-y-1">
              <Label>Observações</Label>
              <Input value={observacoes} onChange={e => setObservacoes(e.target.value)} placeholder="Opcional" />
            </div>
          </div>
        </div>

        <div className="rounded-xl border bg-card p-6 space-y-4">
          <h2 className="font-semibold">Itens do Pedido</h2>
          <div className="overflow-x-auto rounded-md border">
            <table className="w-full text-sm">
              <thead>
                <tr className="border-b bg-muted/50">
                  <th className="px-3 py-2 text-left font-medium">Descrição</th>
                  <th className="px-3 py-2 text-right font-medium w-24">Qtd</th>
                  <th className="px-3 py-2 text-right font-medium w-32">Valor Est.</th>
                  <th className="px-3 py-2 w-10" />
                </tr>
              </thead>
              <tbody>
                {itens.map((item, idx) => (
                  <tr key={idx} className="border-b">
                    <td className="px-2 py-1">
                      <Input
                        value={item.descricao}
                        onChange={e => updateItem(idx, 'descricao', e.target.value)}
                        className="h-8"
                        placeholder="Produto ou serviço"
                        required
                      />
                    </td>
                    <td className="px-2 py-1">
                      <Input
                        type="number"
                        value={item.quantidade}
                        onChange={e => updateItem(idx, 'quantidade', parseFloat(e.target.value) || 0)}
                        className="h-8 text-right"
                        min={0}
                      />
                    </td>
                    <td className="px-2 py-1">
                      <Input
                        type="number"
                        value={item.valorEstimado}
                        onChange={e => updateItem(idx, 'valorEstimado', parseFloat(e.target.value) || 0)}
                        className="h-8 text-right"
                        min={0}
                        step={0.01}
                      />
                    </td>
                    <td className="px-2 py-1">
                      <Button
                        type="button"
                        size="icon"
                        variant="ghost"
                        className="h-8 w-8 text-destructive"
                        onClick={() => removeItem(idx)}
                      >
                        <Trash2 size={14} />
                      </Button>
                    </td>
                  </tr>
                ))}
              </tbody>
              <tfoot>
                <tr className="border-t bg-muted/30">
                  <td colSpan={2} className="px-3 py-2 text-right font-semibold text-sm">Total Estimado:</td>
                  <td className="px-3 py-2 text-right font-bold">
                    {total.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' })}
                  </td>
                  <td />
                </tr>
              </tfoot>
            </table>
          </div>
          <Button type="button" variant="outline" size="sm" onClick={addItem}>
            <Plus size={14} className="mr-1" /> Adicionar Item
          </Button>
        </div>

        <div className="flex justify-end gap-2">
          <Button type="button" variant="outline" onClick={() => navigate('/compras/pedidos')}>
            Cancelar
          </Button>
          <Button type="submit" disabled={saving}>
            {saving ? 'Salvando...' : 'Criar Pedido'}
          </Button>
        </div>
      </form>
    </div>
  )
}
