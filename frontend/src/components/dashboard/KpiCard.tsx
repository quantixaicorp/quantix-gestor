import { type LucideIcon } from 'lucide-react'
import { cn } from '@/lib/utils'

interface Props {
  titulo: string
  valor: string
  icon: LucideIcon
  cor?: 'default' | 'green' | 'red' | 'yellow'
  detalhe?: string
}

const corClasses = {
  default: 'text-primary',
  green: 'text-green-600',
  red: 'text-red-600',
  yellow: 'text-yellow-600',
}

export default function KpiCard({ titulo, valor, icon: Icon, cor = 'default', detalhe }: Props) {
  return (
    <div className="rounded-lg border bg-card p-4 space-y-2">
      <div className="flex items-center justify-between gap-2">
        <p className="text-sm text-muted-foreground truncate min-w-0">{titulo}</p>
        <Icon size={18} className="text-muted-foreground shrink-0" />
      </div>
      <p className={cn('text-xl sm:text-2xl font-bold break-words tabular-nums leading-tight', corClasses[cor])}>{valor}</p>
      {detalhe && <p className="text-xs text-muted-foreground break-words">{detalhe}</p>}
    </div>
  )
}
