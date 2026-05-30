import { useEffect, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { FileText, Plus } from 'lucide-react'
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
  const { contratos, loading, error, list } = useContratos()
  const [filtroStatus, setFiltroStatus] = useState('')

  useEffect(() => { void list(filtroStatus || undefined) }, [list, filtroStatus])

  const fmtVal = (v: number) => v.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' })
  const fmtDate = (s: string) => new Date(s + 'T00:00:00').toLocaleDateString('pt-BR')

  return (
    <div className="flex flex-col gap-4">
      <div className="flex items-center gap-3">
        <FileText className="h-5 w-5 text-primary" />
        <h1 className="text-xl font-bold">Contratos</h1>
        <div className="ml-auto flex items-center gap-2">
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

      {error && (
        <div className="rounded-lg border border-destructive/30 bg-destructive/10 px-4 py-3 text-sm text-destructive">
          {error}
        </div>
      )}

      <div className="rounded-xl border overflow-hidden">
        <table className="w-full text-sm">
          <thead className="bg-muted/50 border-b">
            <tr>
              {['Nº', 'Cliente', 'Título', 'Tipo', 'Valor', 'Status', 'Início'].map(h => (
                <th key={h} className="px-4 py-3 text-left font-medium text-muted-foreground">{h}</th>
              ))}
            </tr>
          </thead>
          <tbody>
            {loading && (
              <tr><td colSpan={7} className="text-center py-8 text-muted-foreground">Carregando...</td></tr>
            )}
            {!loading && contratos.length === 0 && (
              <tr><td colSpan={7} className="text-center py-8 text-muted-foreground">Nenhum contrato encontrado</td></tr>
            )}
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
    </div>
  )
}
