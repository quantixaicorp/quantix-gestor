import { useEffect, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { ChevronLeft, ChevronRight, CalendarDays, Plus, Clock, User } from 'lucide-react'
import { useAgendamentos } from '@/hooks/useAgendamentos'
import { useProfissionais } from '@/hooks/useProfissionais'
import { Button } from '@/components/ui/button'
import { cn } from '@/lib/utils'
import type { AgendamentoListItem, AgendamentoStatus } from '@/types/agendamento'

const STATUS_CONFIG: Record<AgendamentoStatus, { label: string; className: string }> = {
  Agendado:              { label: 'Agendado',    className: 'bg-blue-100 text-blue-700 border border-blue-200 dark:bg-blue-900/30 dark:text-blue-300 dark:border-blue-800' },
  Confirmado:            { label: 'Confirmado',  className: 'bg-green-100 text-green-700 border border-green-200 dark:bg-green-900/30 dark:text-green-300 dark:border-green-800' },
  Concluido:             { label: 'Concluído',   className: 'bg-purple-100 text-purple-700 border border-purple-200 dark:bg-purple-900/30 dark:text-purple-300 dark:border-purple-800' },
  Cancelado:             { label: 'Cancelado',   className: 'bg-red-100 text-red-700 border border-red-200 dark:bg-red-900/30 dark:text-red-300 dark:border-red-800' },
  AguardandoConfirmacao: { label: 'Aguardando',  className: 'bg-yellow-100 text-yellow-700 border border-yellow-200 dark:bg-yellow-900/30 dark:text-yellow-300 dark:border-yellow-800' },
}

const fmtHora = (iso: string) =>
  new Date(iso).toLocaleTimeString('pt-BR', { hour: '2-digit', minute: '2-digit' })

const toDateStr = (d: Date) => d.toISOString().slice(0, 10)

const fmtDataLonga = (dateStr: string) => {
  const d = new Date(dateStr + 'T12:00:00Z')
  return d.toLocaleDateString('pt-BR', { weekday: 'long', day: 'numeric', month: 'long' })
}

function StatusBadge({ status }: { status: AgendamentoStatus }) {
  const cfg = STATUS_CONFIG[status] ?? { label: status, className: '' }
  return (
    <span className={cn('inline-flex items-center rounded-full px-2 py-0.5 text-[10px] font-semibold leading-none', cfg.className)}>
      {cfg.label}
    </span>
  )
}

function AppointmentCard({ a, onClick }: { a: AgendamentoListItem; onClick: () => void }) {
  return (
    <button
      onClick={onClick}
      className="w-full text-left rounded-xl border bg-card hover:bg-accent/50 transition-colors p-3 space-y-1.5"
    >
      <div className="flex items-start justify-between gap-2">
        <p className="font-semibold text-sm leading-tight truncate">{a.clienteNome}</p>
        <StatusBadge status={a.status} />
      </div>
      <div className="flex items-center gap-1 text-xs text-muted-foreground">
        <Clock size={11} className="shrink-0" />
        <span>{fmtHora(a.dataHoraInicio)} – {fmtHora(a.dataHoraFim)}</span>
      </div>
      <p className="text-xs text-muted-foreground truncate">{a.servicoNome}</p>
    </button>
  )
}

function AppointmentCardWithProf({ a, onClick }: { a: AgendamentoListItem; onClick: () => void }) {
  return (
    <button
      onClick={onClick}
      className="w-full text-left rounded-xl border bg-card hover:bg-accent/50 transition-colors p-3 space-y-1.5"
    >
      <div className="flex items-start justify-between gap-2">
        <p className="font-semibold text-sm leading-tight truncate">{a.clienteNome}</p>
        <StatusBadge status={a.status} />
      </div>
      <div className="flex items-center gap-1 text-xs text-muted-foreground">
        <Clock size={11} className="shrink-0" />
        <span>{fmtHora(a.dataHoraInicio)} – {fmtHora(a.dataHoraFim)}</span>
      </div>
      <p className="text-xs text-muted-foreground truncate">{a.servicoNome}</p>
      <div className="flex items-center gap-1 text-xs text-muted-foreground">
        <User size={11} className="shrink-0" />
        <span className="truncate">{a.profissionalNome}</span>
      </div>
    </button>
  )
}

export default function Agendamentos() {
  const navigate = useNavigate()
  const [dataStr, setDataStr] = useState(toDateStr(new Date()))
  const [pendentes, setPendentes] = useState<AgendamentoListItem[]>([])
  const [confirmandoId, setConfirmandoId] = useState<string | null>(null)
  const [recusandoId, setRecusandoId] = useState<string | null>(null)
  const { agendamentos, loading, error, list, confirmar, recusar, pendentesConfirmacao } = useAgendamentos()
  const { profissionais, list: listProfs } = useProfissionais()

  useEffect(() => { void listProfs() }, [listProfs])
  useEffect(() => { void list(dataStr) }, [list, dataStr])
  useEffect(() => {
    pendentesConfirmacao().then(setPendentes).catch(() => {})
  }, [pendentesConfirmacao])

  function mudarDia(delta: number) {
    const d = new Date(dataStr + 'T12:00:00Z')
    d.setUTCDate(d.getUTCDate() + delta)
    setDataStr(toDateStr(d))
  }

  const ativos = profissionais.filter(p => p.ativo)
  const isHoje = dataStr === toDateStr(new Date())

  if (error) return <p className="text-destructive p-4">{error}</p>

  return (
    <div className="space-y-4">
      {/* Header */}
      <div className="flex flex-wrap items-center justify-between gap-2">
        <div className="flex items-center gap-2">
          <CalendarDays size={22} className="text-primary shrink-0" />
          <h1 className="text-2xl font-bold">Agenda</h1>
        </div>
        <Button onClick={() => navigate('/agendamentos/novo')}>
          <Plus size={16} className="mr-1.5" /> Novo Agendamento
        </Button>
      </div>

      {/* Date navigation */}
      <div className="flex items-center gap-2">
        <button
          onClick={() => mudarDia(-1)}
          className="p-2 rounded-lg border bg-card hover:bg-accent transition-colors"
          aria-label="Dia anterior"
        >
          <ChevronLeft size={18} />
        </button>

        <div className="flex-1 flex flex-col items-center">
          <input
            type="date"
            value={dataStr}
            onChange={e => setDataStr(e.target.value)}
            className="rounded-lg border border-input bg-transparent px-3 py-1.5 text-sm font-medium text-center focus:outline-none focus:ring-2 focus:ring-primary/40"
          />
          <span className="text-[11px] text-muted-foreground mt-0.5 capitalize">
            {fmtDataLonga(dataStr)}
          </span>
        </div>

        <button
          onClick={() => mudarDia(1)}
          className="p-2 rounded-lg border bg-card hover:bg-accent transition-colors"
          aria-label="Próximo dia"
        >
          <ChevronRight size={18} />
        </button>

        {!isHoje && (
          <button
            onClick={() => setDataStr(toDateStr(new Date()))}
            className="px-3 py-1.5 rounded-lg border bg-card hover:bg-accent transition-colors text-sm font-medium text-muted-foreground"
          >
            Hoje
          </button>
        )}
      </div>

      {/* Pending confirmations */}
      {pendentes.length > 0 && (
        <div className="rounded-xl border border-yellow-300 bg-yellow-50 dark:bg-yellow-950/20 dark:border-yellow-800 p-4 space-y-3">
          <h2 className="font-semibold text-yellow-800 dark:text-yellow-400 text-sm">
            {pendentes.length} agendamento{pendentes.length > 1 ? 's' : ''} aguardando confirmação
          </h2>
          <div className="space-y-2">
            {pendentes.map(a => (
              <div
                key={a.id}
                className="flex flex-col sm:flex-row sm:items-center justify-between gap-3 bg-white dark:bg-background rounded-lg border px-3 py-3"
              >
                <div className="min-w-0">
                  <p className="font-semibold text-sm">{a.clienteNome}</p>
                  <p className="text-muted-foreground text-xs mt-0.5">
                    {a.servicoNome} · {a.profissionalNome}
                  </p>
                  <p className="text-muted-foreground text-xs">
                    {new Date(a.dataHoraInicio).toLocaleString('pt-BR', {
                      day: '2-digit', month: '2-digit', hour: '2-digit', minute: '2-digit',
                    })}
                  </p>
                </div>
                <div className="flex gap-2 shrink-0">
                  <Button
                    size="sm"
                    className="min-h-[40px] flex-1 sm:flex-none"
                    disabled={confirmandoId === a.id}
                    onClick={async () => {
                      setConfirmandoId(a.id)
                      try { await confirmar(a.id); setPendentes(p => p.filter(x => x.id !== a.id)) }
                      finally { setConfirmandoId(null) }
                    }}
                  >
                    {confirmandoId === a.id ? '...' : 'Confirmar'}
                  </Button>
                  <Button
                    size="sm"
                    variant="outline"
                    className="min-h-[40px] flex-1 sm:flex-none text-destructive hover:text-destructive hover:border-destructive"
                    disabled={recusandoId === a.id}
                    onClick={async () => {
                      setRecusandoId(a.id)
                      try { await recusar(a.id); setPendentes(p => p.filter(x => x.id !== a.id)) }
                      finally { setRecusandoId(null) }
                    }}
                  >
                    {recusandoId === a.id ? '...' : 'Recusar'}
                  </Button>
                </div>
              </div>
            ))}
          </div>
        </div>
      )}

      {loading ? (
        <div className="flex items-center justify-center py-16">
          <p className="text-muted-foreground text-sm">Carregando...</p>
        </div>
      ) : agendamentos.length === 0 && ativos.length === 0 ? (
        <div className="flex flex-col items-center justify-center py-16 gap-2 text-muted-foreground">
          <CalendarDays size={40} className="opacity-30" />
          <p className="text-sm">Nenhum profissional ativo cadastrado.</p>
        </div>
      ) : (
        <>
          {/* Mobile: list view */}
          <div className="md:hidden space-y-2">
            {agendamentos.length === 0 ? (
              <div className="flex flex-col items-center justify-center py-12 gap-2 text-muted-foreground">
                <CalendarDays size={36} className="opacity-30" />
                <p className="text-sm">Nenhum agendamento para este dia.</p>
              </div>
            ) : (
              [...agendamentos]
                .sort((a, b) => a.dataHoraInicio.localeCompare(b.dataHoraInicio))
                .map(a => (
                  <AppointmentCardWithProf
                    key={a.id}
                    a={a}
                    onClick={() => navigate(`/agendamentos/${a.id}`)}
                  />
                ))
            )}
          </div>

          {/* Tablet/Desktop: professional columns */}
          <div className="hidden md:block overflow-x-auto pb-2">
            {ativos.length === 0 ? (
              <div className="rounded-xl border p-8 text-center text-muted-foreground text-sm">
                Nenhum profissional ativo.
              </div>
            ) : (
              <div
                className="grid gap-3"
                style={{ gridTemplateColumns: `repeat(${ativos.length}, minmax(220px, 1fr))` }}
              >
                {ativos.map(prof => {
                  const cards = agendamentos
                    .filter(a => a.profissionalId === prof.id)
                    .sort((a, b) => a.dataHoraInicio.localeCompare(b.dataHoraInicio))
                  return (
                    <div key={prof.id} className="rounded-xl border bg-card/50 overflow-hidden">
                      <div className="bg-muted/60 border-b px-3 py-2.5 flex items-center gap-2">
                        <div className="h-6 w-6 rounded-full bg-primary/20 flex items-center justify-center shrink-0">
                          <span className="text-[10px] font-bold text-primary uppercase">
                            {prof.nome.charAt(0)}
                          </span>
                        </div>
                        <span className="font-semibold text-sm truncate">{prof.nome}</span>
                        {cards.length > 0 && (
                          <span className="ml-auto text-xs text-muted-foreground font-medium shrink-0">
                            {cards.length}
                          </span>
                        )}
                      </div>
                      <div className="p-2 space-y-2">
                        {cards.length === 0 ? (
                          <p className="px-2 py-6 text-center text-xs text-muted-foreground">
                            Sem agendamentos
                          </p>
                        ) : cards.map(a => (
                          <AppointmentCard
                            key={a.id}
                            a={a}
                            onClick={() => navigate(`/agendamentos/${a.id}`)}
                          />
                        ))}
                      </div>
                    </div>
                  )
                })}
              </div>
            )}
          </div>
        </>
      )}
    </div>
  )
}
