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
  categoriaId: z.string().min(1, 'Selecione uma categoria'),
  nome: z.string().min(1, 'Nome obrigatório').max(200),
  descricao: z.string().optional(),
  precoVenda: z.number().positive('Preço deve ser maior que zero'),
  duracaoMinutos: z.number().int().min(1, 'Informe a duração em minutos'),
  codigoBarras: z.string().optional(),
})

type FormValues = z.infer<typeof schema>

interface Props {
  categorias: CategoriaResponse[]
  onSubmit: (data: CreateProdutoRequest) => Promise<void>
  onCancel: () => void
  onCreateCategoria?: (nome: string) => Promise<CategoriaResponse>
}

export default function ServicoCriarForm({ categorias, onSubmit, onCancel, onCreateCategoria }: Props) {
  const { register, handleSubmit, setValue, formState: { errors, isSubmitting } } =
    useForm<FormValues>({
      resolver: zodResolver(schema),
      defaultValues: { precoVenda: 0, duracaoMinutos: 60 },
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
      setValue('categoriaId', criada.id)
      setNovaCategoria('')
      setMostraCriarCategoria(false)
    } catch (e) {
      setErroCategoria(e instanceof Error ? e.message : 'Erro ao criar categoria')
    } finally {
      setCriandoCategoria(false)
    }
  }

  async function submit(values: FormValues) {
    await onSubmit({
      categoriaId: values.categoriaId,
      nome: values.nome,
      descricao: values.descricao,
      precoVenda: values.precoVenda,
      custoMedio: 0,
      estoqueAtual: 0,
      estoqueMinimo: 0,
      codigoBarras: values.codigoBarras,
      tipo: 'Servico',
    })
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
        <select {...register('categoriaId')}
          className="flex h-9 w-full rounded-md border border-input bg-transparent px-3 py-1 text-sm">
          <option value="">Selecione...</option>
          {categorias.map(c => <option key={c.id} value={c.id}>{c.nome}</option>)}
        </select>
        {errors.categoriaId && <p className="text-xs text-destructive">{errors.categoriaId.message}</p>}
      </div>

      <div className="grid gap-2">
        <Label>Nome do Serviço</Label>
        <Input {...register('nome')} placeholder="Ex: Corte de cabelo" />
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
          <Input type="number" {...register('duracaoMinutos', { valueAsNumber: true })} placeholder="60" />
          {errors.duracaoMinutos && <p className="text-xs text-destructive">{errors.duracaoMinutos.message}</p>}
        </div>
      </div>

      <div className="grid gap-2">
        <Label>Descrição (opcional)</Label>
        <Input {...register('descricao')} placeholder="Detalhes do serviço" />
      </div>

      <div className="flex justify-end gap-2 pt-2">
        <Button type="button" variant="outline" onClick={onCancel}>Cancelar</Button>
        <Button type="submit" disabled={isSubmitting}>{isSubmitting ? 'Salvando...' : 'Salvar'}</Button>
      </div>
    </form>
  )
}
