import { useState, useEffect } from 'react'
import { useNavigate } from 'react-router-dom'
import { ChevronLeft } from 'lucide-react'
import { useCobrancas } from '@/hooks/useCobrancas'
import { useClientes } from '@/hooks/useClientes'
import { Button } from '@/components/ui/button'

export default function NovaCobranca() {
  const navigate = useNavigate()
  const { create } = useCobrancas()
  const { clientes, list: listClientes } = useClientes()

  useEffect(() => { void listClientes() }, [listClientes])

  const [clienteId, setClienteId] = useState('')
  const [referencia, setReferencia] = useState('')
  const [valor, setValor] = useState('')
  const [dataVencimento, setDataVencimento] = useState('')
  const [observacao, setObservacao] = useState('')
  const [saving, setSaving] = useState(false)
  const [error, setError] = useState('')

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    setError('')
    setSaving(true)
    try {
      const result = await create({
        clienteId, referencia, valor: Number(valor),
        dataVencimento, observacao: observacao || undefined,
      })
      navigate(`/cobrancas/${result.id}`)
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Erro ao criar cobrança')
    } finally { setSaving(false) }
  }

  const inputClass = 'w-full rounded-lg border border-input bg-transparent px-3 py-2 text-sm'

  return (
    <div className="flex flex-col gap-6 max-w-md">
      <div className="flex items-center gap-2">
        <Button variant="ghost" size="icon" onClick={() => navigate('/cobrancas')}>
          <ChevronLeft className="h-5 w-5" />
        </Button>
        <h1 className="text-xl font-bold">Nova Cobrança</h1>
      </div>

      {error && (
        <div className="rounded-lg border border-destructive/30 bg-destructive/10 px-4 py-3 text-sm text-destructive">{error}</div>
      )}

      <form onSubmit={handleSubmit} className="flex flex-col gap-4">
        <div>
          <label className="block text-sm font-medium mb-1">Cliente *</label>
          <select value={clienteId} onChange={e => setClienteId(e.target.value)} required className={inputClass}>
            <option value="">Selecione...</option>
            {clientes.map(c => <option key={c.id} value={c.id}>{c.nome}</option>)}
          </select>
        </div>

        <div>
          <label className="block text-sm font-medium mb-1">Referência *</label>
          <input value={referencia} onChange={e => setReferencia(e.target.value)} required
            className={inputClass} placeholder="Ex: Mensalidade Jun/2026" />
        </div>

        <div className="grid grid-cols-2 gap-4">
          <div>
            <label className="block text-sm font-medium mb-1">Valor (R$) *</label>
            <input type="number" step="0.01" min="0" value={valor}
              onChange={e => setValor(e.target.value)} required className={inputClass} />
          </div>
          <div>
            <label className="block text-sm font-medium mb-1">Vencimento *</label>
            <input type="date" value={dataVencimento}
              onChange={e => setDataVencimento(e.target.value)} required className={inputClass} />
          </div>
        </div>

        <div>
          <label className="block text-sm font-medium mb-1">Observações</label>
          <textarea value={observacao} onChange={e => setObservacao(e.target.value)}
            rows={2} className={inputClass} />
        </div>

        <div className="flex gap-2 pt-2">
          <Button type="submit" disabled={saving}>{saving ? 'Salvando...' : 'Criar Cobrança'}</Button>
          <Button type="button" variant="outline" onClick={() => navigate('/cobrancas')}>Cancelar</Button>
        </div>
      </form>
    </div>
  )
}
