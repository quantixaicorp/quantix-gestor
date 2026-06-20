import { useEffect, useState, useRef } from 'react'
import { useNavigate } from 'react-router-dom'
import { useAgendamentos } from '@/hooks/useAgendamentos'
import { useProfissionais } from '@/hooks/useProfissionais'
import { useEstoque } from '@/hooks/useEstoque'
import { useClientes } from '@/hooks/useClientes'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { toast } from '@/hooks/useToast'
import { cn } from '@/lib/utils'

const MESES_NOME = ['Janeiro','Fevereiro','Março','Abril','Maio','Junho','Julho','Agosto','Setembro','Outubro','Novembro','Dezembro']
const DIAS_SEMANA_MINI = ['D','S','T','Q','Q','S','S']

function buildCells(ano: number, mes: number) {
  const first = new Date(ano, mes, 1).getDay()
  const days = new Date(ano, mes + 1, 0).getDate()
  const today = new Date(); today.setHours(0, 0, 0, 0)
  const cells: Array<{ day: number; date: string; disabled: boolean } | null> = Array(first).fill(null)
  for (let d = 1; d <= days; d++) {
    const dt = new Date(ano, mes, d)
    const date = `${ano}-${String(mes + 1).padStart(2, '0')}-${String(d).padStart(2, '0')}`
    cells.push({ day: d, date, disabled: dt < today })
  }
  return cells
}

function CalendarioPicker({ value, onChange }: { value: string; onChange: (d: string) => void }) {
  const [aberto, setAberto] = useState(false)
  const ref = useRef<HTMLDivElement>(null)
  const base = value ? new Date(value + 'T12:00:00') : new Date()
  const [ano, setAno] = useState(base.getFullYear())
  const [mes, setMes] = useState(base.getMonth())

  useEffect(() => {
    function fechar(e: MouseEvent) {
      if (ref.current && !ref.current.contains(e.target as Node)) setAberto(false)
    }
    if (aberto) document.addEventListener('mousedown', fechar)
    return () => document.removeEventListener('mousedown', fechar)
  }, [aberto])

  function navMes(dir: 1 | -1) {
    if (dir === -1 && mes === 0) { setMes(11); setAno(a => a - 1) }
    else if (dir === 1 && mes === 11) { setMes(0); setAno(a => a + 1) }
    else setMes(m => m + dir)
  }

  const cells = buildCells(ano, mes)
  const displayLabel = value
    ? new Date(value + 'T12:00:00').toLocaleDateString('pt-BR', { weekday: 'short', day: '2-digit', month: 'short', year: 'numeric' })
    : 'Selecionar data'

  return (
    <div className="relative" ref={ref}>
      <button
        type="button"
        onClick={() => setAberto(o => !o)}
        className="flex h-9 w-full items-center justify-between rounded-md border border-input bg-transparent px-3 py-1 text-sm hover:bg-accent/30 transition-colors"
      >
        <span>{displayLabel}</span>
        <span className="text-muted-foreground text-xs">📅</span>
      </button>
      {aberto && (
        <div className="absolute z-50 top-11 left-0 bg-card border rounded-xl shadow-xl p-3 w-64">
          <div className="flex items-center justify-between mb-3">
            <button type="button" onClick={() => navMes(-1)}
              className="h-7 w-7 rounded-md hover:bg-accent transition-colors flex items-center justify-center text-muted-foreground">‹</button>
            <span className="text-sm font-semibold">{MESES_NOME[mes]} {ano}</span>
            <button type="button" onClick={() => navMes(1)}
              className="h-7 w-7 rounded-md hover:bg-accent transition-colors flex items-center justify-center text-muted-foreground">›</button>
          </div>
          <div className="grid grid-cols-7 mb-1">
            {DIAS_SEMANA_MINI.map((d, i) => (
              <span key={i} className="text-center text-[10px] font-medium text-muted-foreground py-1">{d}</span>
            ))}
          </div>
          <div className="grid grid-cols-7 gap-y-0.5">
            {cells.map((cell, i) =>
              cell === null ? <div key={i} /> : (
                <button
                  key={i}
                  type="button"
                  disabled={cell.disabled}
                  onClick={() => { onChange(cell.date); setAberto(false) }}
                  className={cn(
                    'h-8 w-8 mx-auto rounded-full text-sm transition-colors',
                    cell.disabled && 'opacity-30 cursor-not-allowed',
                    value === cell.date ? 'bg-primary text-primary-foreground font-semibold' : !cell.disabled && 'hover:bg-accent'
                  )}
                >
                  {cell.day}
                </button>
              )
            )}
          </div>
        </div>
      )}
    </div>
  )
}

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
    <div className="max-w-lg space-y-4">
      <h1 className="text-2xl font-bold">Novo Agendamento</h1>

      <div className="rounded-xl border bg-card p-6 space-y-5">
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
        <CalendarioPicker value={data} onChange={setData} />
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
    </div>
  )
}
