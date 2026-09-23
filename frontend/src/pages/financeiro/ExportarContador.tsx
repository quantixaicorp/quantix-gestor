import { useEffect, useState } from 'react'
import { FileDown, AlertCircle } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Label } from '@/components/ui/label'
import { useContabilidade } from '@/hooks/useContabilidade'
import type { AccountingSystem } from '@/types/contabilidade'

const MONTHS = [
  'Jan', 'Fev', 'Mar', 'Abr', 'Mai', 'Jun',
  'Jul', 'Ago', 'Set', 'Out', 'Nov', 'Dez',
]

export default function ExportarContador() {
  const { getSettings, downloadExport } = useContabilidade()
  const currentYear = new Date().getFullYear()
  const [year, setYear] = useState(currentYear)
  const [selectedMonths, setSelectedMonths] = useState<number[]>([])
  const [system, setSystem] = useState<AccountingSystem>('Dominio')
  const [includePending, setIncludePending] = useState(false)
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    getSettings()
      .then(s => {
        if (s.preferredAccountingSystem) {
          setSystem(s.preferredAccountingSystem)
        }
      })
      .catch(() => {/* ignora erro de settings — usa default 'Dominio' */})
  }, [getSettings])

  const toggleMonth = (m: number) =>
    setSelectedMonths(prev =>
      prev.includes(m) ? prev.filter(x => x !== m) : [...prev, m].sort((a, b) => a - b)
    )

  const handleExport = async () => {
    if (selectedMonths.length === 0) return
    setError(null)
    setLoading(true)
    try {
      const months = selectedMonths.map(m =>
        `${year}-${String(m).padStart(2, '0')}`
      )
      await downloadExport({ system, months, includePending })
    } catch (e: unknown) {
      const msg = e instanceof Error ? e.message : 'Erro ao gerar arquivo'
      setError(msg)
    } finally {
      setLoading(false)
    }
  }

  const years = [currentYear - 1, currentYear, currentYear + 1]

  return (
    <div className="p-6 max-w-2xl mx-auto">
      <div className="flex items-center gap-3 mb-6">
        <FileDown size={24} />
        <h1 className="text-2xl font-bold">Exportar para Contador</h1>
      </div>

      <div className="space-y-6">
        {/* Sistema contábil */}
        <div>
          <Label className="text-base font-semibold">Sistema Contábil</Label>
          <div className="flex gap-6 mt-2">
            {(['Dominio', 'Fortes'] as AccountingSystem[]).map(s => (
              <label key={s} className="flex items-center gap-2 cursor-pointer">
                <input
                  type="radio"
                  name="system"
                  checked={system === s}
                  onChange={() => setSystem(s)}
                  className="accent-primary"
                />
                <span className="text-sm">
                  {s === 'Dominio' ? 'Domínio (Thomson Reuters)' : 'Fortes (Fortes Tecnologia)'}
                </span>
              </label>
            ))}
          </div>
        </div>

        {/* Período */}
        <div>
          <div className="flex items-center justify-between mb-2">
            <Label className="text-base font-semibold">Período</Label>
            <select
              value={year}
              onChange={e => setYear(Number(e.target.value))}
              className="border rounded px-2 py-1 text-sm bg-background"
            >
              {years.map(y => <option key={y} value={y}>{y}</option>)}
            </select>
          </div>
          <div className="grid grid-cols-6 gap-2">
            {MONTHS.map((label, i) => {
              const m = i + 1
              const selected = selectedMonths.includes(m)
              return (
                <button
                  key={m}
                  onClick={() => toggleMonth(m)}
                  className={`py-2 rounded text-sm font-medium border transition-colors ${
                    selected
                      ? 'bg-primary text-primary-foreground border-primary'
                      : 'bg-background border-border hover:bg-muted'
                  }`}
                >
                  {label}
                </button>
              )
            })}
          </div>
          {selectedMonths.length > 0 && (
            <p className="text-xs text-muted-foreground mt-1">
              {selectedMonths.length} {selectedMonths.length === 1 ? 'mês selecionado' : 'meses selecionados'}
            </p>
          )}
        </div>

        {/* Incluir pendentes */}
        <div className="flex items-center gap-2">
          <input
            type="checkbox"
            id="includePending"
            checked={includePending}
            onChange={e => setIncludePending(e.target.checked)}
            className="accent-primary"
          />
          <Label htmlFor="includePending" className="cursor-pointer font-normal">
            Incluir lançamentos pendentes
          </Label>
        </div>

        {/* Banner de erro */}
        {error && (
          <div className="flex items-start gap-2 p-3 rounded border border-red-400 bg-red-50 text-red-800 dark:bg-red-950/30 dark:border-red-700 dark:text-red-300">
            <AlertCircle size={16} className="mt-0.5 shrink-0" />
            <p className="text-sm">{error}</p>
          </div>
        )}

        {/* Botão exportar */}
        <Button
          onClick={handleExport}
          disabled={loading || selectedMonths.length === 0}
          className="w-full"
        >
          <FileDown size={16} className="mr-2" />
          {loading ? 'Gerando arquivo...' : 'Exportar'}
        </Button>
      </div>
    </div>
  )
}
