import { useState } from 'react'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { Plus } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import type { CategoriaResponse, CreateProdutoRequest, TipoProduto } from '@/types/estoque'

const schema = z.object({
  tipo: z.enum(['Produto', 'Servico']),
  categoriaId: z.string().min(1, 'Selecione uma categoria'),
  nome: z.string().min(1, 'Nome obrigatório').max(200),
  descricao: z.string().optional(),
  precoVenda: z.number().positive('Preço deve ser maior que zero'),
  custoMedio: z.number().min(0),
  estoqueAtual: z.number().min(0),
  estoqueMinimo: z.number().min(0),
  codigoBarras: z.string().optional(),
  duracaoMinutos: z.number().int().min(1).optional(),
})

type FormValues = z.infer<typeof schema>

interface Props {
  categorias: CategoriaResponse[]
  defaultValues?: Partial<FormValues>
  onSubmit: (data: CreateProdutoRequest) => Promise<void>
  onCancel: () => void
  onCreateCategoria?: (nome: string) => Promise<CategoriaResponse>
}

export default function ProdutoForm({ categorias, defaultValues, onSubmit, onCancel, onCreateCategoria }: Props) {
  const { register, handleSubmit, setValue, watch, formState: { errors, isSubmitting } } =
    useForm<FormValues>({
      resolver: zodResolver(schema),
      defaultValues: { tipo: 'Produto', precoVenda: 0, custoMedio: 0, estoqueAtual: 0, estoqueMinimo: 0, ...defaultValues },
    })

  const tipo = watch('tipo') as TipoProduto

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

  return (
    <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
      {/* Tipo */}
      <div className="grid grid-cols-2 gap-2">
        {(['Produto', 'Servico'] as TipoProduto[]).map(t => (
          <label
            key={t}
            className={`flex items-center justify-center gap-2 rounded-md border-2 py-2 cursor-pointer text-sm font-medium transition-colors ${
              tipo === t ? 'border-primary bg-primary/10 text-primary' : 'border-border text-muted-foreground hover:border-muted-foreground'
            }`}
          >
            <input type="radio" value={t} {...register('tipo')} className="sr-only" />
            {t === 'Produto' ? '📦 Produto' : '✂️ Serviço'}
          </label>
        ))}
      </div>

      <div className="grid gap-2">
        <div className="flex items-center justify-between">
          <Label>Categoria</Label>
          {onCreateCategoria && (
            <button
              type="button"
              onClick={() => setMostraCriarCategoria(v => !v)}
              className="flex items-center gap-1 text-xs text-primary hover:underline"
            >
              <Plus size={12} /> Nova categoria
            </button>
          )}
        </div>

        {mostraCriarCategoria && (
          <div className="space-y-1">
            <div className="flex gap-2">
              <Input
                value={novaCategoria}
                onChange={e => setNovaCategoria(e.target.value)}
                placeholder="Nome da categoria"
                className="flex-1"
                onKeyDown={e => {
                  if (e.key === 'Enter') {
                    e.preventDefault()
                    handleCriarCategoria().catch(console.error)
                  }
                }}
              />
              <Button
                type="button"
                size="sm"
                disabled={criandoCategoria || !novaCategoria.trim()}
                onClick={() => { handleCriarCategoria().catch(console.error) }}
              >
                {criandoCategoria ? 'Criando...' : 'Criar'}
              </Button>
            </div>
            {erroCategoria && <p className="text-xs text-destructive">{erroCategoria}</p>}
          </div>
        )}

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
          {errors.custoMedio && <p className="text-xs text-destructive">{errors.custoMedio.message}</p>}
        </div>
      </div>

      <div className="grid grid-cols-2 gap-4">
        <div className="grid gap-2">
          <Label>Estoque Inicial</Label>
          <Input type="number" step="0.01" {...register('estoqueAtual', { valueAsNumber: true })} />
          {errors.estoqueAtual && <p className="text-xs text-destructive">{errors.estoqueAtual.message}</p>}
        </div>
        <div className="grid gap-2">
          <Label>Estoque Mínimo</Label>
          <Input type="number" step="0.01" {...register('estoqueMinimo', { valueAsNumber: true })} />
          {errors.estoqueMinimo && <p className="text-xs text-destructive">{errors.estoqueMinimo.message}</p>}
        </div>
      </div>

      {tipo === 'Servico' && (
        <div className="grid gap-2">
          <Label>Duração (minutos)</Label>
          <Input type="number" {...register('duracaoMinutos', { valueAsNumber: true })} placeholder="Ex: 60" />
          {errors.duracaoMinutos && <p className="text-xs text-destructive">{errors.duracaoMinutos.message}</p>}
        </div>
      )}

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
