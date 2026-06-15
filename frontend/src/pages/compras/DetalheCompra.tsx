import { useEffect, useState } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import { ArrowLeft } from 'lucide-react'
import { useCompras } from '@/hooks/useCompras'
import { useParcelamentos } from '@/hooks/useParcelamentos'
import ItensCompraTable from '@/components/compras/ItensCompraTable'
import { Button } from '@/components/ui/button'
import { toast } from '@/hooks/useToast'
import type { CompraResponse } from '@/types/compras'
import type { ParcelamentoResponse } from '@/types/parcelamentos'

const STATUS_COLORS: Record<string, string> = {
  Rascunho: 'bg-muted text-muted-foreground',
  Confirmada: 'bg-green-100 text-green-700 dark:bg-green-900/30 dark:text-green-400',
  Cancelada: 'bg-red-100 text-red-700 dark:bg-red-900/30 dark:text-red-400',
}

const PARCELA_COLORS: Record<string, string> = {
  Pendente: 'text-muted-foreground',
  Pago: 'text-green-600',
  Cancelado: 'text-red-400 line-through',
}

export default function DetalheCompra() {
  const { id } = useParams<{ id: string }>()
  const navigate = useNavigate()
  const { get, confirmar, cancelar } = useCompras()
  const { listByCompra } = useParcelamentos()
  const [compra, setCompra] = useState<CompraResponse | null>(null)
  const [parcelamento, setParcelamento] = useState<ParcelamentoResponse | null>(null)
  const [loading, setLoading] = useState(true)

  useEffect(() => {
    if (!id) return
    setLoading(true)
    get(id)
      .then(c => {
        setCompra(c)
        if (c.parcelamento) {
          return listByCompra(c.id).then(ps => {
            if (ps.length > 0) setParcelamento(ps[0])
          })
        }
      })
      .catch(() => toast.error('Erro ao carregar compra'))
      .finally(() => setLoading(false))
  }, [id, get, listByCompra])

  async function handleConfirmar() {
    if (!compra) return
    try {
      const updated = await confirmar(compra.id)
      setCompra(updated)
      toast.success('Compra confirmada!')
      // Reload parcelamento
      const ps = await listByCompra(compra.id)
      if (ps.length > 0) setParcelamento(ps[0])
    } catch (e) {
      toast.error(e instanceof Error ? e.message : 'Erro')
    }
  }

  async function handleCancelar() {
    if (!compra || !confirm('Cancelar esta compra?')) return
    try {
      const updated = await cancelar(compra.id)
      setCompra(updated)
      toast.success('Compra cancelada.')
    } catch (e) {
      toast.error(e instanceof Error ? e.message : 'Erro')
    }
  }

  if (loading) return <p className="text-muted-foreground">Carregando...</p>
  if (!compra) return <p className="text-muted-foreground">Compra não encontrada.</p>

  return (
    <div className="space-y-4 max-w-5xl">
      <div className="flex items-center gap-3">
        <Button variant="ghost" size="icon" onClick={() => navigate('/compras')}>
          <ArrowLeft size={18} />
        </Button>
        <div className="flex-1">
          <div className="flex items-center gap-3">
            <h1 className="text-2xl font-bold">Compra #{compra.numero}</h1>
            <span className={`inline-flex items-center rounded-full px-2.5 py-0.5 text-xs font-medium ${STATUS_COLORS[compra.status] ?? ''}`}>
              {compra.status}
            </span>
          </div>
          <p className="text-sm text-muted-foreground">{new Date(compra.data).toLocaleDateString('pt-BR')} — {compra.fornecedorNome}</p>
        </div>
        <div className="flex gap-2">
          {compra.status === 'Rascunho' && (
            <Button onClick={() => void handleConfirmar()}>Confirmar Compra</Button>
          )}
          {compra.status === 'Confirmada' && (
            <Button variant="outline" onClick={() => void handleCancelar()}>Cancelar</Button>
          )}
        </div>
      </div>

      {/* Detalhes */}
      <div className="rounded-xl border bg-card p-6">
        <h2 className="font-semibold mb-4">Informações</h2>
        <div className="grid grid-cols-2 sm:grid-cols-3 gap-x-6 gap-y-2 text-sm">
          <div><span className="text-muted-foreground">Tipo:</span> {compra.tipoCompra}</div>
          <div><span className="text-muted-foreground">Nº Nota:</span> {compra.numeroNota ?? '—'}</div>
          <div><span className="text-muted-foreground">Condição:</span> {compra.condicaoPagamento}</div>
          <div><span className="text-muted-foreground">Forma:</span> {compra.formaPagamento}</div>
          <div className="col-span-2">
            <span className="text-muted-foreground">Obs.:</span> {compra.observacoes ?? '—'}
          </div>
          <div className="col-span-2 sm:col-span-3 font-semibold text-lg mt-2">
            Total: {compra.valorTotal.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' })}
          </div>
        </div>
      </div>

      {/* Itens */}
      <div className="rounded-xl border bg-card p-6">
        <h2 className="font-semibold mb-4">Itens</h2>
        <ItensCompraTable itens={compra.itens.map(i => ({
          produtoId: i.produtoId,
          descricao: i.descricao,
          destinoCompra: i.destinoCompra as 'EstoqueParaVenda' | 'ConsumoInterno' | 'AtivoImobilizado',
          quantidade: i.quantidade,
          valorUnitario: i.valorUnitario,
          desconto: i.desconto,
          freteRateado: i.freteRateado,
          impostos: i.impostos,
          categoriaFinanceira: i.categoriaFinanceira,
          centroCusto: i.centroCusto,
        }))} onChange={() => {}} readonly />
      </div>

      {/* Parcelamento */}
      {parcelamento && (
        <div className="rounded-xl border bg-card p-6">
          <div className="flex items-center justify-between mb-4">
            <h2 className="font-semibold">Parcelamento</h2>
            <span className="text-sm text-muted-foreground">{parcelamento.status}</span>
          </div>
          <div className="overflow-x-auto rounded-md border">
            <table className="w-full text-sm">
              <thead>
                <tr className="border-b bg-muted/50">
                  <th className="px-3 py-2 text-left font-medium">Parcela</th>
                  <th className="px-3 py-2 text-left font-medium">Vencimento</th>
                  <th className="px-3 py-2 text-left font-medium">Pagamento</th>
                  <th className="px-3 py-2 text-right font-medium">Valor</th>
                  <th className="px-3 py-2 text-left font-medium">Status</th>
                </tr>
              </thead>
              <tbody>
                {parcelamento.parcelas.map(p => (
                  <tr key={p.id} className="border-b last:border-0">
                    <td className="px-3 py-2">{p.numeroParcela}/{parcelamento.qtdParcelas}</td>
                    <td className="px-3 py-2">
                      <span className={p.vencido ? 'text-red-500 font-medium' : ''}>
                        {new Date(p.dataVencimento).toLocaleDateString('pt-BR')}
                      </span>
                    </td>
                    <td className="px-3 py-2 text-muted-foreground">
                      {p.dataPagamento ? new Date(p.dataPagamento).toLocaleDateString('pt-BR') : '—'}
                    </td>
                    <td className="px-3 py-2 text-right font-medium">
                      {p.valor.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' })}
                    </td>
                    <td className={`px-3 py-2 ${PARCELA_COLORS[p.status] ?? ''}`}>
                      {p.status}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </div>
      )}
    </div>
  )
}
