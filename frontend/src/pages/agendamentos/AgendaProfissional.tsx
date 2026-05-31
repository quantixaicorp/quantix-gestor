import { useState, useEffect, useCallback } from 'react'
import { useNavigate } from 'react-router-dom'
import { ChevronLeft, ChevronRight, CalendarDays } from 'lucide-react'
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

const STATUS_STYLES: Record<AgendamentoStatus, string> = {
  Agendado:   'border-l-blue-400 bg-blue-50 dark:bg-blue-950/40 text-blue-800 dark:text-blue-200',
  Confirmado: 'border-l-green-500 bg-green-50 dark:bg-green-950/40 text-green-800 dark:text-green-200',
  Concluido:  'border-l-gray-400 bg-gray-100 dark:bg-gray-800 text-gray-500',
  Cancelado:  'border-l-red-300 bg-red-50/50 dark:bg-red-950/20 text-red-400 opacity-50',
}

const STATUS_LABEL: Record<AgendamentoStatus, string> = {
  Agendado: 'Agendado', Confirmado: 'Confirmado',
  Concluido: 'Concluído', Cancelado: 'Cancelado',
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

  useEffect(() => { void listProfissionais() }, [listProfissionais])

  const load = useCallback(() => {
    const de = toDateKey(semanaInicio)
    const ate = toDateKey(addDays(semanaInicio, 6))
    void listSemana(de, ate, profissionalId || undefined)
  }, [semanaInicio, profissionalId, listSemana])

  useEffect(() => { load() }, [load])

  const weekDays = Array.from({ length: 7 }, (_, i) => addDays(semanaInicio, i))
  const todayKey = toDateKey(new Date())

  const agendamentosPorDia = weekDays.map(day =>
    agendamentos.filter(a => toDateKey(new Date(a.dataHoraInicio)) === toDateKey(day))
  )

  const totalCount = agendamentos.filter(a => a.status !== 'Cancelado').length
  const semanaLabel = `${fmtShort(semanaInicio)} — ${fmtShort(addDays(semanaInicio, 6))}`

  const profissionalNome = profissionalId
    ? (profissionais.find(p => p.id === profissionalId)?.nome ?? 'Profissional')
    : 'Todos os profissionais'

  return (
    <div className="flex flex-col gap-4">
      {/* Header */}
      <div className="flex flex-wrap items-center gap-3">
        <div className="flex items-center gap-2">
          <CalendarDays className="h-5 w-5 text-primary" />
          <h1 className="text-xl font-bold">Agenda</h1>
        </div>

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
                    {count} {count === 1 ? 'ag.' : 'ag.'}
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
  )
}
