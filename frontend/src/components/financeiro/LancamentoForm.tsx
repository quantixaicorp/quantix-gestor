import { useEffect, useState } from 'react'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { useCategoriasLancamento } from '@/hooks/useCategoriasLancamento'
import type { CreateLancamentoRequest, CategoriaLancamentoResponse } from '@/types/financeiro'

const schema = z.object({
  tipo: z.enum(['Receita', 'Despesa']),
  descricao: z.string().min(1, 'Descrição obrigatória').max(300),
  valor: z.string()
    .min(1, 'Valor obrigatório')
    .refine(v => !isNaN(parseFloat(v)) && parseFloat(v) > 0, 'Valor deve ser maior que zero'),
  dataVencimento: z.string().min(1, 'Data obrigatória'),
  categoria: z.string().min(1, 'Categoria obrigatória').max(100),
  observacao: z.string().optional(),
})
type FormValues = z.infer<typeof schema>

function addMonths(dateStr: string, months: number): string {
  const d = new Date(dateStr + 'T12:00:00Z')
  d.setUTCMonth(d.getUTCMonth() + months)
  return d.toISOString().slice(0, 10)
}

function buildParcelas(
  descricao: string,
  valorParcela: number,
  dataVencimento: string,
  numParcelas: number,
  tipo: 'Receita' | 'Despesa',
  categoria: string,
  observacao?: string,
): CreateLancamentoRequest[] {
  return Array.from({ length: numParcelas }, (_, i) => ({
    tipo,
    descricao: `${descricao} ${i + 1}/${numParcelas}`,
    valor: valorParcela,
    dataVencimento: addMonths(dataVencimento, i),
    categoria,
    observacao,
  }))
}

interface Props {
  defaultTipo?: 'Receita' | 'Despesa'
  defaultValues?: Partial<FormValues>
  allowParcelamento?: boolean
  onSubmit: (data: CreateLancamentoRequest) => Promise<void>
  onAllCreated?: (count: number) => void
  onCancel: () => void
}

export default function LancamentoForm({ defaultTipo = 'Despesa', defaultValues, allowParcelamento, onSubmit, onAllCreated, onCancel }: Props) {
  const { register, watch, handleSubmit, setValue, formState: { errors, isSubmitting } } =
    useForm<FormValues>({
      resolver: zodResolver(schema),
      defaultValues: { tipo: defaultTipo ?? 'Despesa', ...defaultValues },
    })

  const tipo = watch('tipo')
  const descricaoWatch = watch('descricao')
  const valorWatch = watch('valor')
  const dataWatch = watch('dataVencimento')
  const { list: listCategorias } = useCategoriasLancamento()
  const [categorias, setCategorias] = useState<CategoriaLancamentoResponse[]>([])
  const [parcelado, setParcelado] = useState(false)
  const [numParcelas, setNumParcelas] = useState(2)
  const [numParcelasStr, setNumParcelasStr] = useState('2')
  const [saving, setSaving] = useState(false)
  const [progresso, setProgresso] = useState<{ atual: number; total: number } | null>(null)

  useEffect(() => {
    void listCategorias(tipo).then(cats => {
      setCategorias(cats)
      if (!defaultValues?.categoria) setValue('categoria', '')
    })
  }, [tipo, listCategorias, setValue, defaultValues?.categoria])

  const previewParcelas = parcelado && valorWatch && dataWatch && descricaoWatch
    ? buildParcelas(
        descricaoWatch,
        parseFloat(valorWatch) || 0,
        dataWatch,
        numParcelas,
        tipo,
        '',
      )
    : []

  async function handleFormSubmit(data: FormValues) {
    if (parcelado) {
      const parcelas = buildParcelas(
        data.descricao,
        parseFloat(data.valor),
        data.dataVencimento,
        numParcelas,
        data.tipo,
        data.categoria,
        data.observacao,
      )
      setSaving(true)
      setProgresso({ atual: 0, total: parcelas.length })
      try {
        for (let i = 0; i < parcelas.length; i++) {
          setProgresso({ atual: i + 1, total: parcelas.length })
          await onSubmit(parcelas[i])
        }
        onAllCreated?.(parcelas.length)
      } finally {
        setSaving(false)
        setProgresso(null)
      }
    } else {
      await onSubmit({ ...data, valor: parseFloat(data.valor) })
      onAllCreated?.(1)
    }
  }

  const fmtVal = (v: number) => v.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' })
  const fmtDate = (s: string) => new Date(s + 'T12:00:00Z').toLocaleDateString('pt-BR')

  return (
    <form onSubmit={handleSubmit(handleFormSubmit)} className="space-y-4">
      <div className="grid gap-2">
        <Label>Tipo</Label>
        <div className="flex gap-2">
          {(['Despesa', 'Receita'] as const).map(t => (
            <label key={t} className="flex items-center gap-2 cursor-pointer">
              <input type="radio" value={t} {...register('tipo')} />
              <span className="text-sm">{t}</span>
            </label>
          ))}
        </div>
      </div>

      <div className="grid gap-2">
        <Label>Descrição</Label>
        <Input {...register('descricao')} placeholder="Ex: Aluguel" />
        {errors.descricao && <p className="text-xs text-destructive">{errors.descricao.message}</p>}
      </div>

      <div className="grid grid-cols-2 gap-4">
        <div className="grid gap-2">
          <Label>{parcelado ? 'Valor por parcela (R$)' : 'Valor (R$)'}</Label>
          <Input type="number" step="0.01" {...register('valor')} />
          {errors.valor && <p className="text-xs text-destructive">{errors.valor.message}</p>}
        </div>
        <div className="grid gap-2">
          <Label>{parcelado ? 'Vencimento da 1ª parcela' : 'Vencimento'}</Label>
          <Input type="date" {...register('dataVencimento')} />
          {errors.dataVencimento && <p className="text-xs text-destructive">{errors.dataVencimento.message}</p>}
        </div>
      </div>

      <div className="grid gap-2">
        <Label>Categoria</Label>
        <select {...register('categoria')}
          className="flex h-9 w-full rounded-md border border-input bg-transparent px-3 py-1 text-sm">
          <option value="">Selecione...</option>
          {categorias.map(c => <option key={c.id} value={c.nome}>{c.nome}</option>)}
        </select>
        {errors.categoria && <p className="text-xs text-destructive">{errors.categoria.message}</p>}
      </div>

      <div className="grid gap-2">
        <Label>Observação (opcional)</Label>
        <Input {...register('observacao')} placeholder="Anotações" />
      </div>

      {allowParcelamento && (
        <div className="space-y-3 rounded-lg border bg-muted/30 p-3">
          <label className="flex items-center gap-2 cursor-pointer">
            <input
              type="checkbox"
              checked={parcelado}
              onChange={e => setParcelado(e.target.checked)}
              className="h-4 w-4 rounded"
            />
            <span className="text-sm font-medium">Lançamento parcelado</span>
          </label>

          {parcelado && (
            <div className="space-y-3">
              <div className="flex items-center gap-3">
                <Label className="shrink-0">Nº de parcelas</Label>
                <Input
                  type="text"
                  inputMode="numeric"
                  value={numParcelasStr}
                  onChange={e => {
                    const raw = e.target.value.replace(/\D/g, '')
                    setNumParcelasStr(raw)
                    const n = parseInt(raw)
                    if (!isNaN(n) && n >= 2 && n <= 60) setNumParcelas(n)
                  }}
                  onBlur={() => {
                    const n = parseInt(numParcelasStr)
                    const clamped = isNaN(n) ? 2 : Math.max(2, Math.min(60, n))
                    setNumParcelas(clamped)
                    setNumParcelasStr(String(clamped))
                  }}
                  className="w-24 h-8 text-center"
                />
              </div>

              {previewParcelas.length > 0 && (
                <div className="rounded-md border overflow-hidden">
                  <div className="bg-muted/50 px-3 py-1.5 text-xs font-semibold text-muted-foreground uppercase tracking-wide">
                    Prévia das parcelas
                  </div>
                  <div className="divide-y max-h-48 overflow-y-auto">
                    {previewParcelas.map((p, i) => (
                      <div key={i} className="flex items-center justify-between px-3 py-2 text-sm">
                        <span className="text-muted-foreground">{i + 1}ª · {fmtDate(p.dataVencimento)}</span>
                        <span className="font-medium">{fmtVal(p.valor)}</span>
                      </div>
                    ))}
                  </div>
                  <div className="bg-muted/30 px-3 py-1.5 flex justify-between text-xs font-semibold">
                    <span>Total ({numParcelas}x)</span>
                    <span>{fmtVal(previewParcelas.reduce((s, p) => s + p.valor, 0))}</span>
                  </div>
                </div>
              )}
            </div>
          )}
        </div>
      )}

      {progresso && (
        <div className="rounded-lg bg-primary/10 px-4 py-3 text-sm text-primary font-medium text-center">
          Criando parcela {progresso.atual} de {progresso.total}...
        </div>
      )}

      <div className="flex justify-end gap-2 pt-2">
        <Button type="button" variant="outline" onClick={onCancel} disabled={saving}>Cancelar</Button>
        <Button type="submit" disabled={isSubmitting || saving}>
          {saving
            ? `Salvando ${progresso?.atual ?? ''}/${progresso?.total ?? ''}...`
            : parcelado
              ? `Criar ${numParcelas} parcelas`
              : 'Salvar'}
        </Button>
      </div>
    </form>
  )
}
