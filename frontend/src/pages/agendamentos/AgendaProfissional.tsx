import { useState, useEffect, useCallback } from 'react'
import { useNavigate } from 'react-router-dom'
import { ChevronLeft, ChevronRight, CalendarDays, Plus, Clock, User } from 'lucide-react'
import { useAgendamentos } from '@/hooks/useAgendamentos'
import { useProfissionais } from '@/hooks/useProfissionais'
import { Button } from '@/components/ui/button'
import { cn } from '@/lib/utils'
import type { AgendamentoListItem, AgendamentoStatus } from '@/types/agendamento'

const HOUR_START = 7
const HOUR_END = 22
const PX_PER_MIN = 1.4
const HOURS = Array.from({ length: HOUR_END - HOUR_START + 1 }, (_, i) => HOUR_START + i)
const DIAS_SEMANA = ['Seg', 'Ter', 'Qua', 'Qui', 'Sex', 'Sáb', 'Dom']
const DIAS_SEMANA_FULL = ['Segunda', 'Terça', 'Quarta', 'Quinta', 'Sexta', 'Sábado', 'Domingo']

const STATUS_STYLES: Record<AgendamentoStatus, string> = {
  Agendado:               'border-l-blue-400 bg-blue-50 dark:bg-blue-950/40 text-blue-800 dark:text-blue-200',
  AguardandoConfirmacao:  'border-l-yellow-400 bg-yellow-50 dark:bg-yellow-950/40 text-yellow-800 dark:text-yellow-200',
  Confirmado:             'border-l-green-500 bg-green-50 dark:bg-green-950/40 text-green-800 dark:text-green-200',
  Concluido:              'border-l-gray-400 bg-gray-100 dark:bg-gray-800 text-gray-500',
  Cancelado:              'border-l-red-300 bg-red-50/50 dark:bg-red-950/20 text-red-400 opacity-50',
}

const STATUS_CARD: Record<AgendamentoStatus, string> = {
  Agendado:               'border-l-4 border-l-blue-400 bg-blue-50/60 dark:bg-blue-950/30',
  AguardandoConfirmacao:  'border-l-4 border-l-yellow-400 bg-yellow-50/60 dark:bg-yellow-950/30',
  Confirmado:             'border-l-4 border-l-green-500 bg-green-50/60 dark:bg-green-950/30',
  Concluido:              'border-l-4 border-l-gray-400 bg-gray-100/60 dark:bg-gray-800/60',
  Cancelado:              'border-l-4 border-l-red-300 bg-red-50/40 dark:bg-red-950/20 opacity-60',
}

const STATUS_LABEL: Record<AgendamentoStatus, string> = {
  Agendado: 'Agendado', AguardandoConfirmacao: 'Aguardando',
  Confirmado: 'Confirmado', Concluido: 'Concluído', Cancelado: 'Cancelado',
}

function getMonday(date: Date): Date {
  const d = new Date(date)
  d.setHours(0, 0, 0, 0)
  const day = d.getDay()
  d.setDate(d.getDate() + (day === 0 ? -6 : 1 - day))
  return d
}

function addDays(date: Date, days: number): Date {
  const d = new Date(date)
  d.setDate(d.getDate() + days)
  return d
}

function toDateKey(date: Date): string {
  return `${date.getFullYear()}-${String(date.getMonth() + 1).padStart(2, '0')}-${String(date.getDate()).padStart(2, '0')}`
}

function fmtTime(isoStr: string): string {
  return new Date(isoStr).toLocaleTimeString('pt-BR', { hour: '2-digit', minute: '2-digit' })
}

function fmtShort(date: Date): string {
  return date.toLocaleDateString('pt-BR', { day: '2-digit', month: 'short' })
}

function fmtFullDate(date: Date): string {
  return date.toLocaleDateString('pt-BR', { weekday: 'short', day: '2-digit', month: 'long', year: 'numeric' })
}

function getCardMetrics(a: AgendamentoListItem) {
  const start = new Date(a.dataHoraInicio)
  const end = new Date(a.dataHoraFim)
  const startMin = start.getHours() * 60 + start.getMinutes()
  const endMin = end.getHours() * 60 + end.getMinutes()
  const clampedStart = Math.max(startMin, HOUR_START * 60)
  const clampedEnd = Math.min(endMin, HOUR_END * 60)
  return {
    top: (clampedStart - HOUR_START * 60) * PX_PER_MIN,
    height: Math.max((clampedEnd - clampedStart) * PX_PER_MIN, 22),
  }
}

const TOTAL_HEIGHT = (HOUR_END - HOUR_START) * 60 * PX_PER_MIN

export default function AgendaProfissional() {
  const navigate = useNavigate()
  const { agendamentos, loading, error, listSemana } = useAgendamentos()
  const { profissionais, list: listProfissionais } = useProfissionais()

  const [semanaInicio, setSemanaInicio] = useState(() => getMonday(new Date()))
  const [profissionalId, setProfissionalId] = useState<string>('')
  const [diaSelecionado, setDiaSelecionado] = useState(() => toDateKey(new Date()))

  useEffect(() => { void listProfissionais() }, [listProfissionais])

  const load = useCallback(() => {
    const de = toDateKey(semanaInicio)
    const ate = toDateKey(addDays(semanaInicio, 6))
    void listSemana(de, ate, profissionalId || undefined)
  }, [semanaInicio, profissionalId, listSemana])

  useEffect(() => { load() }, [load])

  // Keep selected day in sync when week changes
  useEffect(() => {
    const weekKeys = Array.from({ length: 7 }, (_, i) => toDateKey(addDays(semanaInicio, i)))
    if (!weekKeys.includes(diaSelecionado)) {
      setDiaSelecionado(weekKeys[0])
    }
  }, [semanaInicio, diaSelecionado])

  const weekDays = Array.from({ length: 7 }, (_, i) => addDays(semanaInicio, i))
  const todayKey = toDateKey(new Date())

  const agendamentosPorDia = weekDays.map(day =>
    agendamentos.filter(a => toDateKey(new Date(a.dataHoraInicio)) === toDateKey(day))
  )

  const totalCount = agendamentos.filter(a => a.status !== 'Cancelado').length
  const semanaLabel = `${fmtShort(semanaInicio)} — ${fmtShort(addDays(semanaInicio, 6))}`

  const profissionalNome = profissionalId
    ? (profissionais.find(p => p.id === profissionalId)?.nome ?? 'Profissional')
    : 'Todos'

  const diaSelecionadoIdx = weekDays.findIndex(d => toDateKey(d) === diaSelecionado)
  const agendamentosHoje = diaSelecionadoIdx >= 0
    ? agendamentosPorDia[diaSelecionadoIdx].sort((a, b) => a.dataHoraInicio.localeCompare(b.dataHoraInicio))
    : []

  function mudarDiaMobile(delta: number) {
    const cur = new Date(diaSelecionado + 'T12:00:00')
    cur.setDate(cur.getDate() + delta)
    const newKey = toDateKey(cur)
    setDiaSelecionado(newKey)
    // Update week if needed
    const newMonday = getMonday(cur)
    if (toDateKey(newMonday) !== toDateKey(semanaInicio)) {
      setSemanaInicio(newMonday)
    }
  }

  return (
    <div className="flex flex-col gap-4">
      {/* Header */}
      <div className="flex flex-wrap items-center justify-between gap-2">
        <div className="flex items-center gap-2">
          <CalendarDays className="h-5 w-5 text-primary" />
          <h1 className="text-xl font-bold">Agenda</h1>
        </div>
        <Button size="sm" onClick={() => navigate('/agendamentos/novo')}>
          <Plus size={15} className="mr-1" /> Novo
        </Button>
      </div>

      {/* ── MOBILE VIEW ── */}
      <div className="md:hidden flex flex-col gap-3">
        {/* Week strip */}
        <div className="flex items-center gap-1">
          <button
            onClick={() => mudarDiaMobile(-1)}
            className="p-1.5 rounded-lg border bg-card hover:bg-accent transition-colors shrink-0"
          >
            <ChevronLeft size={16} />
          </button>
          <div className="flex-1 grid grid-cols-7 gap-0.5 overflow-hidden">
            {weekDays.map((day, i) => {
              const key = toDateKey(day)
              const isToday = key === todayKey
              const isSelected = key === diaSelecionado
              const count = agendamentosPorDia[i].filter(a => a.status !== 'Cancelado').length
              return (
                <button
                  key={key}
                  onClick={() => setDiaSelecionado(key)}
                  className={cn(
                    'flex flex-col items-center py-1.5 rounded-lg transition-colors',
                    isSelected
                      ? 'bg-primary text-primary-foreground'
                      : isToday
                        ? 'bg-primary/10 text-primary'
                        : 'hover:bg-muted text-muted-foreground'
                  )}
                >
                  <span className="text-[9px] font-semibold uppercase">{DIAS_SEMANA[i]}</span>
                  <span className={cn('text-sm font-bold leading-tight', isSelected && 'text-primary-foreground')}>
                    {day.getDate()}
                  </span>
                  {count > 0 && (
                    <span className={cn(
                      'w-1 h-1 rounded-full mt-0.5',
                      isSelected ? 'bg-primary-foreground' : 'bg-primary'
                    )} />
                  )}
                </button>
              )
            })}
          </div>
          <button
            onClick={() => mudarDiaMobile(1)}
            className="p-1.5 rounded-lg border bg-card hover:bg-accent transition-colors shrink-0"
          >
            <ChevronRight size={16} />
          </button>
        </div>

        {/* Day label */}
        <div className="flex items-center justify-between">
          <span className="text-sm font-medium text-muted-foreground capitalize">
            {diaSelecionadoIdx >= 0
              ? `${DIAS_SEMANA_FULL[diaSelecionadoIdx]}, ${weekDays[diaSelecionadoIdx]?.toLocaleDateString('pt-BR', { day: 'numeric', month: 'long' })}`
              : ''}
          </span>
          {diaSelecionado !== todayKey && (
            <button
              onClick={() => {
                setDiaSelecionado(todayKey)
                setSemanaInicio(getMonday(new Date()))
              }}
              className="text-xs text-primary font-medium"
            >
              Hoje
            </button>
          )}
        </div>

        {/* Professional filter */}
        {profissionais.length > 1 && (
          <select
            value={profissionalId}
            onChange={e => setProfissionalId(e.target.value)}
            className="h-9 w-full rounded-lg border border-input bg-transparent px-3 text-sm"
          >
            <option value="">Todos os profissionais</option>
            {profissionais.map(p => (
              <option key={p.id} value={p.id}>{p.nome}</option>
            ))}
          </select>
        )}

        {/* Appointment list for selected day */}
        {loading ? (
          <div className="flex items-center justify-center py-12">
            <p className="text-sm text-muted-foreground">Carregando...</p>
          </div>
        ) : agendamentosHoje.length === 0 ? (
          <div className="flex flex-col items-center justify-center py-12 gap-2 text-muted-foreground">
            <CalendarDays size={36} className="opacity-30" />
            <p className="text-sm">Nenhum agendamento</p>
          </div>
        ) : (
          <div className="space-y-2">
            {agendamentosHoje.map(a => (
              <button
                key={a.id}
                onClick={() => navigate(`/agendamentos/${a.id}`)}
                className={cn(
                  'w-full text-left rounded-xl border p-3 space-y-1.5 hover:brightness-95 active:scale-[0.99] transition-all',
                  STATUS_CARD[a.status]
                )}
              >
                <div className="flex items-start justify-between gap-2">
                  <p className="font-semibold text-sm leading-tight truncate">{a.clienteNome}</p>
                  <span className="text-[10px] font-semibold shrink-0 opacity-70">{STATUS_LABEL[a.status]}</span>
                </div>
                <div className="flex items-center gap-1 text-xs opacity-75">
                  <Clock size={11} className="shrink-0" />
                  <span>{fmtTime(a.dataHoraInicio)} – {fmtTime(a.dataHoraFim)}</span>
                </div>
                <p className="text-xs opacity-70 truncate">{a.servicoNome}</p>
                {!profissionalId && (
                  <div className="flex items-center gap-1 text-xs opacity-70">
                    <User size={11} className="shrink-0" />
                    <span className="truncate">{a.profissionalNome}</span>
                  </div>
                )}
              </button>
            ))}
          </div>
        )}
      </div>

      {/* ── DESKTOP VIEW ── */}
      <div className="hidden md:flex md:flex-col gap-4">
        {/* Controls */}
        <div className="flex flex-wrap items-center gap-3">
          <select
            value={profissionalId}
            onChange={e => setProfissionalId(e.target.value)}
            className="h-9 rounded-lg border border-input bg-transparent px-3 py-1 text-sm min-w-[180px]"
          >
            <option value="">Todos os profissionais</option>
            {profissionais.map(p => (
              <option key={p.id} value={p.id}>{p.nome}</option>
            ))}
          </select>

          <div className="flex items-center gap-1 ml-auto">
            <Button variant="outline" size="sm"
              onClick={() => setSemanaInicio(getMonday(new Date()))}>
              Hoje
            </Button>
            <Button variant="outline" size="icon" className="h-8 w-8"
              onClick={() => setSemanaInicio(prev => addDays(prev, -7))}>
              <ChevronLeft size={14} />
            </Button>
            <span className="text-sm font-medium w-44 text-center">{semanaLabel}</span>
            <Button variant="outline" size="icon" className="h-8 w-8"
              onClick={() => setSemanaInicio(prev => addDays(prev, 7))}>
              <ChevronRight size={14} />
            </Button>
          </div>
        </div>

        {/* Summary strip */}
        <div className="flex items-center gap-4 text-sm text-muted-foreground">
          <span>{profissionalNome} · {totalCount} {totalCount === 1 ? 'agendamento' : 'agendamentos'} na semana</span>
          <div className="flex items-center gap-3 ml-auto text-[11px]">
            {(['Agendado', 'Confirmado', 'Concluido', 'Cancelado'] as AgendamentoStatus[]).map(s => (
              <span key={s} className="flex items-center gap-1">
                <span className={cn('inline-block w-2.5 h-2.5 rounded-sm border-l-2',
                  STATUS_STYLES[s].split(' ').slice(0, 3).join(' '))} />
                {STATUS_LABEL[s]}
              </span>
            ))}
          </div>
        </div>

        {error && (
          <div className="rounded-lg border border-destructive/30 bg-destructive/10 px-4 py-3 text-sm text-destructive">
            Erro ao carregar agenda: {error}
          </div>
        )}

        {/* Calendar grid */}
        <div className="rounded-xl border overflow-auto">
          {/* Day headers */}
          <div className="sticky top-0 z-10 bg-background border-b flex min-w-[700px]">
            <div className="w-14 shrink-0 border-r bg-background" />
            {weekDays.map((day, i) => {
              const isToday = toDateKey(day) === todayKey
              const count = agendamentosPorDia[i].filter(a => a.status !== 'Cancelado').length
              return (
                <div key={i} className={cn(
                  'flex-1 py-2 text-center border-r last:border-r-0',
                  isToday && 'bg-primary/5'
                )}>
                  <div className={cn('text-[11px] font-semibold uppercase tracking-wide',
                    isToday ? 'text-primary' : 'text-muted-foreground')}>
                    {DIAS_SEMANA[i]}
                  </div>
                  <div className={cn(
                    'mx-auto mt-1 h-7 w-7 flex items-center justify-center rounded-full text-sm font-medium',
                    isToday
                      ? 'bg-primary text-primary-foreground'
                      : 'text-foreground'
                  )}>
                    {day.getDate()}
                  </div>
                  {count > 0 && (
                    <div className="mt-0.5 text-[10px] text-muted-foreground">
                      {count} ag.
                    </div>
                  )}
                </div>
              )
            })}
          </div>

          {/* Time grid */}
          <div className="flex min-w-[700px] relative">
            {loading && (
              <div className="absolute inset-0 bg-background/60 z-20 flex items-center justify-center">
                <span className="text-sm text-muted-foreground">Carregando...</span>
              </div>
            )}

            {/* Hours column */}
            <div className="w-14 shrink-0 border-r relative select-none" style={{ height: TOTAL_HEIGHT }}>
              {HOURS.map(h => (
                <div
                  key={h}
                  style={{ top: (h - HOUR_START) * 60 * PX_PER_MIN - 7 }}
                  className="absolute right-2 text-[10px] text-muted-foreground tabular-nums"
                >
                  {String(h).padStart(2, '0')}:00
                </div>
              ))}
            </div>

            {!loading && agendamentos.length === 0 && (
              <div className="absolute inset-0 flex items-center justify-center pointer-events-none z-10">
                <p className="text-sm text-muted-foreground">Nenhum agendamento nesta semana</p>
              </div>
            )}

            {/* Day columns */}
            {weekDays.map((day, di) => {
              const isToday = toDateKey(day) === todayKey
              const dayItems = agendamentosPorDia[di]
              return (
                <div key={di}
                  className={cn('flex-1 relative border-r last:border-r-0', isToday && 'bg-primary/[0.015]')}
                  style={{ height: TOTAL_HEIGHT }}
                >
                  {/* Hour lines */}
                  {HOURS.map(h => (
                    <div key={h}
                      style={{ top: (h - HOUR_START) * 60 * PX_PER_MIN }}
                      className="absolute inset-x-0 border-t border-border/40 pointer-events-none"
                    />
                  ))}
                  {/* Half-hour dashed lines */}
                  {HOURS.slice(0, -1).map(h => (
                    <div key={h}
                      style={{ top: ((h - HOUR_START) * 60 + 30) * PX_PER_MIN }}
                      className="absolute inset-x-0 border-t border-border/20 border-dashed pointer-events-none"
                    />
                  ))}

                  {/* Appointments */}
                  {dayItems.map(a => {
                    const { top, height } = getCardMetrics(a)
                    const showTime = height >= 26
                    const showClient = height >= 36
                    const showService = height >= 52

                    return (
                      <button
                        key={a.id}
                        title={`${fmtFullDate(new Date(a.dataHoraInicio))}\n${a.clienteNome} · ${a.servicoNome}\n${fmtTime(a.dataHoraInicio)} — ${fmtTime(a.dataHoraFim)}`}
                        onClick={() => navigate(`/agendamentos/${a.id}`)}
                        style={{ top, height, left: 3, right: 3 }}
                        className={cn(
                          'absolute rounded border-l-[3px] px-1.5 text-left overflow-hidden',
                          'hover:brightness-95 active:scale-[0.99] transition-all cursor-pointer',
                          STATUS_STYLES[a.status]
                        )}
                      >
                        {showTime && (
                          <div className="text-[9px] font-semibold leading-tight truncate opacity-75 mt-0.5">
                            {fmtTime(a.dataHoraInicio)} — {fmtTime(a.dataHoraFim)}
                          </div>
                        )}
                        {showClient && (
                          <div className="text-[11px] font-semibold leading-tight truncate">
                            {a.clienteNome}
                          </div>
                        )}
                        {showService && (
                          <div className="text-[10px] leading-tight truncate opacity-70">
                            {a.servicoNome}
                          </div>
                        )}
                      </button>
                    )
                  })}
                </div>
              )
            })}
          </div>
        </div>
      </div>
    </div>
  )
}
