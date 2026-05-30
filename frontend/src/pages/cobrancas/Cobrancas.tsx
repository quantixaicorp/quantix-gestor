import { useEffect, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { DollarSign, Plus } from 'lucide-react'
import { useCobrancas } from '@/hooks/useCobrancas'
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
  const { cobrancas, loading, error, list } = useCobrancas()
  const [filtroStatus, setFiltroStatus] = useState('')
  const [filtroMes, setFiltroMes] = useState('')

  useEffect(() => {
    void list({
      status: filtroStatus || undefined,
      mes: filtroMes || undefined,
    })
  }, [list, filtroStatus, filtroMes])

  const fmtVal = (v: number) => v.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' })
  const fmtDate = (s: string) => new Date(s + 'T00:00:00').toLocaleDateString('pt-BR')

  return (
    <div className="flex flex-col gap-4">
      <div className="flex items-center gap-3">
        <DollarSign className="h-5 w-5 text-primary" />
        <h1 className="text-xl font-bold">Cobranças</h1>
        <div className="ml-auto flex items-center gap-2">
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

      {error && (
        <div className="rounded-lg border border-destructive/30 bg-destructive/10 px-4 py-3 text-sm text-destructive">{error}</div>
      )}

      <div className="rounded-xl border overflow-hidden">
        <table className="w-full text-sm">
          <thead className="bg-muted/50 border-b">
            <tr>
              {['Referência', 'Cliente', 'Contrato', 'Valor', 'Vencimento', 'Status'].map(h => (
                <th key={h} className="px-4 py-3 text-left font-medium text-muted-foreground">{h}</th>
              ))}
            </tr>
          </thead>
          <tbody>
            {loading && <tr><td colSpan={6} className="text-center py-8 text-muted-foreground">Carregando...</td></tr>}
            {!loading && cobrancas.length === 0 && (
              <tr><td colSpan={6} className="text-center py-8 text-muted-foreground">Nenhuma cobrança encontrada</td></tr>
            )}
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
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  )
}
