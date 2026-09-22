import { useState } from 'react'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { Plus } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import type { CategoriaResponse, CreateProdutoRequest } from '@/types/estoque'

const schema = z.object({
  categoryId: z.string().min(1, 'Selecione uma categoria'),
  name: z.string().min(1, 'Nome obrigatório').max(200),
  description: z.string().optional(),
  salePrice: z.number().positive('Preço deve ser maior que zero'),
  averageCost: z.number().min(0),
  currentStock: z.number().min(0),
  minimumStock: z.number().min(0),
  barcode: z.string().optional(),
})

type FormValues = z.infer<typeof schema>

interface Props {
  categorias: CategoriaResponse[]
  onSubmit: (data: CreateProdutoRequest) => Promise<void>
  onCancel: () => void
  onCreateCategoria?: (nome: string) => Promise<CategoriaResponse>
}

export default function ProdutoCriarForm({ categorias, onSubmit, onCancel, onCreateCategoria }: Props) {
  const { register, handleSubmit, setValue, formState: { errors, isSubmitting } } =
    useForm<FormValues>({
      resolver: zodResolver(schema),
      defaultValues: { salePrice: 0, averageCost: 0, currentStock: 0, minimumStock: 0 },
    })

  const [novaCategoria, setNovaCategoria] = useState('')
  const [criandoCategoria, setCriandoCategoria] = useState(false)
  const [mostraCriarCategoria, setMostraCriarCategoria] = useState(false)
  const [erroCategoria, setErroCategoria] = useState<string | null>(null)

  async function handleCriarCategoria() {
    if (!novaCategoria.trim() || !onCreateCategoria) return
    setErroCategoria(null)
    setCriandoCategoria(true)
    try {
      const criada = await onCreateCategoria(novaCategoria.trim())
      setValue('categoryId', criada.id)
      setNovaCategoria('')
      setMostraCriarCategoria(false)
    } catch (e) {
      setErroCategoria(e instanceof Error ? e.message : 'Erro ao criar categoria')
    } finally {
      setCriandoCategoria(false)
    }
  }

  async function submit(values: FormValues) {
    await onSubmit({ ...values, type: 'Produto' })
  }

  return (
    <form onSubmit={handleSubmit(submit)} className="space-y-4">
      <div className="grid gap-2">
        <div className="flex items-center justify-between">
          <Label>Categoria</Label>
          {onCreateCategoria && (
            <button type="button" onClick={() => setMostraCriarCategoria(v => !v)}
              className="flex items-center gap-1 text-xs text-primary hover:underline">
              <Plus size={12} /> Nova categoria
            </button>
          )}
        </div>
        {mostraCriarCategoria && (
          <div className="space-y-1">
            <div className="flex gap-2">
              <Input value={novaCategoria} onChange={e => setNovaCategoria(e.target.value)}
                placeholder="Nome da categoria" className="flex-1"
                onKeyDown={e => { if (e.key === 'Enter') { e.preventDefault(); handleCriarCategoria().catch(console.error) } }} />
              <Button type="button" size="sm" disabled={criandoCategoria || !novaCategoria.trim()}
                onClick={() => { handleCriarCategoria().catch(console.error) }}>
                {criandoCategoria ? 'Criando...' : 'Criar'}
              </Button>
            </div>
            {erroCategoria && <p className="text-xs text-destructive">{erroCategoria}</p>}
          </div>
        )}
        <select {...register('categoryId')}
          className="flex h-9 w-full rounded-md border border-input bg-transparent px-3 py-1 text-sm">
          <option value="">Selecione...</option>
          {categorias.map(c => <option key={c.id} value={c.id}>{c.name}</option>)}
        </select>
        {errors.categoryId && <p className="text-xs text-destructive">{errors.categoryId.message}</p>}
      </div>

      <div className="grid gap-2">
        <Label>Nome</Label>
        <Input {...register('name')} placeholder="Nome do produto" />
        {errors.name && <p className="text-xs text-destructive">{errors.name.message}</p>}
      </div>

      <div className="grid grid-cols-2 gap-4">
        <div className="grid gap-2">
          <Label>Preço de Venda (R$)</Label>
          <Input type="number" step="0.01" {...register('salePrice', { valueAsNumber: true })} />
          {errors.salePrice && <p className="text-xs text-destructive">{errors.salePrice.message}</p>}
        </div>
        <div className="grid gap-2">
          <Label>Custo Médio (R$)</Label>
          <Input type="number" step="0.01" {...register('averageCost', { valueAsNumber: true })} />
          {errors.averageCost && <p className="text-xs text-destructive">{errors.averageCost.message}</p>}
        </div>
      </div>

      <div className="grid grid-cols-2 gap-4">
        <div className="grid gap-2">
          <Label>Estoque Inicial</Label>
          <Input type="number" step="0.01" {...register('currentStock', { valueAsNumber: true })} />
          {errors.currentStock && <p className="text-xs text-destructive">{errors.currentStock.message}</p>}
        </div>
        <div className="grid gap-2">
          <Label>Estoque Mínimo</Label>
          <Input type="number" step="0.01" {...register('minimumStock', { valueAsNumber: true })} />
          {errors.minimumStock && <p className="text-xs text-destructive">{errors.minimumStock.message}</p>}
        </div>
      </div>

      <div className="grid gap-2">
        <Label>Código de Barras (opcional)</Label>
        <Input {...register('barcode')} placeholder="EAN-13" />
      </div>

      <div className="flex justify-end gap-2 pt-2">
        <Button type="button" variant="outline" onClick={onCancel}>Cancelar</Button>
        <Button type="submit" disabled={isSubmitting}>{isSubmitting ? 'Salvando...' : 'Salvar'}</Button>
      </div>
    </form>
  )
}
