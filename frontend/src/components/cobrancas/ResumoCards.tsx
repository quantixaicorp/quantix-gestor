import { useEffect, useState } from 'react'

interface CobrancaResumo {
  totalAReceber: number
  totalVencido: number
  totalRecebidoNoMes: number
}

interface Props {
  fetchResumo: () => Promise<CobrancaResumo>
}

const fmt = (v: number) => v.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' })

export function ResumoCards({ fetchResumo }: Props) {
  const [resumo, setResumo] = useState<CobrancaResumo | null>(null)

  useEffect(() => {
    void fetchResumo().then(setResumo).catch(() => {})
  }, [fetchResumo])

  if (!resumo) return null

  return (
    <div className="grid grid-cols-3 gap-4">
      <div className="rounded-xl border bg-card p-4">
        <p className="text-xs text-muted-foreground font-medium uppercase tracking-wide">A receber</p>
        <p className="text-2xl font-bold mt-1 text-blue-600 dark:text-blue-400">{fmt(resumo.totalAReceber)}</p>
      </div>
      <div className="rounded-xl border bg-card p-4">
        <p className="text-xs text-muted-foreground font-medium uppercase tracking-wide">Vencido</p>
        <p className="text-2xl font-bold mt-1 text-red-600 dark:text-red-400">{fmt(resumo.totalVencido)}</p>
      </div>
      <div className="rounded-xl border bg-card p-4">
        <p className="text-xs text-muted-foreground font-medium uppercase tracking-wide">Recebido no mês</p>
        <p className="text-2xl font-bold mt-1 text-green-600 dark:text-green-400">{fmt(resumo.totalRecebidoNoMes)}</p>
      </div>
    </div>
  )
}
