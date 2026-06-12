// frontend/src/pages/planos/PlanoWizard.tsx
import { useEffect, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { api } from '@/services/api'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { toast } from '@/hooks/useToast'
import type { NichoTemplate } from '@/types/assinaturas'

const NICHOS = ['Barbearia', 'Salão', 'Estética', 'Pet Shop', 'Personal Trainer', 'Personalizado']

interface ItemForm {
  descricao: string
  quantidadePorCiclo: number
  tipo: string
  percentualDesconto: string
}

interface PlanoForm {
  nome: string
  descricao: string
  nicho: string
  preco: string
  periodicidade: string
  maisVendido: boolean
  itens: ItemForm[]
}

const BLANK_FORM: PlanoForm = {
  nome: '', descricao: '', nicho: 'Personalizado', preco: '',
  periodicidade: 'Mensal', maisVendido: false, itens: [],
}

export default function PlanoWizard() {
  const navigate = useNavigate()
  const [step, setStep] = useState(1)
  const [templates, setTemplates] = useState<NichoTemplate[]>([])
  const [selectedNicho, setSelectedNicho] = useState('')
  const [form, setForm] = useState<PlanoForm>(BLANK_FORM)
  const [saving, setSaving] = useState(false)

  useEffect(() => {
    api.get<NichoTemplate[]>('/api/nicho-templates').then(setTemplates).catch(() => {})
  }, [])

  const templatesByNicho = templates.filter(t => t.nicho === selectedNicho)

  function applyTemplate(t: NichoTemplate) {
    setForm({
      nome: t.nomePlano,
      descricao: t.descricao ?? '',
      nicho: t.nicho,
      preco: String(t.precoSugerido),
      periodicidade: t.periodicidade,
      maisVendido: t.maisVendido,
      itens: t.itens.map(i => ({
        descricao: i.descricao,
        quantidadePorCiclo: i.quantidadePorCiclo,
        tipo: i.tipo,
        percentualDesconto: i.percentualDesconto ? String(i.percentualDesconto) : '',
      })),
    })
    setStep(2)
  }

  function addItem() {
    setForm(f => ({ ...f, itens: [...f.itens, { descricao: '', quantidadePorCiclo: 1, tipo: 'Servico', percentualDesconto: '' }] }))
  }

  function removeItem(idx: number) {
    setForm(f => ({ ...f, itens: f.itens.filter((_, i) => i !== idx) }))
  }

  async function handleSave() {
    setSaving(true)
    try {
      const result = await api.post<{ id: string }>('/api/planos-assinatura', {
        nome: form.nome,
        descricao: form.descricao || null,
        nicho: form.nicho,
        preco: parseFloat(form.preco),
        periodicidade: form.periodicidade,
        maisVendido: form.maisVendido,
        itens: form.itens.map(i => ({
          descricao: i.descricao,
          servicoId: null,
          quantidadePorCiclo: i.quantidadePorCiclo,
          tipo: i.tipo,
          percentualDesconto: i.percentualDesconto ? parseFloat(i.percentualDesconto) : null,
        })),
      })
      toast.success('Plano criado com sucesso!')
      navigate(`/planos/${result.id}`)
    } catch (e) {
      toast.error(e instanceof Error ? e.message : 'Erro ao salvar')
    } finally {
      setSaving(false)
    }
  }

  return (
    <div className="max-w-2xl space-y-4">
      <div className="flex items-center gap-2">
        {[1, 2, 3].map(n => (
          <div key={n} className={`h-2 flex-1 rounded-full ${step >= n ? 'bg-primary' : 'bg-muted'}`} />
        ))}
      </div>

      {step === 1 && (
        <div className="rounded-xl border bg-card p-6 space-y-4">
          <h1 className="text-xl font-bold">Passo 1 — Escolha o Nicho</h1>
          <div className="grid grid-cols-2 gap-3 sm:grid-cols-3">
            {NICHOS.map(n => (
              <button key={n} onClick={() => setSelectedNicho(n)}
                className={`rounded-lg border p-4 text-left transition-colors hover:border-primary ${selectedNicho === n ? 'border-primary bg-primary/5' : ''}`}>
                <p className="font-medium">{n}</p>
              </button>
            ))}
          </div>

          {selectedNicho && templatesByNicho.length > 0 && (
            <div className="space-y-2">
              <p className="text-sm font-medium">Modelos sugeridos para {selectedNicho}:</p>
              <div className="grid gap-3 sm:grid-cols-3">
                {templatesByNicho.map(t => (
                  <button key={t.id} onClick={() => applyTemplate(t)}
                    className="rounded-lg border p-3 text-left hover:border-primary transition-colors">
                    {t.maisVendido && <span className="text-xs text-amber-700 font-medium">⭐ Mais vendido</span>}
                    <p className="font-semibold">{t.nomePlano}</p>
                    <p className="text-primary font-bold">R$ {t.precoSugerido.toFixed(2).replace('.', ',')}/mês</p>
                    <ul className="text-xs text-muted-foreground mt-1 space-y-0.5">
                      {t.itens.slice(0, 3).map(i => <li key={i.id}>• {i.quantidadePorCiclo === 0 ? `${i.descricao} ilimitado` : `${i.quantidadePorCiclo}x ${i.descricao}`}</li>)}
                    </ul>
                  </button>
                ))}
              </div>
            </div>
          )}

          <div className="flex justify-between pt-2">
            <Button variant="outline" onClick={() => navigate('/planos')}>Cancelar</Button>
            <Button onClick={() => { setForm(f => ({ ...f, nicho: selectedNicho || 'Personalizado' })); setStep(2) }}
              disabled={!selectedNicho}>
              Próximo
            </Button>
          </div>
        </div>
      )}

      {step === 2 && (
        <div className="rounded-xl border bg-card p-6 space-y-4">
          <h1 className="text-xl font-bold">Passo 2 — Serviços e Preço</h1>
          <div className="grid gap-3">
            <div className="grid gap-1.5">
              <Label>Nome do Plano</Label>
              <Input value={form.nome} onChange={e => setForm(f => ({ ...f, nome: e.target.value }))} />
            </div>
            <div className="grid gap-1.5">
              <Label>Descrição (opcional)</Label>
              <Input value={form.descricao} onChange={e => setForm(f => ({ ...f, descricao: e.target.value }))} />
            </div>
            <div className="grid grid-cols-2 gap-3">
              <div className="grid gap-1.5">
                <Label>Preço (R$)</Label>
                <Input type="number" min="0" step="0.01" value={form.preco}
                  onChange={e => setForm(f => ({ ...f, preco: e.target.value }))} />
              </div>
              <div className="grid gap-1.5">
                <Label>Periodicidade</Label>
                <select value={form.periodicidade} onChange={e => setForm(f => ({ ...f, periodicidade: e.target.value }))}
                  className="flex h-9 w-full rounded-md border border-input bg-transparent px-3 py-1 text-sm">
                  {['Mensal', 'Trimestral', 'Semestral', 'Anual'].map(p => <option key={p}>{p}</option>)}
                </select>
              </div>
            </div>
          </div>

          <div className="space-y-2">
            <div className="flex items-center justify-between">
              <p className="font-medium text-sm">Itens do Plano</p>
              <Button size="sm" variant="outline" onClick={addItem}>+ Adicionar Item</Button>
            </div>
            {form.itens.map((item, idx) => (
              <div key={idx} className="rounded border p-3 grid gap-2 sm:grid-cols-3">
                <Input placeholder="Descrição" value={item.descricao}
                  onChange={e => setForm(f => ({ ...f, itens: f.itens.map((x, i) => i === idx ? { ...x, descricao: e.target.value } : x) }))} />
                <div className="flex gap-2">
                  <Input type="number" min="0" placeholder="Qtd (0=∞)" value={item.quantidadePorCiclo}
                    onChange={e => setForm(f => ({ ...f, itens: f.itens.map((x, i) => i === idx ? { ...x, quantidadePorCiclo: parseInt(e.target.value) || 0 } : x) }))} />
                  <select value={item.tipo}
                    onChange={e => setForm(f => ({ ...f, itens: f.itens.map((x, i) => i === idx ? { ...x, tipo: e.target.value } : x) }))}
                    className="flex h-9 rounded-md border border-input bg-transparent px-2 py-1 text-sm">
                    <option>Servico</option><option>Desconto</option><option>Beneficio</option>
                  </select>
                </div>
                <div className="flex gap-2 items-center">
                  {item.tipo === 'Desconto' && (
                    <Input type="number" placeholder="% desconto" value={item.percentualDesconto}
                      onChange={e => setForm(f => ({ ...f, itens: f.itens.map((x, i) => i === idx ? { ...x, percentualDesconto: e.target.value } : x) }))} />
                  )}
                  <Button size="sm" variant="ghost" className="text-red-500" onClick={() => removeItem(idx)}>✕</Button>
                </div>
              </div>
            ))}
          </div>

          <div className="flex justify-between pt-2">
            <Button variant="outline" onClick={() => setStep(1)}>Voltar</Button>
            <Button onClick={() => setStep(3)} disabled={!form.nome || !form.preco}>Próximo</Button>
          </div>
        </div>
      )}

      {step === 3 && (
        <div className="rounded-xl border bg-card p-6 space-y-4">
          <h1 className="text-xl font-bold">Passo 3 — Revisão</h1>
          <div className="rounded-lg border p-5 space-y-3 max-w-xs">
            {form.maisVendido && <span className="text-xs bg-amber-100 text-amber-800 px-2 py-0.5 rounded-full">⭐ Mais vendido</span>}
            <h2 className="text-lg font-bold">{form.nome || 'Sem nome'}</h2>
            {form.descricao && <p className="text-sm text-muted-foreground">{form.descricao}</p>}
            <p className="text-3xl font-bold text-primary">
              R$ {(parseFloat(form.preco) || 0).toFixed(2).replace('.', ',')}
              <span className="text-sm font-normal text-muted-foreground">/{form.periodicidade.toLowerCase()}</span>
            </p>
            <ul className="text-sm space-y-1">
              {form.itens.map((i, idx) => (
                <li key={idx} className="flex items-center gap-1">
                  <span className="text-green-500">✓</span>
                  {i.quantidadePorCiclo === 0 ? `${i.descricao} (ilimitado)` : `${i.quantidadePorCiclo}x ${i.descricao}`}
                  {i.tipo === 'Desconto' && i.percentualDesconto && ` (${i.percentualDesconto}% desc.)`}
                </li>
              ))}
            </ul>
          </div>

          <div className="flex items-center gap-2">
            <input type="checkbox" id="maisVendido" checked={form.maisVendido}
              onChange={e => setForm(f => ({ ...f, maisVendido: e.target.checked }))} />
            <Label htmlFor="maisVendido">Marcar como "Mais vendido"</Label>
          </div>

          <div className="flex justify-between pt-2">
            <Button variant="outline" onClick={() => setStep(2)}>Voltar</Button>
            <Button onClick={() => void handleSave()} disabled={saving}>
              {saving ? 'Salvando...' : 'Ativar Plano'}
            </Button>
          </div>
        </div>
      )}
    </div>
  )
}
