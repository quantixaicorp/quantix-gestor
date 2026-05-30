import { useState, useEffect } from 'react'
import { useNavigate } from 'react-router-dom'
import { ChevronLeft, Plus, Trash2 } from 'lucide-react'
import { useContratos } from '@/hooks/useContratos'
import { useClientes } from '@/hooks/useClientes'
import { Button } from '@/components/ui/button'
import type { ContratoItemRequest } from '@/types/contrato'

export default function NovoContrato() {
  const navigate = useNavigate()
  const { create } = useContratos()
  const { clientes, list: listClientes } = useClientes()

  useEffect(() => { void listClientes() }, [listClientes])

  const [clienteId, setClienteId] = useState('')
  const [titulo, setTitulo] = useState('')
  const [objeto, setObjeto] = useState('')
  const [tipoCobranca, setTipoCobranca] = useState('Recorrente')
  const [valor, setValor] = useState('')
  const [dataInicio, setDataInicio] = useState('')
  const [dataFim, setDataFim] = useState('')
  const [periodicidade, setPeriodicidade] = useState('Mensal')
  const [diaVencimento, setDiaVencimento] = useState('10')
  const [observacao, setObservacao] = useState('')
  const [itens, setItens] = useState<ContratoItemRequest[]>([{ descricao: '', quantidade: 1, valorUnitario: 0 }])
  const [saving, setSaving] = useState(false)
  const [error, setError] = useState('')

  const addItem = () => setItens(prev => [...prev, { descricao: '', quantidade: 1, valorUnitario: 0 }])
  const removeItem = (i: number) => setItens(prev => prev.filter((_, idx) => idx !== i))
  const updateItem = (i: number, field: keyof ContratoItemRequest, val: string | number) =>
    setItens(prev => prev.map((item, idx) => idx === i ? { ...item, [field]: val } : item))

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    setError('')
    setSaving(true)
    try {
      const result = await create({
        clienteId, titulo, objeto, tipoCobranca,
        valor: Number(valor),
        dataInicio,
        dataFim: dataFim || undefined,
        periodicidade,
        diaVencimento: Number(diaVencimento),
        observacao: observacao || undefined,
        itens,
      })
      navigate(`/contratos/${result.id}`)
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Erro ao criar contrato')
    } finally { setSaving(false) }
  }

  const labelClass = 'block text-sm font-medium mb-1'
  const inputClass = 'w-full rounded-lg border border-input bg-transparent px-3 py-2 text-sm'

  return (
    <div className="flex flex-col gap-6 max-w-2xl">
      <div className="flex items-center gap-2">
        <Button variant="ghost" size="icon" onClick={() => navigate('/contratos')}>
          <ChevronLeft className="h-5 w-5" />
        </Button>
        <h1 className="text-xl font-bold">Novo Contrato</h1>
      </div>

      {error && (
        <div className="rounded-lg border border-destructive/30 bg-destructive/10 px-4 py-3 text-sm text-destructive">{error}</div>
      )}

      <form onSubmit={handleSubmit} className="flex flex-col gap-4">
        <div>
          <label className={labelClass}>Cliente *</label>
          <select value={clienteId} onChange={e => setClienteId(e.target.value)} required className={inputClass}>
            <option value="">Selecione...</option>
            {clientes.map(c => <option key={c.id} value={c.id}>{c.nome}</option>)}
          </select>
        </div>

        <div>
          <label className={labelClass}>Título *</label>
          <input value={titulo} onChange={e => setTitulo(e.target.value)} required className={inputClass} placeholder="Ex: Plano Mensal de Manutenção" />
        </div>

        <div>
          <label className={labelClass}>Objeto do Contrato *</label>
          <textarea value={objeto} onChange={e => setObjeto(e.target.value)} required rows={4} className={inputClass} placeholder="Descrição detalhada do que o contrato cobre..." />
        </div>

        <div className="grid grid-cols-2 gap-4">
          <div>
            <label className={labelClass}>Tipo de Cobrança</label>
            <select value={tipoCobranca} onChange={e => setTipoCobranca(e.target.value)} className={inputClass}>
              <option value="Recorrente">Recorrente</option>
              <option value="ParceladoPrazoFixo">Parcelado (Prazo Fixo)</option>
            </select>
          </div>
          <div>
            <label className={labelClass}>Periodicidade</label>
            <select value={periodicidade} onChange={e => setPeriodicidade(e.target.value)} className={inputClass}>
              {['Mensal', 'Trimestral', 'Semestral', 'Anual'].map(p => <option key={p} value={p}>{p}</option>)}
            </select>
          </div>
        </div>

        <div className="grid grid-cols-3 gap-4">
          <div>
            <label className={labelClass}>Valor (R$) *</label>
            <input type="number" step="0.01" min="0" value={valor} onChange={e => setValor(e.target.value)} required className={inputClass} />
          </div>
          <div>
            <label className={labelClass}>Data Início *</label>
            <input type="date" value={dataInicio} onChange={e => setDataInicio(e.target.value)} required className={inputClass} />
          </div>
          <div>
            <label className={labelClass}>Data Fim {tipoCobranca === 'ParceladoPrazoFixo' ? '*' : ''}</label>
            <input type="date" value={dataFim} onChange={e => setDataFim(e.target.value)}
              required={tipoCobranca === 'ParceladoPrazoFixo'} className={inputClass} />
          </div>
        </div>

        <div className="w-32">
          <label className={labelClass}>Dia Vencimento (1-28) *</label>
          <input type="number" min={1} max={28} value={diaVencimento} onChange={e => setDiaVencimento(e.target.value)} required className={inputClass} />
        </div>

        <div>
          <div className="flex items-center justify-between mb-2">
            <label className={labelClass + ' mb-0'}>Itens</label>
            <Button type="button" variant="outline" size="sm" onClick={addItem}>
              <Plus className="h-3 w-3 mr-1" /> Adicionar
            </Button>
          </div>
          <div className="flex flex-col gap-2">
            {itens.map((item, i) => (
              <div key={i} className="flex gap-2 items-end">
                <div className="flex-1">
                  {i === 0 && <label className="text-xs text-muted-foreground mb-1 block">Descrição</label>}
                  <input value={item.descricao} onChange={e => updateItem(i, 'descricao', e.target.value)}
                    className={inputClass} placeholder="Descrição do item" />
                </div>
                <div className="w-20">
                  {i === 0 && <label className="text-xs text-muted-foreground mb-1 block">Qtd</label>}
                  <input type="number" min="0" step="0.01" value={item.quantidade}
                    onChange={e => updateItem(i, 'quantidade', Number(e.target.value))} className={inputClass} />
                </div>
                <div className="w-28">
                  {i === 0 && <label className="text-xs text-muted-foreground mb-1 block">Valor Unit.</label>}
                  <input type="number" min="0" step="0.01" value={item.valorUnitario}
                    onChange={e => updateItem(i, 'valorUnitario', Number(e.target.value))} className={inputClass} />
                </div>
                <Button type="button" variant="ghost" size="icon" className="mb-0.5 text-destructive" onClick={() => removeItem(i)}>
                  <Trash2 className="h-4 w-4" />
                </Button>
              </div>
            ))}
          </div>
        </div>

        <div>
          <label className={labelClass}>Observações</label>
          <textarea value={observacao} onChange={e => setObservacao(e.target.value)} rows={2} className={inputClass} />
        </div>

        <div className="flex gap-2 pt-2">
          <Button type="submit" disabled={saving}>{saving ? 'Salvando...' : 'Criar Contrato'}</Button>
          <Button type="button" variant="outline" onClick={() => navigate('/contratos')}>Cancelar</Button>
        </div>
      </form>
    </div>
  )
}
