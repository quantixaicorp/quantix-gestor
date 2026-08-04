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
  minimumStock: z.number().min(0),
  barcode: z.string().optional(),
  isActive: z.boolean(),
})

type FormValues = z.infer<typeof schema>

interface Props {
  produto: ProdutoResponse
  categorias: CategoriaResponse[]
  onSubmit: (id: string, data: UpdateProdutoRequest) => Promise<void>
  onCancel: () => void
}

export default function ProdutoEditForm({ produto, categorias, onSubmit, onCancel }: Props) {
  const { register, handleSubmit, formState: { errors, isSubmitting } } =
    useForm<FormValues>({
      resolver: zodResolver(schema),
      defaultValues: {
        categoryId: produto.categoryId,
        name: produto.name,
        description: produto.description ?? '',
        salePrice: produto.salePrice,
        minimumStock: produto.minimumStock,
        barcode: produto.barcode ?? '',
        isActive: produto.isActive,
      },
    })

  async function submit(values: FormValues) {
    await onSubmit(produto.id, {
      categoryId: values.categoryId,
      name: values.name,
      description: values.description || undefined,
      salePrice: values.salePrice,
      minimumStock: values.minimumStock,
      barcode: values.barcode || undefined,
      isActive: values.isActive,
      durationMinutes: null,
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
        <Label>Nome</Label>
        <Input {...register('name')} />
        {errors.name && <p className="text-xs text-destructive">{errors.name.message}</p>}
      </div>

      <div className="grid grid-cols-2 gap-4">
        <div className="grid gap-2">
          <Label>Preço de Venda (R$)</Label>
          <Input type="number" step="0.01" {...register('salePrice', { valueAsNumber: true })} />
          {errors.salePrice && <p className="text-xs text-destructive">{errors.salePrice.message}</p>}
        </div>
        <div className="grid gap-2">
          <Label>Estoque Mínimo</Label>
          <Input type="number" step="0.01" {...register('minimumStock', { valueAsNumber: true })} />
        </div>
      </div>

      <div className="grid gap-2">
        <Label>Código de Barras (opcional)</Label>
        <Input {...register('barcode')} placeholder="EAN-13" />
      </div>

      <div className="flex items-center gap-2">
        <input type="checkbox" id="ativo-produto" {...register('isActive')} className="h-4 w-4 rounded border" />
        <Label htmlFor="ativo-produto">Produto ativo</Label>
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
