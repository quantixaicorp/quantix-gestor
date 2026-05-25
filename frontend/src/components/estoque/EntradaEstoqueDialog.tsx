import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { Dialog, DialogContent, DialogHeader, DialogTitle } from '@/components/ui/dialog'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import type { ProdutoResponse } from '@/types/estoque'

const schema = z.object({
  quantidade: z.number().positive('Quantidade deve ser maior que zero'),
  custoUnitario: z.number().min(0).optional(),
  observacao: z.string().optional(),
})
type FormValues = z.infer<typeof schema>

interface Props {
  produto: ProdutoResponse | null
  open: boolean
  onClose: () => void
  onConfirm: (produtoId: string, data: FormValues) => Promise<void>
}

export default function EntradaEstoqueDialog({ produto, open, onClose, onConfirm }: Props) {
  const { register, handleSubmit, reset, formState: { errors, isSubmitting } } =
    useForm<FormValues>({ resolver: zodResolver(schema) })

  async function handleConfirm(data: FormValues) {
    if (!produto) return
    await onConfirm(produto.id, data)
    reset()
    onClose()
  }

  return (
    <Dialog open={open} onOpenChange={onClose}>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>Entrada de Estoque — {produto?.nome}</DialogTitle>
        </DialogHeader>
        <form onSubmit={handleSubmit(handleConfirm)} className="space-y-4">
          <div className="grid gap-2">
            <Label>Quantidade</Label>
            <Input type="number" step="0.01" {...register('quantidade', { valueAsNumber: true })} autoFocus />
            {errors.quantidade && <p className="text-xs text-destructive">{errors.quantidade.message}</p>}
          </div>
          <div className="grid gap-2">
            <Label>Custo Unitário (R$) — opcional</Label>
            <Input type="number" step="0.01" {...register('custoUnitario', { valueAsNumber: true })} />
          </div>
          <div className="grid gap-2">
            <Label>Observação</Label>
            <Input {...register('observacao')} placeholder="Ex: Reposição de fornecedor" />
          </div>
          <div className="flex justify-end gap-2">
            <Button type="button" variant="outline" onClick={onClose}>Cancelar</Button>
            <Button type="submit" disabled={isSubmitting}>
              {isSubmitting ? 'Salvando...' : 'Confirmar Entrada'}
            </Button>
          </div>
        </form>
      </DialogContent>
    </Dialog>
  )
}
