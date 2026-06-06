import { useEffect, useState, useCallback } from 'react'
import { useParams, useNavigate } from 'react-router-dom'
import { useProfissionais } from '@/hooks/useProfissionais'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { toast } from '@/hooks/useToast'
import type { DisponibilidadeItem, DisponibilidadePeriodoResponse, TipoPeriodo } from '@/types/agendamento'

const DIAS = ['Dom', 'Seg', 'Ter', 'Qua', 'Qui', 'Sex', 'Sáb']

// ── utilitários de período ────────────────────────────────────────────────────

function hoje(): Date { return new Date() }

function fmt(d: Date): string {
  return d.toISOString().slice(0, 10)
}

function labelPeriodo(tipo: TipoPeriodo, inicio: Date): string {
  const ano = inicio.getFullYear()
  switch (tipo) {
    case 'semana': {
      const fim = new Date(inicio); fim.setDate(fim.getDate() + 6)
      return `${inicio.toLocaleDateString('pt-BR', { day: '2-digit', month: 'short' })} – ${fim.toLocaleDateString('pt-BR', { day: '2-digit', month: 'short', year: 'numeric' })}`
    }
    case 'mes':
      return inicio.toLocaleDateString('pt-BR', { month: 'long', year: 'numeric' })
    case 'trimestre': {
      const q = Math.floor(inicio.getMonth() / 3) + 1
      return `${q}º trimestre ${ano}`
    }
    case 'semestre': {
      const s = inicio.getMonth() < 6 ? 1 : 2
      return `${s}º semestre ${ano}`
    }
    case 'ano':
      return `Ano ${ano}`
  }
}

function calcularPeriodo(tipo: TipoPeriodo, ref: Date): { inicio: Date; fim: Date } {
  const r = new Date(ref)
  r.setHours(0, 0, 0, 0)
  switch (tipo) {
    case 'semana': {
      const dow = r.getDay()  // 0=dom
      const seg = new Date(r); seg.setDate(r.getDate() - dow + (dow === 0 ? -6 : 1))
      const dom = new Date(seg); dom.setDate(seg.getDate() + 6)
      return { inicio: seg, fim: dom }
    }
    case 'mes': {
      const ini = new Date(r.getFullYear(), r.getMonth(), 1)
      const fim = new Date(r.getFullYear(), r.getMonth() + 1, 0)
      return { inicio: ini, fim }
    }
    case 'trimestre': {
      const q = Math.floor(r.getMonth() / 3)
      const ini = new Date(r.getFullYear(), q * 3, 1)
      const fim = new Date(r.getFullYear(), q * 3 + 3, 0)
      return { inicio: ini, fim }
    }
    case 'semestre': {
      const s = r.getMonth() < 6 ? 0 : 6
      const ini = new Date(r.getFullYear(), s, 1)
      const fim = new Date(r.getFullYear(), s + 6, 0)
      return { inicio: ini, fim }
    }
    case 'ano': {
      const ini = new Date(r.getFullYear(), 0, 1)
      const fim = new Date(r.getFullYear(), 11, 31)
      return { inicio: ini, fim }
    }
  }
}

function avancarRef(tipo: TipoPeriodo, ref: Date, direcao: 1 | -1): Date {
  const r = new Date(ref)
  switch (tipo) {
    case 'semana': r.setDate(r.getDate() + direcao * 7); break
    case 'mes':    r.setMonth(r.getMonth() + direcao); break
    case 'trimestre': r.setMonth(r.getMonth() + direcao * 3); break
    case 'semestre':  r.setMonth(r.getMonth() + direcao * 6); break
    case 'ano':    r.setFullYear(r.getFullYear() + direcao); break
  }
  return r
}

// ── componente ────────────────────────────────────────────────────────────────

export default function DisponibilidadeProfissional() {
  const { id } = useParams<{ id: string }>()
  const navigate = useNavigate()
  const { listPeriodos, getDisponibilidade, saveDisponibilidade, deletePeriodo } = useProfissionais()

  const [tipo, setTipo] = useState<TipoPeriodo>('semana')
  const [ref, setRef] = useState<Date>(hoje())
  const [faixas, setFaixas] = useState<DisponibilidadeItem[]>([])
  const [periodos, setPeriodos] = useState<DisponibilidadePeriodoResponse[]>([])
  const [loading, setLoading] = useState(false)
  const [saving, setSaving] = useState(false)

  const { inicio, fim } = calcularPeriodo(tipo, ref)
  const dataInicio = fmt(inicio)
  const dataFim = fmt(fim)

  // Carrega faixas do período selecionado
  const carregarPeriodo = useCallback(async () => {
    if (!id) return
    setLoading(true)
    try {
      const res = await getDisponibilidade(id, dataInicio, dataFim)
      setFaixas(res.faixas)
    } catch {
      setFaixas([])
    } finally {
      setLoading(false)
    }
  }, [id, getDisponibilidade, dataInicio, dataFim])

  // Lista todos os períodos cadastrados (sidebar)
  const carregarLista = useCallback(async () => {
    if (!id) return
    try {
      const res = await listPeriodos(id)
      setPeriodos(res)
    } catch { /* silencioso */ }
  }, [id, listPeriodos])

  useEffect(() => { void carregarPeriodo() }, [carregarPeriodo])
  useEffect(() => { void carregarLista() }, [carregarLista])

  function adicionarFaixa(dia: number) {
    setFaixas(prev => [...prev, { diaSemana: dia, horaInicio: '08:00', horaFim: '18:00' }])
  }

  function removerFaixa(index: number) {
    setFaixas(prev => prev.filter((_, i) => i !== index))
  }

  function atualizarFaixa(index: number, field: keyof DisponibilidadeItem, value: string | number) {
    setFaixas(prev => prev.map((f, i) => i === index ? { ...f, [field]: value } : f))
  }

  async function salvar() {
    if (!id) return
    setSaving(true)
    try {
      await saveDisponibilidade(id, { dataInicio, dataFim, faixas })
      toast.success('Disponibilidade salva!')
      void carregarLista()
    } catch (e) {
      toast.error(e instanceof Error ? e.message : 'Erro ao salvar')
    } finally {
      setSaving(false)
    }
  }

  async function excluirPeriodo() {
    if (!id || !confirm('Excluir todos os horários deste período?')) return
    try {
      await deletePeriodo(id, dataInicio, dataFim)
      setFaixas([])
      toast.success('Período excluído')
      void carregarLista()
    } catch (e) {
      toast.error(e instanceof Error ? e.message : 'Erro ao excluir')
    }
  }

  function irParaPeriodo(p: DisponibilidadePeriodoResponse) {
    const d = new Date(p.dataInicio + 'T12:00:00')
    setRef(d)
  }

  const TIPOS: { value: TipoPeriodo; label: string }[] = [
    { value: 'semana', label: 'Semana' },
    { value: 'mes', label: 'Mês' },
    { value: 'trimestre', label: 'Trimestre' },
    { value: 'semestre', label: 'Semestre' },
    { value: 'ano', label: 'Ano' },
  ]

  return (
    <div className="flex gap-6 max-w-5xl">
      {/* Sidebar — períodos cadastrados */}
      <aside className="w-52 shrink-0">
        <p className="text-xs font-semibold text-muted-foreground uppercase tracking-wide mb-2">
          Períodos cadastrados
        </p>
        {periodos.length === 0 ? (
          <p className="text-xs text-muted-foreground">Nenhum período ainda</p>
        ) : (
          <ul className="space-y-1">
            {periodos.map(p => (
              <li key={`${p.dataInicio}-${p.dataFim}`}>
                <button
                  onClick={() => irParaPeriodo(p)}
                  className={`w-full text-left rounded-md px-2 py-1.5 text-xs hover:bg-accent transition-colors
                    ${p.dataInicio === dataInicio && p.dataFim === dataFim ? 'bg-accent font-medium' : ''}`}
                >
                  <span className="block">{new Date(p.dataInicio + 'T12:00:00').toLocaleDateString('pt-BR', { day: '2-digit', month: 'short', year: '2-digit' })}</span>
                  <span className="block text-muted-foreground">até {new Date(p.dataFim + 'T12:00:00').toLocaleDateString('pt-BR', { day: '2-digit', month: 'short', year: '2-digit' })}</span>
                </button>
              </li>
            ))}
          </ul>
        )}
      </aside>

      {/* Editor principal */}
      <div className="flex-1 space-y-5">
        <div className="flex flex-wrap items-center justify-between gap-2">
          <h1 className="text-2xl font-bold">Disponibilidade</h1>
          <Button variant="ghost" onClick={() => navigate('/profissionais')}>← Voltar</Button>
        </div>

        {/* Seletor de tipo + navegação */}
        <div className="rounded-lg border bg-card p-4 space-y-3">
          <div className="flex gap-1 flex-wrap">
            {TIPOS.map(t => (
              <button
                key={t.value}
                onClick={() => setTipo(t.value)}
                className={`px-3 py-1 rounded-md text-sm font-medium transition-colors
                  ${tipo === t.value ? 'bg-primary text-primary-foreground' : 'bg-muted hover:bg-accent'}`}
              >
                {t.label}
              </button>
            ))}
          </div>

          <div className="flex items-center gap-3">
            <Button variant="outline" size="sm" onClick={() => setRef(avancarRef(tipo, ref, -1))}>‹</Button>
            <span className="text-sm font-medium min-w-48 text-center">{labelPeriodo(tipo, inicio)}</span>
            <Button variant="outline" size="sm" onClick={() => setRef(avancarRef(tipo, ref, 1))}>›</Button>
            <Button variant="ghost" size="sm" onClick={() => setRef(hoje())} className="text-xs text-muted-foreground">
              Hoje
            </Button>
          </div>

          <p className="text-xs text-muted-foreground">
            Vigência: {new Date(dataInicio + 'T12:00:00').toLocaleDateString('pt-BR')} até {new Date(dataFim + 'T12:00:00').toLocaleDateString('pt-BR')}
          </p>
        </div>

        {/* Faixas de horário por dia */}
        {loading ? (
          <p className="text-sm text-muted-foreground">Carregando...</p>
        ) : (
          <div className="space-y-3">
            {DIAS.map((dia, diaIdx) => {
              const faixasDia = faixas.filter(f => f.diaSemana === diaIdx)
              return (
                <div key={diaIdx} className="rounded-md border p-3">
                  <div className="flex items-center justify-between mb-1">
                    <span className="font-medium text-sm w-8">{dia}</span>
                    <Button variant="outline" size="sm" onClick={() => adicionarFaixa(diaIdx)}>
                      + Faixa
                    </Button>
                  </div>
                  {faixasDia.length === 0 && (
                    <p className="text-xs text-muted-foreground">Sem horários neste dia</p>
                  )}
                  {faixasDia.map((faixa) => {
                    const idx = faixas.indexOf(faixa)
                    return (
                      <div key={idx} className="flex items-center gap-2 mt-2">
                        <Input
                          type="time"
                          value={faixa.horaInicio}
                          onChange={e => atualizarFaixa(idx, 'horaInicio', e.target.value)}
                          className="w-28"
                        />
                        <span className="text-xs text-muted-foreground">até</span>
                        <Input
                          type="time"
                          value={faixa.horaFim}
                          onChange={e => atualizarFaixa(idx, 'horaFim', e.target.value)}
                          className="w-28"
                        />
                        <Button variant="ghost" size="sm" onClick={() => removerFaixa(idx)}>✕</Button>
                      </div>
                    )
                  })}
                </div>
              )
            })}
          </div>
        )}

        <div className="flex gap-2">
          <Button onClick={salvar} disabled={saving}>
            {saving ? 'Salvando...' : 'Salvar período'}
          </Button>
          {faixas.length > 0 && (
            <Button variant="destructive" size="sm" onClick={excluirPeriodo}>
              Excluir período
            </Button>
          )}
        </div>
      </div>
    </div>
  )
}
