import { Controller, useForm, useWatch } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { cn } from '@/lib/utils'
import {
  isValidDocumento,
  labelDocumento,
  maskDocumento,
  normalizeDocumento,
  placeholderDocumento,
  type TipoPessoa,
} from '@/lib/documento'
import type { CreateClienteRequest } from '@/types/clientes'

const schema = z.object({
  name: z.string().min(1, 'Nome obrigatório').max(200),
  whatsApp: z.string().min(8, 'WhatsApp obrigatório').max(20),
  email: z.string().email('E-mail inválido').optional().or(z.literal('')),
  notes: z.string().optional(),
  personType: z.enum(['PF', 'PJ']),
  cnpjCpf: z.string().optional(),
}).superRefine((values, ctx) => {
  if (values.cnpjCpf && !isValidDocumento(values.personType, values.cnpjCpf)) {
    ctx.addIssue({
      code: 'custom',
      path: ['cnpjCpf'],
      message: `${labelDocumento(values.personType)} inválido`,
    })
  }
})
type FormValues = z.infer<typeof schema>

const TIPOS: { value: TipoPessoa; label: string }[] = [
  { value: 'PF', label: 'Pessoa Física' },
  { value: 'PJ', label: 'Pessoa Jurídica' },
]

interface Props {
  defaultValues?: Partial<FormValues>
  onSubmit: (data: CreateClienteRequest) => Promise<void>
  onCancel: () => void
}

export default function ClienteForm({ defaultValues, onSubmit, onCancel }: Props) {
  const { register, control, handleSubmit, getValues, setValue, clearErrors,
    formState: { errors, isSubmitting } } =
    useForm<FormValues>({
      resolver: zodResolver(schema),
      defaultValues: { personType: 'PF', cnpjCpf: '', ...defaultValues },
    })

  const personType = useWatch({ control, name: 'personType' })

  function trocarTipo(tipo: TipoPessoa) {
    if (tipo === personType) return
    setValue('personType', tipo)
    setValue('cnpjCpf', maskDocumento(tipo, getValues('cnpjCpf') ?? ''))
    clearErrors('cnpjCpf')
  }

  async function submit(values: FormValues) {
    const documento = values.cnpjCpf
      ? normalizeDocumento(values.personType, values.cnpjCpf)
      : ''
    await onSubmit({
      name: values.name,
      whatsApp: values.whatsApp,
      email: values.email,
      notes: values.notes,
      personType: documento ? values.personType : null,
      cnpjCpf: documento || null,
    })
  }

  return (
    <form onSubmit={handleSubmit(submit)} className="space-y-4">
      <div className="grid gap-2">
        <Label>Nome</Label>
        <Input {...register('name')} placeholder="Nome completo" />
        {errors.name && <p className="text-xs text-destructive">{errors.name.message}</p>}
      </div>
      <div className="grid gap-2">
        <Label>WhatsApp</Label>
        <Input {...register('whatsApp')} placeholder="11999990000" />
        {errors.whatsApp && <p className="text-xs text-destructive">{errors.whatsApp.message}</p>}
      </div>
      <div className="grid gap-2">
        <Label>Tipo de pessoa</Label>
        <div className="inline-flex rounded-md border p-0.5" role="group">
          {TIPOS.map(t => (
            <button
              key={t.value}
              type="button"
              aria-pressed={personType === t.value}
              onClick={() => trocarTipo(t.value)}
              className={cn(
                'rounded px-3 py-1.5 text-sm transition-colors',
                personType === t.value
                  ? 'bg-primary text-primary-foreground'
                  : 'text-muted-foreground hover:bg-muted',
              )}
            >
              {t.label}
            </button>
          ))}
        </div>
      </div>
      <div className="grid gap-2">
        <Label>{labelDocumento(personType)} (opcional)</Label>
        <Controller
          name="cnpjCpf"
          control={control}
          render={({ field }) => (
            <Input
              {...field}
              value={field.value ?? ''}
              placeholder={placeholderDocumento(personType)}
              onChange={e => field.onChange(maskDocumento(personType, e.target.value))}
            />
          )}
        />
        {errors.cnpjCpf && <p className="text-xs text-destructive">{errors.cnpjCpf.message}</p>}
      </div>
      <div className="grid gap-2">
        <Label>E-mail (opcional)</Label>
        <Input {...register('email')} type="email" placeholder="email@exemplo.com" />
        {errors.email && <p className="text-xs text-destructive">{errors.email.message}</p>}
      </div>
      <div className="grid gap-2">
        <Label>Observações</Label>
        <Input {...register('notes')} placeholder="Anotações rápidas" />
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
