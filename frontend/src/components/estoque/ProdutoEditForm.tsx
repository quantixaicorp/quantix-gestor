import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import type { CategoriaResponse, ProdutoResponse, UpdateProdutoRequest } from '@/types/estoque'

const schema = z.object({
  categoriaId: z.string().min(1, 'Selecione uma categoria'),
  nome: z.string().min(1, 'Nome obrigatório').max(200),
  descricao: z.string().optional(),
  precoVenda: z.number().positive('Preço deve ser maior que zero'),
  estoqueMinimo: z.number().min(0).default(0),
  codigoBarras: z.string().optional(),
  ativo: z.boolean(),
  duracaoMinutos: z.number().int().min(1).optional().nullable(),
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
        categoriaId: produto.categoriaId,
        nome: produto.nome,
        descricao: produto.descricao ?? '',
        precoVenda: produto.precoVenda,
        estoqueMinimo: produto.estoqueMinimo,
        codigoBarras: produto.codigoBarras ?? '',
        ativo: produto.ativo,
        duracaoMinutos: produto.duracaoMinutos ?? undefined,
      },
    })

  async function submit(values: FormValues) {
    await onSubmit(produto.id, {
      categoriaId: values.categoriaId,
      nome: values.nome,
      descricao: values.descricao || undefined,
      precoVenda: values.precoVenda,
      estoqueMinimo: produto.tipo === 'Servico' ? 0 : values.estoqueMinimo,
      codigoBarras: values.codigoBarras || undefined,
      ativo: values.ativo,
      duracaoMinutos: values.duracaoMinutos ?? null,
    })
  }

  return (
    <form onSubmit={handleSubmit(submit)} className="space-y-4">
      <div className="grid gap-2">
        <Label>Categoria</Label>
        <select
          {...register('categoriaId')}
          className="flex h-9 w-full rounded-md border border-input bg-transparent px-3 py-1 text-sm"
        >
          <option value="">Selecione...</option>
          {categorias.map(c => <option key={c.id} value={c.id}>{c.nome}</option>)}
        </select>
        {errors.categoriaId && <p className="text-xs text-destructive">{errors.categoriaId.message}</p>}
      </div>

      <div className="grid gap-2">
        <Label>Nome</Label>
        <Input {...register('nome')} />
        {errors.nome && <p className="text-xs text-destructive">{errors.nome.message}</p>}
      </div>

      <div className="grid grid-cols-2 gap-4">
        <div className="grid gap-2">
          <Label>Preço de Venda (R$)</Label>
          <Input type="number" step="0.01" {...register('precoVenda', { valueAsNumber: true })} />
          {errors.precoVenda && <p className="text-xs text-destructive">{errors.precoVenda.message}</p>}
        </div>
        <div className={`grid gap-2 ${produto.tipo === 'Servico' ? 'hidden' : ''}`}>
          <Label>Estoque Mínimo</Label>
          <Input type="number" step="0.01" {...register('estoqueMinimo', { valueAsNumber: true })} />
        </div>
      </div>

      {produto.tipo === 'Servico' && (
        <div className="grid gap-2">
          <Label>Duração (minutos)</Label>
          <Input type="number" {...register('duracaoMinutos', { valueAsNumber: true, setValueAs: v => v === '' ? null : Number(v) })} placeholder="Ex: 60" />
          {errors.duracaoMinutos && <p className="text-xs text-destructive">{errors.duracaoMinutos.message}</p>}
        </div>
      )}

      <div className="grid gap-2">
        <Label>Código de Barras (opcional)</Label>
        <Input {...register('codigoBarras')} placeholder="EAN-13" />
      </div>

      <div className="flex items-center gap-2">
        <input type="checkbox" id="ativo" {...register('ativo')} className="h-4 w-4 rounded border" />
        <Label htmlFor="ativo">{produto.tipo === 'Servico' ? 'Serviço ativo' : 'Produto ativo'}</Label>
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
