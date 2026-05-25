import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import type { CategoriaResponse, CreateProdutoRequest } from '@/types/estoque'

const schema = z.object({
  categoriaId: z.string().min(1, 'Selecione uma categoria'),
  nome: z.string().min(1, 'Nome obrigatório').max(200),
  descricao: z.string().optional(),
  precoVenda: z.number().positive('Preço deve ser maior que zero'),
  custoMedio: z.number().min(0),
  estoqueAtual: z.number().min(0),
  estoqueMinimo: z.number().min(0),
  codigoBarras: z.string().optional(),
})

type FormValues = z.infer<typeof schema>

interface Props {
  categorias: CategoriaResponse[]
  defaultValues?: Partial<FormValues>
  onSubmit: (data: CreateProdutoRequest) => Promise<void>
  onCancel: () => void
}

export default function ProdutoForm({ categorias, defaultValues, onSubmit, onCancel }: Props) {
  const { register, handleSubmit, formState: { errors, isSubmitting } } =
    useForm<FormValues>({ resolver: zodResolver(schema), defaultValues })

  return (
    <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
      <div className="grid gap-2">
        <Label>Categoria</Label>
        <select {...register('categoriaId')}
          className="flex h-9 w-full rounded-md border border-input bg-transparent px-3 py-1 text-sm">
          <option value="">Selecione...</option>
          {categorias.map(c => <option key={c.id} value={c.id}>{c.nome}</option>)}
        </select>
        {errors.categoriaId && <p className="text-xs text-destructive">{errors.categoriaId.message}</p>}
      </div>

      <div className="grid gap-2">
        <Label>Nome</Label>
        <Input {...register('nome')} placeholder="Nome do produto" />
        {errors.nome && <p className="text-xs text-destructive">{errors.nome.message}</p>}
      </div>

      <div className="grid grid-cols-2 gap-4">
        <div className="grid gap-2">
          <Label>Preço de Venda (R$)</Label>
          <Input type="number" step="0.01" {...register('precoVenda', { valueAsNumber: true })} />
          {errors.precoVenda && <p className="text-xs text-destructive">{errors.precoVenda.message}</p>}
        </div>
        <div className="grid gap-2">
          <Label>Custo Médio (R$)</Label>
          <Input type="number" step="0.01" {...register('custoMedio', { valueAsNumber: true })} />
        </div>
      </div>

      <div className="grid grid-cols-2 gap-4">
        <div className="grid gap-2">
          <Label>Estoque Inicial</Label>
          <Input type="number" step="0.01" {...register('estoqueAtual', { valueAsNumber: true })} />
        </div>
        <div className="grid gap-2">
          <Label>Estoque Mínimo</Label>
          <Input type="number" step="0.01" {...register('estoqueMinimo', { valueAsNumber: true })} />
        </div>
      </div>

      <div className="grid gap-2">
        <Label>Código de Barras (opcional)</Label>
        <Input {...register('codigoBarras')} placeholder="EAN-13" />
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
