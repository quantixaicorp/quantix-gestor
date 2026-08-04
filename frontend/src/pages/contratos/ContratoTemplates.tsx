import { useState } from 'react'
import { useContratoTemplates } from '@/hooks/useContratoTemplates'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Trash2, Plus } from 'lucide-react'
import { toast } from '@/hooks/useToast'
import type { CreateContratoTemplateRequest } from '@/types/contrato-template'

export default function ContratoTemplatesPage() {
  const { templates, loading, create, remove } = useContratoTemplates()
  const [showForm, setShowForm] = useState(false)
  const [form, setForm] = useState<CreateContratoTemplateRequest>({
    name: '', subject: '', chargeType: 'Recorrente',
    frequency: 'Mensal', dueDay: 10, defaultAmount: null,
    items: [{ description: '', quantity: 1, unitPrice: 0 }],
  })
  const [saving, setSaving] = useState(false)

  async function handleSave() {
    if (!form.name || !form.subject) { toast.error('Nome e Objeto são obrigatórios'); return }
    setSaving(true)
    try {
      await create(form)
      toast.success('Template criado!')
      setShowForm(false)
      setForm({ name: '', subject: '', chargeType: 'Recorrente',
        frequency: 'Mensal', dueDay: 10, defaultAmount: null,
        items: [{ description: '', quantity: 1, unitPrice: 0 }] })
    } catch (e) {
      toast.error(e instanceof Error ? e.message : 'Erro ao salvar')
    } finally {
      setSaving(false)
    }
  }

  function addItem() {
    setForm(f => ({ ...f, items: [...f.items, { description: '', quantity: 1, unitPrice: 0 }] }))
  }

  function updateItem(idx: number, field: string, value: string | number) {
    setForm(f => ({ ...f, items: f.items.map((it, i) => i === idx ? { ...it, [field]: value } : it) }))
  }

  function removeItem(idx: number) {
    setForm(f => ({ ...f, items: f.items.filter((_, i) => i !== idx) }))
  }

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <h1 className="text-2xl font-bold">Templates de Contrato</h1>
        <Button onClick={() => setShowForm(v => !v)}>
          <Plus className="w-4 h-4 mr-2" />Novo Template
        </Button>
      </div>

      {showForm && (
        <div className="rounded-md border p-4 space-y-4">
          <h2 className="font-semibold">Novo Template</h2>
          <div className="grid gap-2">
            <Label>Nome</Label>
            <Input value={form.name} onChange={e => setForm(f => ({ ...f, name: e.target.value }))} placeholder="Ex: Mensalidade Padrão" />
          </div>
          <div className="grid gap-2">
            <Label>Objeto (texto do contrato)</Label>
            <textarea
              className="flex min-h-[80px] w-full rounded-md border border-input bg-background px-3 py-2 text-sm"
              value={form.subject}
              onChange={e => setForm(f => ({ ...f, subject: e.target.value }))}
              placeholder="Descreva o objeto do contrato..."
            />
          </div>
          <div className="grid grid-cols-2 gap-4">
            <div className="grid gap-2">
              <Label>Tipo de Cobrança</Label>
              <select
                className="flex h-9 w-full rounded-md border border-input bg-background px-3 py-1 text-sm"
                value={form.chargeType}
                onChange={e => setForm(f => ({ ...f, chargeType: e.target.value }))}
              >
                <option value="Recorrente">Recorrente</option>
                <option value="ParceladoPrazoFixo">Parcelado</option>
              </select>
            </div>
            <div className="grid gap-2">
              <Label>Periodicidade</Label>
              <select
                className="flex h-9 w-full rounded-md border border-input bg-background px-3 py-1 text-sm"
                value={form.frequency}
                onChange={e => setForm(f => ({ ...f, frequency: e.target.value }))}
              >
                <option value="Mensal">Mensal</option>
                <option value="Trimestral">Trimestral</option>
                <option value="Semestral">Semestral</option>
                <option value="Anual">Anual</option>
              </select>
            </div>
          </div>
          <div className="space-y-2">
            <Label>Itens</Label>
            {form.items.map((item, idx) => (
              <div key={idx} className="flex gap-2 items-center">
                <Input
                  value={item.description}
                  onChange={e => updateItem(idx, 'description', e.target.value)}
                  placeholder="Descrição"
                  className="flex-1"
                />
                <Input
                  type="number"
                  value={item.quantity}
                  onChange={e => updateItem(idx, 'quantity', Number(e.target.value))}
                  className="w-20"
                />
                <Input
                  type="number"
                  value={item.unitPrice}
                  onChange={e => updateItem(idx, 'unitPrice', Number(e.target.value))}
                  placeholder="R$"
                  className="w-28"
                />
                <Button size="icon" variant="ghost" onClick={() => removeItem(idx)}>
                  <Trash2 className="w-4 h-4 text-destructive" />
                </Button>
              </div>
            ))}
            <Button variant="outline" size="sm" onClick={addItem}>
              <Plus className="w-4 h-4 mr-1" />Adicionar item
            </Button>
          </div>
          <div className="flex gap-2">
            <Button onClick={() => void handleSave()} disabled={saving}>{saving ? 'Salvando...' : 'Salvar Template'}</Button>
            <Button variant="outline" onClick={() => setShowForm(false)}>Cancelar</Button>
          </div>
        </div>
      )}

      {loading ? (
        <p className="text-sm text-muted-foreground">Carregando...</p>
      ) : templates.length === 0 ? (
        <p className="text-sm text-muted-foreground">Nenhum template cadastrado.</p>
      ) : (
        <div className="rounded-md border">
          <table className="w-full text-sm">
            <thead>
              <tr className="border-b bg-muted/40">
                <th className="px-4 py-3 text-left font-medium">Nome</th>
                <th className="px-4 py-3 text-left font-medium">Tipo</th>
                <th className="px-4 py-3 text-left font-medium">Periodicidade</th>
                <th className="px-4 py-3 text-right font-medium">Valor Padrão</th>
                <th className="px-4 py-3 text-right font-medium"></th>
              </tr>
            </thead>
            <tbody>
              {templates.map(t => (
                <tr key={t.id} className="border-b last:border-0">
                  <td className="px-4 py-3">{t.name}</td>
                  <td className="px-4 py-3">{t.chargeType}</td>
                  <td className="px-4 py-3">{t.frequency}</td>
                  <td className="px-4 py-3 text-right">
                    {t.defaultAmount ? `R$ ${t.defaultAmount.toFixed(2)}` : '—'}
                  </td>
                  <td className="px-4 py-3 text-right">
                    <Button
                      size="icon" variant="ghost"
                      onClick={() => { if (confirm('Excluir template?')) void remove(t.id) }}
                    >
                      <Trash2 className="w-4 h-4 text-destructive" />
                    </Button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </div>
  )
}
