import { useEffect, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { Plus } from 'lucide-react'
import { KpiRow } from '@/components/ui/KpiRow'
import { useCobrancas } from '@/hooks/useCobrancas'
import { AgingPanel } from '@/components/cobrancas/AgingPanel'
import { ResumoCards } from '@/components/cobrancas/ResumoCards'
import { Button } from '@/components/ui/button'
import { cn } from '@/lib/utils'

const STATUS_STYLES: Record<string, string> = {
  Pendente:  'bg-yellow-100 text-yellow-800 dark:bg-yellow-900/40 dark:text-yellow-200',
  Pago:      'bg-green-100 text-green-800 dark:bg-green-900/40 dark:text-green-200',
  Vencido:   'bg-red-100 text-red-800 dark:bg-red-900/40 dark:text-red-200',
  Cancelado: 'bg-gray-100 text-gray-500 dark:bg-gray-800 dark:text-gray-400',
}

export default function Cobrancas() {
  const navigate = useNavigate()
  const { cobrancas, loading, error, list, fetchAging, fetchResumo, pagar } = useCobrancas()
  const [filtroStatus, setFiltroStatus] = useState('')
  const [filtroMes, setFiltroMes] = useState('')
  const [pagando, setPagando] = useState<string | null>(null)
  const [dataPagamento, setDataPagamento] = useState(new Date().toISOString().slice(0, 10))
  const [formaPagamento, setFormaPagamento] = useState('Dinheiro')
  const [salvandoPag, setSalvandoPag] = useState(false)

  useEffect(() => {
    void list({
      status: filtroStatus || undefined,
      mes: filtroMes || undefined,
    })
  }, [list, filtroStatus, filtroMes])

  const fmtVal = (v: number) => v.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' })
  const fmtDate = (s: string) => new Date(s + 'T00:00:00').toLocaleDateString('pt-BR')

  async function handlePagar(id: string) {
    setSalvandoPag(true)
    try {
      await pagar(id, {
        dataPagamento: new Date(dataPagamento + 'T12:00:00').toISOString(),
        formaPagamento,
      })
      setPagando(null)
      void list({ status: filtroStatus || undefined, mes: filtroMes || undefined })
    } catch (e) {
      alert(e instanceof Error ? e.message : 'Erro ao pagar')
    } finally {
      setSalvandoPag(false)
    }
  }

  return (
    <div className="space-y-4">
      <div className="flex flex-wrap items-center justify-between gap-2">
        <h1 className="text-2xl font-bold">Cobranças</h1>
        <div className="flex flex-wrap items-center gap-2">
          <input type="month" value={filtroMes} onChange={e => setFiltroMes(e.target.value)}
            className="h-9 rounded-lg border border-input bg-transparent px-3 text-sm" />
          <select value={filtroStatus} onChange={e => setFiltroStatus(e.target.value)}
            className="h-9 rounded-lg border border-input bg-transparent px-3 text-sm">
            <option value="">Todos</option>
            {['Pendente', 'Pago', 'Vencido', 'Cancelado'].map(s => <option key={s} value={s}>{s}</option>)}
          </select>
          <Button size="sm" onClick={() => navigate('/cobrancas/nova')}>
            <Plus className="h-4 w-4 mr-1" /> Nova Cobrança
          </Button>
        </div>
      </div>

      <KpiRow items={[
        { label: 'Total cobranças', value: String(cobrancas.length) },
        { label: 'Pendentes', value: String(cobrancas.filter(c => c.status === 'Pendente').length), color: 'text-yellow-600 dark:text-yellow-400' },
        { label: 'Pagas', value: String(cobrancas.filter(c => c.status === 'Pago').length), color: 'text-green-600 dark:text-green-400' },
        { label: 'Vencidas', value: String(cobrancas.filter(c => c.status === 'Vencido').length), color: cobrancas.some(c => c.status === 'Vencido') ? 'text-red-600 dark:text-red-400' : '' },
      ]} />

      <ResumoCards fetchResumo={fetchResumo} />

      <AgingPanel fetchAging={fetchAging} />

      {error && (
        <div className="rounded-lg border border-destructive/30 bg-destructive/10 px-4 py-3 text-sm text-destructive">{error}</div>
      )}

      {loading ? (
        <p className="text-muted-foreground">Carregando...</p>
      ) : cobrancas.length === 0 ? (
        <p className="text-center text-muted-foreground py-12">Nenhuma cobrança encontrada</p>
      ) : (
        <>
          {/* Mobile: card list */}
          <div className="md:hidden space-y-2">
            {cobrancas.map(c => (
              <div key={c.id}
                className="rounded-lg border bg-card p-4 space-y-2 cursor-pointer hover:bg-accent/50 transition-colors"
                onClick={() => navigate(`/cobrancas/${c.id}`)}>
                <div className="flex items-start justify-between gap-2">
                  <div className="min-w-0 flex-1">
                    <p className="font-medium truncate">{c.clienteNome}</p>
                    <p className="text-xs text-muted-foreground">{c.referencia}{c.contratoTitulo ? ` · ${c.contratoTitulo}` : ''}</p>
                  </div>
                  <span className={cn('px-2 py-0.5 rounded-full text-xs font-medium shrink-0', STATUS_STYLES[c.status])}>
                    {c.status}
                  </span>
                </div>
                <div className="flex items-center justify-between">
                  <span className="text-sm text-muted-foreground">Vence: {fmtDate(c.dataVencimento)}</span>
                  <span className="font-semibold">{fmtVal(c.valor)}</span>
                </div>
                {(c.status === 'Pendente' || c.status === 'Vencido') && (
                  <div onClick={e => e.stopPropagation()}>
                    <Button size="sm" variant="outline" className="w-full"
                      onClick={() => { setPagando(c.id); setDataPagamento(new Date().toISOString().slice(0, 10)) }}>
                      Registrar Pagamento
                    </Button>
                  </div>
                )}
              </div>
            ))}
          </div>

          {/* Desktop: table */}
          <div className="hidden md:block rounded-xl border overflow-hidden">
            <table className="w-full text-sm">
              <thead className="bg-muted/50 border-b">
                <tr>
                  {['Referência', 'Cliente', 'Contrato', 'Valor', 'Vencimento', 'Status', ''].map(h => (
                    <th key={h} className="px-4 py-3 text-left font-medium text-muted-foreground">{h}</th>
                  ))}
                </tr>
              </thead>
              <tbody>
                {cobrancas.map(c => (
                  <tr key={c.id}
                    className="border-b last:border-0 hover:bg-muted/30 cursor-pointer transition-colors"
                    onClick={() => navigate(`/cobrancas/${c.id}`)}>
                    <td className="px-4 py-3 font-medium">{c.referencia}</td>
                    <td className="px-4 py-3">{c.clienteNome}</td>
                    <td className="px-4 py-3 text-muted-foreground text-xs">{c.contratoTitulo ?? '—'}</td>
                    <td className="px-4 py-3 font-medium">{fmtVal(c.valor)}</td>
                    <td className="px-4 py-3 text-muted-foreground">{fmtDate(c.dataVencimento)}</td>
                    <td className="px-4 py-3">
                      <span className={cn('px-2 py-0.5 rounded-full text-xs font-medium', STATUS_STYLES[c.status])}>
                        {c.status}
                      </span>
                    </td>
                    <td className="px-4 py-3" onClick={e => e.stopPropagation()}>
                      {(c.status === 'Pendente' || c.status === 'Vencido') && (
                        <Button size="sm" variant="outline"
                          onClick={() => { setPagando(c.id); setDataPagamento(new Date().toISOString().slice(0, 10)) }}>
                          Pagar
                        </Button>
                      )}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </>
      )}

      {pagando && (
        <div className="fixed inset-0 bg-black/50 z-50 flex items-center justify-center">
          <div className="bg-background rounded-xl border p-6 w-full max-w-sm flex flex-col gap-4">
            <h2 className="font-bold">Registrar Pagamento</h2>
            <div>
              <label className="block text-sm mb-1">Data do pagamento</label>
              <input type="date" value={dataPagamento}
                onChange={e => setDataPagamento(e.target.value)}
                className="w-full rounded-lg border border-input bg-transparent px-3 py-2 text-sm" />
            </div>
            <div>
              <label className="block text-sm mb-1">Forma de pagamento</label>
              <select value={formaPagamento}
                onChange={e => setFormaPagamento(e.target.value)}
                className="w-full rounded-lg border border-input bg-transparent px-3 py-2 text-sm">
                {['Dinheiro', 'Pix', 'Cartao', 'Boleto', 'Transferencia', 'Outro'].map(f => (
                  <option key={f} value={f}>{f}</option>
                ))}
              </select>
            </div>
            <div className="flex gap-2">
              <Button onClick={() => void handlePagar(pagando)} disabled={salvandoPag}>
                {salvandoPag ? 'Salvando...' : 'Confirmar'}
              </Button>
              <Button variant="outline" onClick={() => setPagando(null)}>Cancelar</Button>
            </div>
          </div>
        </div>
      )}
    </div>
  )
}
