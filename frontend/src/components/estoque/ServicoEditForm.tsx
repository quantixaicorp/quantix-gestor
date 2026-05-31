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
  duracaoMinutos: z.number().int().min(1, 'Informe a duração'),
  ativo: z.boolean(),
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
        categoriaId: servico.categoriaId,
        nome: servico.nome,
        descricao: servico.descricao ?? '',
        precoVenda: servico.precoVenda,
        duracaoMinutos: servico.duracaoMinutos ?? 60,
        ativo: servico.ativo,
      },
    })

  async function submit(values: FormValues) {
    await onSubmit(servico.id, {
      categoriaId: values.categoriaId,
      nome: values.nome,
      descricao: values.descricao || undefined,
      precoVenda: values.precoVenda,
      estoqueMinimo: 0,
      codigoBarras: undefined,
      ativo: values.ativo,
      duracaoMinutos: values.duracaoMinutos,
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
        <Label>Nome do Serviço</Label>
        <Input {...register('nome')} />
        {errors.nome && <p className="text-xs text-destructive">{errors.nome.message}</p>}
      </div>

      <div className="grid grid-cols-2 gap-4">
        <div className="grid gap-2">
          <Label>Preço (R$)</Label>
          <Input type="number" step="0.01" {...register('precoVenda', { valueAsNumber: true })} />
          {errors.precoVenda && <p className="text-xs text-destructive">{errors.precoVenda.message}</p>}
        </div>
        <div className="grid gap-2">
          <Label>Duração (minutos)</Label>
          <Input type="number" {...register('duracaoMinutos', { valueAsNumber: true })} placeholder="Ex: 60" />
          {errors.duracaoMinutos && <p className="text-xs text-destructive">{errors.duracaoMinutos.message}</p>}
        </div>
      </div>

      <div className="grid gap-2">
        <Label>Descrição (opcional)</Label>
        <Input {...register('descricao')} placeholder="Detalhes do serviço" />
      </div>

      <div className="flex items-center gap-2">
        <input type="checkbox" id="ativo-servico" {...register('ativo')} className="h-4 w-4 rounded border" />
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
