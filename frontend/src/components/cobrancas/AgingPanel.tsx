import { useEffect, useState } from 'react'
import type { AgingData } from '@/types/cobranca'

interface Props {
  fetchAging: () => Promise<AgingData>
}

function fmt(v: number) {
  return v.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' })
}

export function AgingPanel({ fetchAging }: Props) {
  const [data, setData] = useState<AgingData | null>(null)

  useEffect(() => {
    fetchAging().then(setData).catch(() => {})
  }, [fetchAging])

  if (!data) return null

  const buckets = [
    { label: 'A vencer', value: data.atual, qtd: data.qtdAtual,
      color: 'text-blue-600', bg: 'bg-blue-50 border-blue-100' },
    { label: '1–30 dias', value: data.ate30Dias, qtd: data.qtdAte30Dias,
      color: 'text-yellow-600', bg: 'bg-yellow-50 border-yellow-100' },
    { label: '31–60 dias', value: data.de31A60Dias, qtd: data.qtdDe31A60Dias,
      color: 'text-orange-600', bg: 'bg-orange-50 border-orange-100' },
    { label: '61–90 dias', value: data.de61A90Dias, qtd: data.qtdDe61A90Dias,
      color: 'text-red-500', bg: 'bg-red-50 border-red-100' },
    { label: '+90 dias', value: data.acima90Dias, qtd: data.qtdAcima90Dias,
      color: 'text-red-700', bg: 'bg-red-100 border-red-200' },
  ]

  return (
    <div className="grid grid-cols-2 sm:grid-cols-3 lg:grid-cols-5 gap-3">
      {buckets.map(b => (
        <div key={b.label} className={`rounded-lg border p-3 ${b.bg}`}>
          <p className="text-xs text-muted-foreground mb-1">{b.label}</p>
          <p className={`text-sm font-bold ${b.color}`}>{fmt(b.value)}</p>
          <p className="text-xs text-muted-foreground">{b.qtd} cobr.</p>
        </div>
      ))}
    </div>
  )
}
