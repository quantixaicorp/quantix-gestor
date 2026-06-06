import { useEffect, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { ChevronLeft, ChevronRight } from 'lucide-react'
import { useAgendamentos } from '@/hooks/useAgendamentos'
import { useProfissionais } from '@/hooks/useProfissionais'
import { Button } from '@/components/ui/button'
import { Badge } from '@/components/ui/badge'
import type { AgendamentoListItem, AgendamentoStatus } from '@/types/agendamento'

const statusClassName = (s: AgendamentoStatus): string => {
  if (s === 'Agendado')   return 'bg-blue-100 text-blue-700 border-blue-200'
  if (s === 'Confirmado') return 'bg-green-100 text-green-700 border-green-200'
  if (s === 'Concluido')  return 'bg-purple-100 text-purple-700 border-purple-200'
  if (s === 'Cancelado')  return 'bg-red-100 text-red-700 border-red-200'
  if (s === 'AguardandoConfirmacao') return 'bg-yellow-100 text-yellow-700 border-yellow-200'
  return ''
}

const fmtHora = (iso: string) =>
  new Date(iso).toLocaleTimeString('pt-BR', { hour: '2-digit', minute: '2-digit' })

const toDateStr = (d: Date) => d.toISOString().slice(0, 10)

export default function Agendamentos() {
  const navigate = useNavigate()
  const [data, setData] = useState(toDateStr(new Date()))
  const [pendentes, setPendentes] = useState<AgendamentoListItem[]>([])
  const [confirmandoId, setConfirmandoId] = useState<string | null>(null)
  const [recusandoId, setRecusandoId] = useState<string | null>(null)
  const { agendamentos, loading, error, list, confirmar, recusar, pendentesConfirmacao } = useAgendamentos()
  const { profissionais, list: listProfs } = useProfissionais()

  useEffect(() => { void listProfs() }, [listProfs])
  useEffect(() => { void list(data) }, [list, data])
  useEffect(() => {
    pendentesConfirmacao().then(setPendentes).catch(() => {})
  }, [pendentesConfirmacao])

  function mudarDia(delta: number) {
    const d = new Date(data + 'T12:00:00Z')
    d.setUTCDate(d.getUTCDate() + delta)
    setData(toDateStr(d))
  }

  const ativos = profissionais.filter(p => p.ativo)

  if (error) return <p className="text-destructive">{error}</p>

  return (
    <div className="space-y-4">
      <div className="flex flex-wrap items-center justify-between gap-2">
        <h1 className="text-2xl font-bold">Agenda</h1>
        <Button onClick={() => navigate('/agendamentos/novo')}>Novo Agendamento</Button>
      </div>

      <div className="flex items-center gap-3">
        <Button variant="outline" size="sm" onClick={() => mudarDia(-1)}>
          <ChevronLeft size={16} />
        </Button>
        <input
          type="date"
          value={data}
          onChange={e => setData(e.target.value)}
          className="rounded-md border border-input bg-transparent px-3 py-1 text-sm"
        />
        <Button variant="outline" size="sm" onClick={() => mudarDia(1)}>
          <ChevronRight size={16} />
        </Button>
        <Button variant="ghost" size="sm" onClick={() => setData(toDateStr(new Date()))}>
          Hoje
        </Button>
      </div>

      {pendentes.length > 0 && (
        <div className="rounded-md border border-yellow-300 bg-yellow-50 p-4 space-y-2">
          <h2 className="font-semibold text-yellow-800 text-sm">
            {pendentes.length} agendamento(s) aguardando confirmação
          </h2>
          {pendentes.map(a => (
            <div key={a.id} className="flex items-center justify-between bg-white rounded-md border px-3 py-2 text-sm">
              <div>
                <p className="font-medium">{a.clienteNome}</p>
                <p className="text-muted-foreground text-xs">
                  {a.servicoNome} · {a.profissionalNome} · {new Date(a.dataHoraInicio).toLocaleString('pt-BR', { day: '2-digit', month: '2-digit', hour: '2-digit', minute: '2-digit' })}
                </p>
              </div>
              <div className="flex gap-2">
                <Button size="sm" disabled={confirmandoId === a.id}
                  onClick={async () => {
                    setConfirmandoId(a.id)
                    try { await confirmar(a.id); setPendentes(p => p.filter(x => x.id !== a.id)) }
                    finally { setConfirmandoId(null) }
                  }}>
                  {confirmandoId === a.id ? '...' : 'Confirmar'}
                </Button>
                <Button size="sm" variant="outline" className="text-destructive hover:text-destructive"
                  disabled={recusandoId === a.id}
                  onClick={async () => {
                    setRecusandoId(a.id)
                    try { await recusar(a.id); setPendentes(p => p.filter(x => x.id !== a.id)) }
                    finally { setRecusandoId(null) }
                  }}>
                  {recusandoId === a.id ? '...' : 'Recusar'}
                </Button>
              </div>
            </div>
          ))}
        </div>
      )}

      {loading ? (
        <p className="text-muted-foreground">Carregando...</p>
      ) : (
        <div className="grid gap-4" style={{ gridTemplateColumns: `repeat(${Math.max(ativos.length, 1)}, minmax(200px, 1fr))` }}>
          {ativos.length === 0 ? (
            <div className="rounded-md border p-8 text-center text-muted-foreground">
              Nenhum profissional ativo.
            </div>
          ) : ativos.map(prof => {
            const cards = agendamentos.filter(a => a.profissionalId === prof.id)
            return (
              <div key={prof.id} className="overflow-x-auto rounded-md border">
                <div className="border-b bg-muted/50 px-3 py-2 font-semibold text-sm">
                  {prof.nome}
                </div>
                <div className="divide-y">
                  {cards.length === 0 ? (
                    <p className="px-3 py-6 text-center text-sm text-muted-foreground">
                      Sem agendamentos
                    </p>
                  ) : cards.map(a => (
                    <button
                      key={a.id}
                      className="w-full px-3 py-3 text-left hover:bg-accent transition-colors"
                      onClick={() => navigate(`/agendamentos/${a.id}`)}
                    >
                      <div className="flex items-start justify-between gap-2">
                        <div>
                          <p className="text-sm font-medium">{a.clienteNome}</p>
                          <p className="text-xs text-muted-foreground">
                            {fmtHora(a.dataHoraInicio)} – {fmtHora(a.dataHoraFim)}
                          </p>
                          <p className="text-xs text-muted-foreground">{a.servicoNome}</p>
                        </div>
                        <Badge className={statusClassName(a.status)}>{a.status}</Badge>
                      </div>
                    </button>
                  ))}
                </div>
              </div>
            )
          })}
        </div>
      )}
    </div>
  )
}
