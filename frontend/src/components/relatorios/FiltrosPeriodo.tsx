import { useState } from 'react'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'

const PERIODOS = [
  { label: 'Hoje', dias: 0 },
  { label: '7 dias', dias: 7 },
  { label: '30 dias', dias: 30 },
  { label: 'Mês atual', dias: -1 },
]

function isoDate(d: Date) { return d.toISOString().split('T')[0] }

interface Props {
  onChange: (de: string, ate: string) => void
}

export default function FiltrosPeriodo({ onChange }: Props) {
  const [de, setDe] = useState(isoDate(new Date()))
  const [ate, setAte] = useState(isoDate(new Date()))

  function aplicar(preset?: number) {
    const hoje = new Date()
    let inicio = de
    const fim = isoDate(hoje)

    if (preset === 0) {
      inicio = isoDate(hoje)
    } else if (preset !== undefined && preset > 0) {
      const d = new Date(hoje)
      d.setDate(d.getDate() - preset)
      inicio = isoDate(d)
    } else if (preset === -1) {
      inicio = isoDate(new Date(hoje.getFullYear(), hoje.getMonth(), 1))
    }

    setDe(inicio)
    setAte(fim)
    onChange(inicio, fim)
  }

  return (
    <div className="flex items-center gap-3 flex-wrap">
      {PERIODOS.map(p => (
        <Button key={p.label} size="sm" variant="outline" onClick={() => aplicar(p.dias)}>
          {p.label}
        </Button>
      ))}
      <div className="flex items-center gap-2 ml-2">
        <Input type="date" value={de} onChange={e => setDe(e.target.value)} className="h-8 w-36" />
        <span className="text-muted-foreground text-sm">até</span>
        <Input type="date" value={ate} onChange={e => setAte(e.target.value)} className="h-8 w-36" />
        <Button size="sm" onClick={() => onChange(de, ate)}>Filtrar</Button>
      </div>
    </div>
  )
}
