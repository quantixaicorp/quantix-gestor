import { useEffect, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { Plus } from 'lucide-react'
import { KpiRow } from '@/components/ui/KpiRow'
import { useContratos } from '@/hooks/useContratos'
import { Button } from '@/components/ui/button'
import { cn } from '@/lib/utils'
import type { ContratoStatus } from '@/types/contrato'

const STATUS_STYLES: Record<string, string> = {
  Rascunho:  'bg-yellow-100 text-yellow-800 dark:bg-yellow-900/40 dark:text-yellow-200',
  Ativo:     'bg-green-100 text-green-800 dark:bg-green-900/40 dark:text-green-200',
  Encerrado: 'bg-gray-100 text-gray-600 dark:bg-gray-800 dark:text-gray-400',
  Cancelado: 'bg-red-100 text-red-700 dark:bg-red-900/40 dark:text-red-300',
}

const TIPO_LABEL: Record<string, string> = {
  Recorrente: 'Recorrente',
  ParceladoPrazoFixo: 'Parcelado',
}

export default function Contratos() {
  const navigate = useNavigate()
  const { contratos, loading, error, list, fetchVencendo } = useContratos()
  const [filtroStatus, setFiltroStatus] = useState('')
  const [vencendo, setVencendo] = useState<{ id: string; numero: number; clienteNome: string; titulo: string; dataFim: string; valor: number }[]>([])

  useEffect(() => { void list(filtroStatus || undefined) }, [list, filtroStatus])

  useEffect(() => {
    void fetchVencendo(30).then(setVencendo).catch(() => {})
  }, [fetchVencendo])

  const fmtVal = (v: number) => v.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' })
  const fmtDate = (s: string) => new Date(s + 'T00:00:00').toLocaleDateString('pt-BR')

  return (
    <div className="space-y-4">
      <div className="flex flex-wrap items-center justify-between gap-2">
        <h1 className="text-2xl font-bold">Contratos</h1>
        <div className="flex items-center gap-2">
          <select
            value={filtroStatus}
            onChange={e => setFiltroStatus(e.target.value)}
            className="h-9 rounded-lg border border-input bg-transparent px-3 text-sm"
          >
            <option value="">Todos os status</option>
            {(['Rascunho', 'Ativo', 'Encerrado', 'Cancelado'] as ContratoStatus[]).map(s => (
              <option key={s} value={s}>{s}</option>
            ))}
          </select>
          <Button size="sm" onClick={() => navigate('/contratos/novo')}>
            <Plus className="h-4 w-4 mr-1" /> Novo Contrato
          </Button>
        </div>
      </div>

      <KpiRow items={[
        { label: 'Total', value: String(contratos.length) },
        { label: 'Ativos', value: String(contratos.filter(c => c.status === 'Ativo').length), color: 'text-green-600 dark:text-green-400' },
        { label: 'Vencendo (30 dias)', value: String(vencendo.length), color: vencendo.length > 0 ? 'text-yellow-600 dark:text-yellow-400' : '' },
        { label: 'Valor total ativo', value: contratos.filter(c => c.status === 'Ativo').reduce((s, c) => s + c.valor, 0).toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' }) },
      ]} />

      {error && (
        <div className="rounded-lg border border-destructive/30 bg-destructive/10 px-4 py-3 text-sm text-destructive">
          {error}
        </div>
      )}

      {vencendo.length > 0 && (
        <div className="rounded-lg border border-yellow-300 bg-yellow-50 dark:bg-yellow-950/30 px-4 py-3 text-sm text-yellow-800 dark:text-yellow-300">
          <strong>{vencendo.length} contrato(s)</strong> vence(m) nos próximos 30 dias:{' '}
          {vencendo.map(v => (
            <span key={v.id} className="inline-flex items-center gap-1 mr-3">
              <button
                className="underline font-medium hover:opacity-80"
                onClick={() => navigate(`/contratos/${v.id}`)}
              >
                {String(v.numero).padStart(3, '0')} — {v.titulo}
              </button>
              <span className="text-yellow-600 dark:text-yellow-400">
                ({new Date(v.dataFim + 'T00:00:00').toLocaleDateString('pt-BR')})
              </span>
            </span>
          ))}
        </div>
      )}

      {loading ? (
        <p className="text-muted-foreground">Carregando...</p>
      ) : contratos.length === 0 ? (
        <p className="text-center text-muted-foreground py-12">Nenhum contrato encontrado</p>
      ) : (
        <>
          {/* Mobile: card list */}
          <div className="md:hidden space-y-2">
            {contratos.map(c => (
              <div key={c.id}
                className="rounded-lg border bg-card p-4 space-y-2 cursor-pointer hover:bg-accent/50 transition-colors"
                onClick={() => navigate(`/contratos/${c.id}`)}>
                <div className="flex items-start justify-between gap-2">
                  <div className="min-w-0 flex-1">
                    <p className="font-medium truncate">{c.titulo}</p>
                    <p className="text-sm text-muted-foreground truncate">{c.clienteNome}</p>
                  </div>
                  <span className={cn('px-2 py-0.5 rounded-full text-xs font-medium shrink-0', STATUS_STYLES[c.status])}>
                    {c.status}
                  </span>
                </div>
                <div className="flex items-center justify-between text-sm">
                  <span className="text-muted-foreground">{TIPO_LABEL[c.tipoCobranca]} · {fmtDate(c.dataInicio)}</span>
                  <span className="font-semibold">{fmtVal(c.valor)}</span>
                </div>
              </div>
            ))}
          </div>

          {/* Desktop: table */}
          <div className="hidden md:block rounded-xl border overflow-hidden">
            <table className="w-full text-sm">
              <thead className="bg-muted/50 border-b">
                <tr>
                  {['Nº', 'Cliente', 'Título', 'Tipo', 'Valor', 'Status', 'Início'].map(h => (
                    <th key={h} className="px-4 py-3 text-left font-medium text-muted-foreground">{h}</th>
                  ))}
                </tr>
              </thead>
              <tbody>
                {contratos.map(c => (
                  <tr key={c.id}
                    className="border-b last:border-0 hover:bg-muted/30 cursor-pointer transition-colors"
                    onClick={() => navigate(`/contratos/${c.id}`)}>
                    <td className="px-4 py-3 font-mono text-xs text-muted-foreground">{String(c.numero).padStart(3, '0')}</td>
                    <td className="px-4 py-3 font-medium">{c.clienteNome}</td>
                    <td className="px-4 py-3">{c.titulo}</td>
                    <td className="px-4 py-3 text-muted-foreground">{TIPO_LABEL[c.tipoCobranca]}</td>
                    <td className="px-4 py-3 font-medium">{fmtVal(c.valor)}</td>
                    <td className="px-4 py-3">
                      <span className={cn('px-2 py-0.5 rounded-full text-xs font-medium', STATUS_STYLES[c.status])}>
                        {c.status}
                      </span>
                    </td>
                    <td className="px-4 py-3 text-muted-foreground">{fmtDate(c.dataInicio)}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </>
      )}
    </div>
  )
}
