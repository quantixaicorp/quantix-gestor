import { useState, useEffect } from 'react'
import { useNavigate } from 'react-router-dom'
import { ArrowLeft, ArrowRight, Check } from 'lucide-react'
import { useCompras } from '@/hooks/useCompras'
import { useFornecedores } from '@/hooks/useFornecedores'
import ItensCompraTable from '@/components/compras/ItensCompraTable'
import PreviewParcelas from '@/components/compras/PreviewParcelas'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { toast } from '@/hooks/useToast'
import type { CreateCompraRequest, ItemCompraRequest } from '@/types/compras'

const CONDICOES = ['AVista', '30d', '30_60_90d', 'Parcelado', 'Personalizado']
const FORMAS = ['Dinheiro', 'PIX', 'Boleto', 'CartaoCredito', 'CartaoDebito', 'Transferencia']
const TIPOS = ['Mercadoria', 'Serviço', 'Ativo']

type Step = 1 | 2 | 3 | 4

interface FormState {
  fornecedorId: string
  data: string
  tipoCompra: string
  numeroNota: string
  pedidoCompraId: string
  observacoes: string
  itens: ItemCompraRequest[]
  condicaoPagamento: string
  formaPagamento: string
  qtdParcelas: number
}

function calcParcelas(condicao: string, total: number, data: string, qtd: number) {
  if (!data || total <= 0) return []
  const base = new Date(data)
  const parcelas: { numero: number; dataVencimento: string; valor: number }[] = []

  if (condicao === 'AVista') {
    parcelas.push({ numero: 1, dataVencimento: data, valor: total })
  } else if (condicao === '30d') {
    const d = new Date(base); d.setDate(d.getDate() + 30)
    parcelas.push({ numero: 1, dataVencimento: d.toISOString(), valor: total })
  } else if (condicao === '30_60_90d') {
    for (let i = 1; i <= 3; i++) {
      const d = new Date(base); d.setDate(d.getDate() + 30 * i)
      parcelas.push({ numero: i, dataVencimento: d.toISOString(), valor: Math.round(total / 3 * 100) / 100 })
    }
  } else if (condicao === 'Parcelado' && qtd > 0) {
    for (let i = 1; i <= qtd; i++) {
      const d = new Date(base); d.setDate(d.getDate() + 30 * i)
      parcelas.push({ numero: i, dataVencimento: d.toISOString(), valor: Math.round(total / qtd * 100) / 100 })
    }
  }
  return parcelas
}

export default function NovaCompra() {
  const navigate = useNavigate()
  const { create, confirmar } = useCompras()
  const { fornecedores, list: listFornecedores } = useFornecedores()
  const [step, setStep] = useState<Step>(1)
  const [saving, setSaving] = useState(false)

  const [form, setForm] = useState<FormState>({
    fornecedorId: '',
    data: new Date().toISOString().slice(0, 10),
    tipoCompra: 'Mercadoria',
    numeroNota: '',
    pedidoCompraId: '',
    observacoes: '',
    itens: [],
    condicaoPagamento: 'AVista',
    formaPagamento: 'PIX',
    qtdParcelas: 2,
  })

  useEffect(() => { void listFornecedores() }, [listFornecedores])

  const totalItens = form.itens.reduce(
    (acc, i) => acc + i.quantidade * i.valorUnitario - i.desconto + i.freteRateado + i.impostos, 0
  )

  const parcelas = form.condicaoPagamento === 'Personalizado'
    ? []
    : calcParcelas(form.condicaoPagamento, totalItens, form.data, form.qtdParcelas)

  function buildRequest(): CreateCompraRequest {
    return {
      fornecedorId: form.fornecedorId,
      data: form.data,
      tipoCompra: form.tipoCompra,
      numeroNota: form.numeroNota || undefined,
      pedidoCompraId: form.pedidoCompraId || undefined,
      observacoes: form.observacoes || undefined,
      itens: form.itens,
      condicaoPagamento: form.condicaoPagamento,
      formaPagamento: form.formaPagamento,
      qtdParcelas: form.condicaoPagamento === 'Parcelado' ? form.qtdParcelas : undefined,
    }
  }

  async function handleSaveRascunho() {
    setSaving(true)
    try {
      const c = await create(buildRequest())
      toast.success('Rascunho salvo!')
      navigate(`/compras/${c.id}`)
    } catch (e) {
      toast.error(e instanceof Error ? e.message : 'Erro ao salvar')
    } finally {
      setSaving(false)
    }
  }

  async function handleConfirmar() {
    setSaving(true)
    try {
      const c = await create(buildRequest())
      await confirmar(c.id)
      toast.success('Compra confirmada!')
      navigate(`/compras/${c.id}`)
    } catch (e) {
      toast.error(e instanceof Error ? e.message : 'Erro ao confirmar')
    } finally {
      setSaving(false)
    }
  }

  const steps = ['Cabeçalho', 'Itens', 'Pagamento', 'Revisão']

  return (
    <div className="space-y-4 max-w-5xl">
      <div className="flex items-center gap-3">
        <Button variant="ghost" size="icon" onClick={() => navigate('/compras')}>
          <ArrowLeft size={18} />
        </Button>
        <h1 className="text-2xl font-bold">Nova Compra</h1>
      </div>

      {/* Steps */}
      <div className="flex gap-1 rounded-xl border bg-card p-2">
        {steps.map((label, i) => {
          const n = (i + 1) as Step
          const active = step === n
          const done = step > n
          return (
            <button
              key={label}
              onClick={() => step > n && setStep(n)}
              className={`flex-1 rounded-lg px-3 py-2 text-sm font-medium transition-colors
                ${active ? 'bg-primary text-primary-foreground' : ''}
                ${done ? 'text-muted-foreground hover:text-foreground cursor-pointer' : ''}
                ${!active && !done ? 'text-muted-foreground cursor-default' : ''}
              `}
            >
              {done ? <Check size={12} className="inline mr-1" /> : null}{label}
            </button>
          )
        })}
      </div>

      {/* Step 1: Cabeçalho */}
      {step === 1 && (
        <div className="rounded-xl border bg-card p-6 space-y-4">
          <h2 className="font-semibold">Cabeçalho</h2>
          <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
            <div className="space-y-1">
              <Label>Fornecedor *</Label>
              <select
                value={form.fornecedorId}
                onChange={e => setForm(f => ({ ...f, fornecedorId: e.target.value }))}
                className="w-full h-9 rounded-md border border-input bg-background px-3 text-sm"
              >
                <option value="">Selecione...</option>
                {fornecedores.map(f => <option key={f.id} value={f.id}>{f.nome}</option>)}
              </select>
            </div>
            <div className="space-y-1">
              <Label>Data *</Label>
              <Input type="date" value={form.data} onChange={e => setForm(f => ({ ...f, data: e.target.value }))} />
            </div>
            <div className="space-y-1">
              <Label>Tipo de Compra *</Label>
              <select
                value={form.tipoCompra}
                onChange={e => setForm(f => ({ ...f, tipoCompra: e.target.value }))}
                className="w-full h-9 rounded-md border border-input bg-background px-3 text-sm"
              >
                {TIPOS.map(t => <option key={t} value={t}>{t}</option>)}
              </select>
            </div>
            <div className="space-y-1">
              <Label>Número da Nota</Label>
              <Input
                value={form.numeroNota}
                onChange={e => setForm(f => ({ ...f, numeroNota: e.target.value }))}
                placeholder="Opcional"
              />
            </div>
            <div className="sm:col-span-2 space-y-1">
              <Label>Observações</Label>
              <Input
                value={form.observacoes}
                onChange={e => setForm(f => ({ ...f, observacoes: e.target.value }))}
                placeholder="Opcional"
              />
            </div>
          </div>
          <div className="flex justify-end">
            <Button
              onClick={() => setStep(2)}
              disabled={!form.fornecedorId || !form.data}
            >
              Próximo <ArrowRight size={16} className="ml-2" />
            </Button>
          </div>
        </div>
      )}

      {/* Step 2: Itens */}
      {step === 2 && (
        <div className="rounded-xl border bg-card p-6 space-y-4">
          <h2 className="font-semibold">Itens</h2>
          <ItensCompraTable
            itens={form.itens}
            onChange={itens => setForm(f => ({ ...f, itens }))}
          />
          <div className="flex justify-between">
            <Button variant="outline" onClick={() => setStep(1)}>
              <ArrowLeft size={16} className="mr-2" /> Anterior
            </Button>
            <Button
              onClick={() => setStep(3)}
              disabled={form.itens.length === 0}
            >
              Próximo <ArrowRight size={16} className="ml-2" />
            </Button>
          </div>
        </div>
      )}

      {/* Step 3: Pagamento */}
      {step === 3 && (
        <div className="rounded-xl border bg-card p-6 space-y-4">
          <h2 className="font-semibold">Pagamento</h2>
          <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
            <div className="space-y-1">
              <Label>Condição de Pagamento *</Label>
              <select
                value={form.condicaoPagamento}
                onChange={e => setForm(f => ({ ...f, condicaoPagamento: e.target.value }))}
                className="w-full h-9 rounded-md border border-input bg-background px-3 text-sm"
              >
                {CONDICOES.map(c => <option key={c} value={c}>{c}</option>)}
              </select>
            </div>
            <div className="space-y-1">
              <Label>Forma de Pagamento *</Label>
              <select
                value={form.formaPagamento}
                onChange={e => setForm(f => ({ ...f, formaPagamento: e.target.value }))}
                className="w-full h-9 rounded-md border border-input bg-background px-3 text-sm"
              >
                {FORMAS.map(f => <option key={f} value={f}>{f}</option>)}
              </select>
            </div>
            {form.condicaoPagamento === 'Parcelado' && (
              <div className="space-y-1">
                <Label>Número de Parcelas</Label>
                <Input
                  type="number"
                  min={2}
                  max={60}
                  value={form.qtdParcelas}
                  onChange={e => setForm(f => ({ ...f, qtdParcelas: parseInt(e.target.value) || 2 }))}
                />
              </div>
            )}
          </div>
          {parcelas.length > 0 && (
            <div className="space-y-2">
              <p className="text-sm font-medium">Preview das Parcelas</p>
              <PreviewParcelas parcelas={parcelas} />
            </div>
          )}
          <div className="flex justify-between">
            <Button variant="outline" onClick={() => setStep(2)}>
              <ArrowLeft size={16} className="mr-2" /> Anterior
            </Button>
            <Button onClick={() => setStep(4)}>
              Próximo <ArrowRight size={16} className="ml-2" />
            </Button>
          </div>
        </div>
      )}

      {/* Step 4: Revisão */}
      {step === 4 && (
        <div className="rounded-xl border bg-card p-6 space-y-4">
          <h2 className="font-semibold">Revisão</h2>
          <div className="grid grid-cols-2 gap-x-6 gap-y-2 text-sm">
            <div><span className="text-muted-foreground">Fornecedor:</span> {fornecedores.find(f => f.id === form.fornecedorId)?.nome}</div>
            <div><span className="text-muted-foreground">Data:</span> {new Date(form.data).toLocaleDateString('pt-BR')}</div>
            <div><span className="text-muted-foreground">Tipo:</span> {form.tipoCompra}</div>
            <div><span className="text-muted-foreground">Nº Nota:</span> {form.numeroNota || '—'}</div>
            <div><span className="text-muted-foreground">Condição:</span> {form.condicaoPagamento}</div>
            <div><span className="text-muted-foreground">Forma:</span> {form.formaPagamento}</div>
            <div className="col-span-2 font-semibold text-base">
              Total: {totalItens.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' })}
            </div>
          </div>
          <ItensCompraTable itens={form.itens} onChange={() => {}} readonly />
          {parcelas.length > 0 && (
            <div className="space-y-2">
              <p className="text-sm font-medium">Parcelas</p>
              <PreviewParcelas parcelas={parcelas} />
            </div>
          )}
          <div className="flex justify-between">
            <Button variant="outline" onClick={() => setStep(3)}>
              <ArrowLeft size={16} className="mr-2" /> Anterior
            </Button>
            <div className="flex gap-2">
              <Button variant="outline" onClick={() => void handleSaveRascunho()} disabled={saving}>
                Salvar Rascunho
              </Button>
              <Button onClick={() => void handleConfirmar()} disabled={saving}>
                Confirmar Compra
              </Button>
            </div>
          </div>
        </div>
      )}
    </div>
  )
}
