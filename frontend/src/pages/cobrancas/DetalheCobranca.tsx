import { useEffect, useState } from 'react'
import { useParams, useNavigate } from 'react-router-dom'
import { ChevronLeft, MessageCircle } from 'lucide-react'
import { useCobrancas } from '@/hooks/useCobrancas'
import { Button } from '@/components/ui/button'
import { cn } from '@/lib/utils'

const STATUS_STYLES: Record<string, string> = {
  Pendente:  'bg-yellow-100 text-yellow-800 dark:bg-yellow-900/40 dark:text-yellow-200',
  Pago:      'bg-green-100 text-green-800 dark:bg-green-900/40 dark:text-green-200',
  Vencido:   'bg-red-100 text-red-800 dark:bg-red-900/40 dark:text-red-200',
  Cancelado: 'bg-gray-100 text-gray-500 dark:bg-gray-800 dark:text-gray-400',
}

const FORMAS_PAGAMENTO = ['Dinheiro', 'Pix', 'Cartao', 'Outro']

export default function DetalheCobranca() {
  const { id } = useParams<{ id: string }>()
  const navigate = useNavigate()
  const { cobranca, loading, error, get, pagar, cancelar, abrirWhatsapp } = useCobrancas()

  const [modalPagar, setModalPagar] = useState(false)
  const [dataPagamento, setDataPagamento] = useState('')
  const [formaPagamento, setFormaPagamento] = useState('Pix')
  const [actionError, setActionError] = useState('')
  const [saving, setSaving] = useState(false)

  useEffect(() => { if (id) void get(id) }, [id, get])

  const fmtVal = (v: number) => v.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' })
  const fmtDate = (s: string) => new Date(s + 'T00:00:00').toLocaleDateString('pt-BR')

  const handleCancelar = async () => {
    if (!id) return
    setActionError('')
    try { await cancelar(id) } catch (e) { setActionError(e instanceof Error ? e.message : 'Erro') }
  }

  const handlePagar = async () => {
    if (!id || !dataPagamento) return
    setActionError('')
    setSaving(true)
    try {
      await pagar(id, { dataPagamento, formaPagamento })
      setModalPagar(false)
    } catch (e) { setActionError(e instanceof Error ? e.message : 'Erro ao registrar pagamento') }
    finally { setSaving(false) }
  }

  const handleWhatsapp = async () => {
    if (!id) return
    try { await abrirWhatsapp(id) } catch (e) { setActionError(e instanceof Error ? e.message : 'Erro') }
  }

  if (loading && !cobranca) return <div className="text-muted-foreground p-8">Carregando...</div>
  if (error) return <div className="text-destructive p-8">{error}</div>
  if (!cobranca) return null

  const c = cobranca

  return (
    <div className="flex flex-col gap-6 max-w-lg">
      <div className="flex items-center gap-2">
        <Button variant="ghost" size="icon" onClick={() => navigate('/cobrancas')}>
          <ChevronLeft className="h-5 w-5" />
        </Button>
        <div className="flex items-center gap-2">
          <h1 className="text-xl font-bold">Cobrança</h1>
          <span className={cn('px-2 py-0.5 rounded-full text-xs font-medium', STATUS_STYLES[c.status])}>
            {c.status}
          </span>
        </div>
      </div>

      {actionError && (
        <div className="rounded-lg border border-destructive/30 bg-destructive/10 px-4 py-3 text-sm text-destructive">{actionError}</div>
      )}

      <div className="rounded-xl border p-4 flex flex-col gap-3 text-sm">
        <div className="grid grid-cols-2 gap-x-4 gap-y-2">
          <div><span className="text-muted-foreground">Referência:</span> <span className="font-medium">{c.referencia}</span></div>
          <div><span className="text-muted-foreground">Valor:</span> <span className="font-medium">{fmtVal(c.valor)}</span></div>
          <div><span className="text-muted-foreground">Cliente:</span> {c.clienteNome}</div>
          <div><span className="text-muted-foreground">Vencimento:</span> {fmtDate(c.dataVencimento)}</div>
          {c.contratoTitulo && (
            <div className="col-span-2"><span className="text-muted-foreground">Contrato:</span> {c.contratoTitulo}</div>
          )}
          {c.dataPagamento && (
            <>
              <div><span className="text-muted-foreground">Pago em:</span> {new Date(c.dataPagamento).toLocaleDateString('pt-BR')}</div>
              <div><span className="text-muted-foreground">Forma:</span> {c.formaPagamento}</div>
            </>
          )}
          {c.observacao && (
            <div className="col-span-2"><span className="text-muted-foreground">Obs:</span> {c.observacao}</div>
          )}
        </div>
      </div>

      {(c.status === 'Pendente' || c.status === 'Vencido') && (
        <div className="flex gap-2">
          <Button onClick={() => setModalPagar(true)}>Registrar Pagamento</Button>
          <Button variant="outline" size="sm" onClick={handleWhatsapp}>
            <MessageCircle className="h-4 w-4 mr-1" /> WhatsApp
          </Button>
          <Button variant="outline" className="text-destructive" onClick={handleCancelar}>Cancelar</Button>
        </div>
      )}

      {modalPagar && (
        <div className="fixed inset-0 bg-black/50 z-50 flex items-center justify-center">
          <div className="bg-background rounded-xl border p-6 w-full max-w-sm flex flex-col gap-4">
            <h2 className="font-bold">Registrar Pagamento</h2>
            <div>
              <label className="block text-sm mb-1">Data do Pagamento *</label>
              <input type="date" value={dataPagamento} onChange={e => setDataPagamento(e.target.value)}
                className="w-full rounded-lg border border-input bg-transparent px-3 py-2 text-sm" />
            </div>
            <div>
              <label className="block text-sm mb-1">Forma de Pagamento</label>
              <select value={formaPagamento} onChange={e => setFormaPagamento(e.target.value)}
                className="w-full rounded-lg border border-input bg-transparent px-3 py-2 text-sm">
                {FORMAS_PAGAMENTO.map(f => <option key={f} value={f}>{f}</option>)}
              </select>
            </div>
            <div className="flex gap-2">
              <Button onClick={handlePagar} disabled={!dataPagamento || saving}>
                {saving ? 'Salvando...' : 'Confirmar'}
              </Button>
              <Button variant="outline" onClick={() => setModalPagar(false)}>Cancelar</Button>
            </div>
          </div>
        </div>
      )}
    </div>
  )
}
