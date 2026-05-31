import { useEffect, useState } from 'react'
import { useParams, useNavigate } from 'react-router-dom'
import { MessageCircle } from 'lucide-react'
import { useAgendamentos } from '@/hooks/useAgendamentos'
import { Button } from '@/components/ui/button'
import { Badge } from '@/components/ui/badge'
import { useConfirm } from '@/hooks/useConfirm'
import { toast } from '@/hooks/useToast'
import type { AgendamentoStatus } from '@/types/agendamento'

const statusClassName = (s: AgendamentoStatus): string => {
  if (s === 'Agendado')   return 'bg-blue-100 text-blue-700 border-blue-200'
  if (s === 'Confirmado') return 'bg-green-100 text-green-700 border-green-200'
  if (s === 'Concluido')  return 'bg-purple-100 text-purple-700 border-purple-200'
  if (s === 'Cancelado')  return 'bg-red-100 text-red-700 border-red-200'
  return ''
}

const fmtDt = (iso: string) =>
  new Date(iso).toLocaleString('pt-BR', {
    day: '2-digit', month: '2-digit', year: 'numeric',
    hour: '2-digit', minute: '2-digit',
  })

export default function DetalheAgendamento() {
  const { id } = useParams<{ id: string }>()
  const navigate = useNavigate()
  const { agendamento, loading, error, get, confirmar, concluir, cancelar } = useAgendamentos()
  const [acao, setAcao] = useState<string | null>(null)
  const { confirm, ConfirmDialogNode } = useConfirm()

  useEffect(() => {
    if (id) void get(id)
  }, [get, id])

  async function executar(fn: () => Promise<unknown>, label: string) {
    const ok = await confirm({ title: `Confirmar: ${label}?` })
    if (!ok) return
    setAcao(label)
    try { await fn() } catch (e) { toast.error(e instanceof Error ? e.message : 'Erro') }
    finally { setAcao(null) }
  }

  async function handleConcluir() {
    if (!id) return
    const ok = await confirm({
      title: 'Concluir agendamento?',
      description: 'Uma Venda será criada automaticamente.',
    })
    if (!ok) return
    setAcao('concluir')
    try {
      const result = await concluir(id)
      navigate(`/vendas/nova?vendaId=${result.vendaId}&origem=agendamento`)
    } catch (e) {
      toast.error(e instanceof Error ? e.message : 'Erro ao concluir')
    } finally {
      setAcao(null)
    }
  }

  function abrirWhatsapp() {
    if (!agendamento?.clienteTelefone) return
    const digits = agendamento.clienteTelefone.replace(/\D/g, '')
    const phone = digits.startsWith('55') ? digits : `55${digits}`
    const dt = fmtDt(agendamento.dataHoraInicio)
    const msg = encodeURIComponent(
      `Olá ${agendamento.clienteNome}! ` +
      `Seu agendamento de "${agendamento.servicoNome}" está confirmado para ${dt}.`
    )
    window.open(`https://wa.me/${phone}?text=${msg}`, '_blank')
  }

  if (loading) return <p className="text-muted-foreground">Carregando...</p>
  if (error) return <p className="text-destructive">{error}</p>
  if (!agendamento) return null

  const a = agendamento

  return (
    <div className="max-w-lg space-y-6">
      <div className="flex items-start justify-between">
        <div>
          <h1 className="text-2xl font-bold">{a.clienteNome}</h1>
          <p className="text-muted-foreground">{a.servicoNome} • {a.duracaoMinutos} min</p>
        </div>
        <Badge className={statusClassName(a.status)}>{a.status}</Badge>
      </div>

      <div className="flex flex-wrap gap-2">
        {(a.status === 'Agendado' || a.status === 'Confirmado') && (
          <Button variant="outline" onClick={abrirWhatsapp}>
            <MessageCircle size={16} className="mr-2" /> WhatsApp
          </Button>
        )}
        {a.status === 'Agendado' && (
          <>
            <Button onClick={() => executar(() => confirmar(a.id), 'Confirmar agendamento')}
              disabled={acao !== null}>
              {acao === 'Confirmar agendamento' ? '...' : 'Confirmar'}
            </Button>
            <Button variant="destructive" onClick={() => executar(() => cancelar(a.id), 'Cancelar agendamento')}
              disabled={acao !== null}>
              Cancelar
            </Button>
          </>
        )}
        {a.status === 'Confirmado' && (
          <>
            <Button onClick={handleConcluir} disabled={acao !== null}>
              {acao === 'concluir' ? '...' : 'Concluir'}
            </Button>
            <Button variant="destructive" onClick={() => executar(() => cancelar(a.id), 'Cancelar agendamento')}
              disabled={acao !== null}>
              Cancelar
            </Button>
          </>
        )}
        {a.status === 'Concluido' && a.vendaId && (
          <Button variant="outline" onClick={() => navigate('/vendas')}>
            Ver histórico de vendas
          </Button>
        )}
      </div>

      <div className="rounded-md border divide-y text-sm">
        <div className="px-4 py-3 flex justify-between">
          <span className="text-muted-foreground">Profissional</span>
          <span className="font-medium">{a.profissionalNome}</span>
        </div>
        <div className="px-4 py-3 flex justify-between">
          <span className="text-muted-foreground">Início</span>
          <span className="font-medium">{fmtDt(a.dataHoraInicio)}</span>
        </div>
        <div className="px-4 py-3 flex justify-between">
          <span className="text-muted-foreground">Fim</span>
          <span className="font-medium">{fmtDt(a.dataHoraFim)}</span>
        </div>
        <div className="px-4 py-3 flex justify-between">
          <span className="text-muted-foreground">Telefone</span>
          <span className="font-medium">{a.clienteTelefone}</span>
        </div>
        {a.observacao && (
          <div className="px-4 py-3">
            <span className="text-muted-foreground">Obs: </span>{a.observacao}
          </div>
        )}
      </div>

      <Button variant="ghost" onClick={() => navigate('/agendamentos')}>← Voltar</Button>
      {ConfirmDialogNode}
    </div>
  )
}
