import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import type { CreateClienteRequest } from '@/types/clientes'

const schema = z.object({
  nome: z.string().min(1, 'Nome obrigatório').max(200),
  whatsapp: z.string().min(8, 'WhatsApp obrigatório').max(20),
  email: z.string().email('E-mail inválido').optional().or(z.literal('')),
  observacoes: z.string().optional(),
})
type FormValues = z.infer<typeof schema>

interface Props {
  defaultValues?: Partial<FormValues>
  onSubmit: (data: CreateClienteRequest) => Promise<void>
  onCancel: () => void
}

export default function ClienteForm({ defaultValues, onSubmit, onCancel }: Props) {
  const { register, handleSubmit, formState: { errors, isSubmitting } } =
    useForm<FormValues>({ resolver: zodResolver(schema), defaultValues })

  return (
    <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
      <div className="grid gap-2">
        <Label>Nome</Label>
        <Input {...register('nome')} placeholder="Nome completo" />
        {errors.nome && <p className="text-xs text-destructive">{errors.nome.message}</p>}
      </div>
      <div className="grid gap-2">
        <Label>WhatsApp</Label>
        <Input {...register('whatsapp')} placeholder="11999990000" />
        {errors.whatsapp && <p className="text-xs text-destructive">{errors.whatsapp.message}</p>}
      </div>
      <div className="grid gap-2">
        <Label>E-mail (opcional)</Label>
        <Input {...register('email')} type="email" placeholder="email@exemplo.com" />
        {errors.email && <p className="text-xs text-destructive">{errors.email.message}</p>}
      </div>
      <div className="grid gap-2">
        <Label>Observações</Label>
        <Input {...register('observacoes')} placeholder="Anotações rápidas" />
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
