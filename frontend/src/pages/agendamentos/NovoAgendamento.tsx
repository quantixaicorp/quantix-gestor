import { useEffect, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { useAgendamentos } from '@/hooks/useAgendamentos'
import { useProfissionais } from '@/hooks/useProfissionais'
import { useEstoque } from '@/hooks/useEstoque'
import { useClientes } from '@/hooks/useClientes'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { toast } from '@/hooks/useToast'

const fmtHora = (iso: string) =>
  new Date(iso).toLocaleTimeString('pt-BR', { hour: '2-digit', minute: '2-digit' })

const toDateStr = (d: Date) => d.toISOString().slice(0, 10)

export default function NovoAgendamento() {
  const navigate = useNavigate()
  const { create, slots } = useAgendamentos()
  const { profissionais, list: listProfs } = useProfissionais()
  const { produtos, listProdutos } = useEstoque()
  const { clientes, list: listClientes } = useClientes()

  const [profissionalId, setProfissionalId] = useState('')
  const [servicoId, setServicoId] = useState('')
  const [data, setData] = useState(toDateStr(new Date()))
  const [slot, setSlot] = useState('')
  const [telefone, setTelefone] = useState('')
  const [clienteNome, setClienteNome] = useState('')
  const [clienteId, setClienteId] = useState<string | undefined>()
  const [observacao, setObservacao] = useState('')
  const [slotOpcoes, setSlotOpcoes] = useState<string[]>([])
  const [loadingSlots, setLoadingSlots] = useState(false)
  const [saving, setSaving] = useState(false)
  const [erros, setErros] = useState<Record<string, string>>({})

  useEffect(() => { void listProfs() }, [listProfs])
  useEffect(() => { void listProdutos() }, [listProdutos])
  useEffect(() => { void listClientes() }, [listClientes])

  useEffect(() => {
    if (!telefone || telefone.replace(/\D/g, '').length < 10) return
    const numeros = telefone.replace(/\D/g, '')
    const encontrado = clientes.find(c => c.whatsapp?.replace(/\D/g, '') === numeros)
    if (encontrado) {
      setClienteNome(encontrado.nome)
      setClienteId(encontrado.id)
    } else {
      setClienteId(undefined)
    }
  }, [telefone, clientes])

  useEffect(() => {
    if (!profissionalId || !servicoId || !data) {
      setSlotOpcoes([])
      setSlot('')
      return
    }
    setLoadingSlots(true)
    slots(profissionalId, data, servicoId)
      .then(s => { setSlotOpcoes(s); setSlot('') })
      .catch(() => setSlotOpcoes([]))
      .finally(() => setLoadingSlots(false))
  }, [profissionalId, servicoId, data, slots])

  const servicos = produtos.filter(p => (p.duracaoMinutos ?? 0) > 0 && p.ativo)
  const profissionaisAtivos = profissionais.filter(p => p.ativo)

  async function salvar() {
    const novosErros: Record<string, string> = {}
    if (!profissionalId) novosErros.profissional = 'Profissional obrigatório'
    if (!servicoId) novosErros.servico = 'Serviço obrigatório'
    if (!slot) novosErros.slot = 'Horário obrigatório'
    if (!telefone.trim()) novosErros.telefone = 'Telefone obrigatório'
    if (!clienteNome.trim()) novosErros.clienteNome = 'Nome obrigatório'

    if (Object.keys(novosErros).length > 0) {
      setErros(novosErros)
      return
    }
    setErros({})

    setSaving(true)
    try {
      await create({
        profissionalId,
        servicoId,
        dataHoraInicio: slot,
        clienteNome: clienteNome.trim(),
        clienteTelefone: telefone.trim(),
        clienteId,
        observacao: observacao.trim() || undefined,
      })
      navigate('/agendamentos')
    } catch (e) {
      toast.error(e instanceof Error ? e.message : 'Erro ao criar agendamento')
    } finally {
      setSaving(false)
    }
  }

  return (
    <div className="max-w-lg space-y-5">
      <h1 className="text-2xl font-bold">Novo Agendamento</h1>

      <div className="space-y-2">
        <Label>Profissional *</Label>
        <select
          value={profissionalId}
          onChange={e => setProfissionalId(e.target.value)}
          className="flex h-9 w-full rounded-md border border-input bg-transparent px-3 py-1 text-sm"
        >
          <option value="">Selecionar profissional...</option>
          {profissionaisAtivos.map(p => (
            <option key={p.id} value={p.id}>{p.nome}</option>
          ))}
        </select>
        {erros.profissional && <p className="text-xs text-destructive">{erros.profissional}</p>}
      </div>

      <div className="space-y-2">
        <Label>Serviço *</Label>
        <select
          value={servicoId}
          onChange={e => setServicoId(e.target.value)}
          className="flex h-9 w-full rounded-md border border-input bg-transparent px-3 py-1 text-sm"
        >
          <option value="">Selecionar serviço...</option>
          {servicos.map(s => (
            <option key={s.id} value={s.id}>{s.nome} ({s.duracaoMinutos} min)</option>
          ))}
        </select>
        {erros.servico && <p className="text-xs text-destructive">{erros.servico}</p>}
      </div>

      <div className="space-y-2">
        <Label>Data *</Label>
        <Input type="date" value={data} onChange={e => setData(e.target.value)} />
      </div>

      <div className="space-y-2">
        <Label>Horário *</Label>
        {loadingSlots ? (
          <p className="text-sm text-muted-foreground">Buscando horários...</p>
        ) : (
          <select
            value={slot}
            onChange={e => setSlot(e.target.value)}
            disabled={slotOpcoes.length === 0}
            className="flex h-9 w-full rounded-md border border-input bg-transparent px-3 py-1 text-sm disabled:opacity-50"
          >
            <option value="">
              {slotOpcoes.length === 0
                ? 'Selecione profissional, serviço e data primeiro'
                : 'Selecionar horário...'}
            </option>
            {slotOpcoes.map(s => (
              <option key={s} value={s}>{fmtHora(s)}</option>
            ))}
          </select>
        )}
        {erros.slot && <p className="text-xs text-destructive">{erros.slot}</p>}
      </div>

      <div className="space-y-2">
        <Label>Telefone / WhatsApp *</Label>
        <Input
          value={telefone}
          onChange={e => setTelefone(e.target.value)}
          placeholder="(11) 99999-9999"
        />
        {erros.telefone && <p className="text-xs text-destructive">{erros.telefone}</p>}
      </div>

      <div className="space-y-2">
        <Label>Nome do Cliente *</Label>
        <Input
          value={clienteNome}
          onChange={e => setClienteNome(e.target.value)}
          placeholder="Nome completo"
        />
        {clienteId && (
          <p className="text-xs text-muted-foreground">Cliente existente encontrado.</p>
        )}
        {erros.clienteNome && <p className="text-xs text-destructive">{erros.clienteNome}</p>}
      </div>

      <div className="space-y-2">
        <Label>Observação</Label>
        <textarea
          value={observacao}
          onChange={e => setObservacao(e.target.value)}
          placeholder="Informações adicionais"
          rows={3}
          className="flex w-full rounded-md border border-input bg-transparent px-3 py-2 text-sm placeholder:text-muted-foreground focus-visible:outline-none focus-visible:ring-1 focus-visible:ring-ring resize-none"
        />
      </div>

      <div className="flex gap-2">
        <Button onClick={salvar} disabled={saving}>
          {saving ? '...' : 'Agendar'}
        </Button>
        <Button variant="ghost" onClick={() => navigate('/agendamentos')}>Cancelar</Button>
      </div>
    </div>
  )
}
