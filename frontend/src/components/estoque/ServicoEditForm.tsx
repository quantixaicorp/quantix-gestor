import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import type { CategoriaResponse, ProdutoResponse, UpdateProdutoRequest } from '@/types/estoque'

const schema = z.object({
  categoryId: z.string().min(1, 'Selecione uma categoria'),
  name: z.string().min(1, 'Nome obrigatório').max(200),
  description: z.string().optional(),
  salePrice: z.number().positive('Preço deve ser maior que zero'),
  durationMinutes: z.number().int().min(1, 'Informe a duração'),
  isActive: z.boolean(),
})

type FormValues = z.infer<typeof schema>

interface Props {
  servico: ProdutoResponse
  categorias: CategoriaResponse[]
  onSubmit: (id: string, data: UpdateProdutoRequest) => Promise<void>
  onCancel: () => void
}

export default function ServicoEditForm({ servico, categorias, onSubmit, onCancel }: Props) {
  const { register, handleSubmit, formState: { errors, isSubmitting } } =
    useForm<FormValues>({
      resolver: zodResolver(schema),
      defaultValues: {
        categoryId: servico.categoryId,
        name: servico.name,
        description: servico.description ?? '',
        salePrice: servico.salePrice,
        durationMinutes: servico.durationMinutes ?? 60,
        isActive: servico.isActive,
      },
    })

  async function submit(values: FormValues) {
    await onSubmit(servico.id, {
      categoryId: values.categoryId,
      name: values.name,
      description: values.description || undefined,
      salePrice: values.salePrice,
      minimumStock: 0,
      barcode: undefined,
      isActive: values.isActive,
      durationMinutes: values.durationMinutes,
    })
  }

  return (
    <form onSubmit={handleSubmit(submit)} className="space-y-4">
      <div className="grid gap-2">
        <Label>Categoria</Label>
        <select
          {...register('categoryId')}
          className="flex h-9 w-full rounded-md border border-input bg-transparent px-3 py-1 text-sm"
        >
          <option value="">Selecione...</option>
          {categorias.map(c => <option key={c.id} value={c.id}>{c.name}</option>)}
        </select>
        {errors.categoryId && <p className="text-xs text-destructive">{errors.categoryId.message}</p>}
      </div>

      <div className="grid gap-2">
        <Label>Nome do Serviço</Label>
        <Input {...register('name')} />
        {errors.name && <p className="text-xs text-destructive">{errors.name.message}</p>}
      </div>

      <div className="grid grid-cols-2 gap-4">
        <div className="grid gap-2">
          <Label>Preço (R$)</Label>
          <Input type="number" step="0.01" {...register('salePrice', { valueAsNumber: true })} />
          {errors.salePrice && <p className="text-xs text-destructive">{errors.salePrice.message}</p>}
        </div>
        <div className="grid gap-2">
          <Label>Duração (minutos)</Label>
          <Input type="number" {...register('durationMinutes', { valueAsNumber: true })} placeholder="Ex: 60" />
          {errors.durationMinutes && <p className="text-xs text-destructive">{errors.durationMinutes.message}</p>}
        </div>
      </div>

      <div className="grid gap-2">
        <Label>Descrição (opcional)</Label>
        <Input {...register('description')} placeholder="Detalhes do serviço" />
      </div>

      <div className="flex items-center gap-2">
        <input type="checkbox" id="ativo-servico" {...register('isActive')} className="h-4 w-4 rounded border" />
        <Label htmlFor="ativo-servico">Serviço ativo</Label>
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
