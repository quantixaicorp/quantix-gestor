import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import type { CreateLancamentoRequest } from '@/types/financeiro'

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

const categoriasDespesa = ['Aluguel', 'Fornecedor', 'Utilidades', 'Salários', 'Marketing', 'Outros']
const categoriasReceita = ['Venda', 'Serviço', 'Outros']

interface Props {
  defaultTipo?: 'Receita' | 'Despesa'
  defaultValues?: Partial<FormValues>
  onSubmit: (data: CreateLancamentoRequest) => Promise<void>
  onCancel: () => void
}

export default function LancamentoForm({ defaultTipo = 'Despesa', defaultValues, onSubmit, onCancel }: Props) {
  const { register, watch, handleSubmit, formState: { errors, isSubmitting } } =
    useForm<FormValues>({
      resolver: zodResolver(schema),
      defaultValues: { tipo: defaultTipo ?? 'Despesa', ...defaultValues },
    })

  const tipo = watch('tipo')
  const categorias = tipo === 'Despesa' ? categoriasDespesa : categoriasReceita

  function handleFormSubmit(data: FormValues) {
    return onSubmit({ ...data, valor: parseFloat(data.valor) })
  }

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
        <Input {...register('descricao')} placeholder="Ex: Aluguel março" />
        {errors.descricao && <p className="text-xs text-destructive">{errors.descricao.message}</p>}
      </div>

      <div className="grid grid-cols-2 gap-4">
        <div className="grid gap-2">
          <Label>Valor (R$)</Label>
          <Input type="number" step="0.01" {...register('valor')} />
          {errors.valor && <p className="text-xs text-destructive">{errors.valor.message}</p>}
        </div>
        <div className="grid gap-2">
          <Label>Vencimento</Label>
          <Input type="date" {...register('dataVencimento')} />
          {errors.dataVencimento && <p className="text-xs text-destructive">{errors.dataVencimento.message}</p>}
        </div>
      </div>

      <div className="grid gap-2">
        <Label>Categoria</Label>
        <select {...register('categoria')}
          className="flex h-9 w-full rounded-md border border-input bg-transparent px-3 py-1 text-sm">
          <option value="">Selecione...</option>
          {categorias.map(c => <option key={c} value={c}>{c}</option>)}
        </select>
        {errors.categoria && <p className="text-xs text-destructive">{errors.categoria.message}</p>}
      </div>

      <div className="grid gap-2">
        <Label>Observação (opcional)</Label>
        <Input {...register('observacao')} placeholder="Anotações" />
      </div>

      <div className="flex justify-end gap-2 pt-2">
        <Button type="button" variant="outline" onClick={onCancel}>Cancelar</Button>
        <Button type="submit" disabled={isSubmitting}>
          {isSubmitting ? 'Salvando...' : 'Salvar'}
        </Button>
      </div>
    </form>
  )
}
