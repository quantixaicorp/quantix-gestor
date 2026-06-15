import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import type { CreateFornecedorRequest } from '@/types/fornecedores'

const schema = z.object({
  nome: z.string().min(1, 'Nome obrigatório').max(200),
  razaoSocial: z.string().max(200).optional().or(z.literal('')),
  nomeFantasia: z.string().max(200).optional().or(z.literal('')),
  cnpjCpf: z.string()
    .refine(v => !v || /^\d{11}$|^\d{14}$/.test(v), 'Informe 11 dígitos (CPF) ou 14 dígitos (CNPJ)')
    .optional()
    .or(z.literal('')),
  inscricaoEstadual: z.string().max(30).optional().or(z.literal('')),
  telefone: z.string().max(20).optional().or(z.literal('')),
  whatsapp: z.string().max(20).optional().or(z.literal('')),
  email: z.string().email('E-mail inválido').optional().or(z.literal('')),
  contato: z.string().max(200).optional().or(z.literal('')),
  logradouro: z.string().max(300).optional().or(z.literal('')),
  cidade: z.string().max(100).optional().or(z.literal('')),
  uf: z.string().max(2).optional().or(z.literal('')),
  cep: z.string().max(9).optional().or(z.literal('')),
  observacoes: z.string().optional().or(z.literal('')),
})
type FormValues = z.infer<typeof schema>

interface Props {
  defaultValues?: Partial<FormValues>
  onSubmit: (data: CreateFornecedorRequest) => Promise<void>
  onCancel: () => void
}

export default function FornecedorForm({ defaultValues, onSubmit, onCancel }: Props) {
  const { register, handleSubmit, formState: { errors, isSubmitting } } =
    useForm<FormValues>({ resolver: zodResolver(schema), defaultValues })

  return (
    <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
      <div className="grid gap-2">
        <Label>Nome *</Label>
        <Input {...register('nome')} placeholder="Nome de exibição" />
        {errors.nome && <p className="text-xs text-destructive">{errors.nome.message}</p>}
      </div>

      <div className="grid grid-cols-2 gap-3">
        <div className="grid gap-2">
          <Label>Razão Social</Label>
          <Input {...register('razaoSocial')} placeholder="Razão social" />
        </div>
        <div className="grid gap-2">
          <Label>Nome Fantasia</Label>
          <Input {...register('nomeFantasia')} placeholder="Nome fantasia" />
        </div>
      </div>

      <div className="grid grid-cols-2 gap-3">
        <div className="grid gap-2">
          <Label>CNPJ / CPF</Label>
          <Input {...register('cnpjCpf')} placeholder="Apenas números" />
          {errors.cnpjCpf && <p className="text-xs text-destructive">{errors.cnpjCpf.message}</p>}
        </div>
        <div className="grid gap-2">
          <Label>Inscrição Estadual</Label>
          <Input {...register('inscricaoEstadual')} placeholder="IE" />
        </div>
      </div>

      <div className="grid grid-cols-2 gap-3">
        <div className="grid gap-2">
          <Label>Telefone</Label>
          <Input {...register('telefone')} placeholder="11999990000" />
        </div>
        <div className="grid gap-2">
          <Label>WhatsApp</Label>
          <Input {...register('whatsapp')} placeholder="11999990000" />
        </div>
      </div>

      <div className="grid grid-cols-2 gap-3">
        <div className="grid gap-2">
          <Label>E-mail</Label>
          <Input {...register('email')} type="email" placeholder="contato@empresa.com" />
          {errors.email && <p className="text-xs text-destructive">{errors.email.message}</p>}
        </div>
        <div className="grid gap-2">
          <Label>Contato</Label>
          <Input {...register('contato')} placeholder="Nome do responsável" />
        </div>
      </div>

      <div className="border-t pt-3">
        <p className="text-xs font-medium text-muted-foreground mb-2 uppercase tracking-wide">Endereço</p>
        <div className="grid gap-3">
          <div className="grid gap-2">
            <Label>Logradouro</Label>
            <Input {...register('logradouro')} placeholder="Rua, número, complemento" />
          </div>
          <div className="grid grid-cols-3 gap-3">
            <div className="col-span-2 grid gap-2">
              <Label>Cidade</Label>
              <Input {...register('cidade')} placeholder="São Paulo" />
            </div>
            <div className="grid gap-2">
              <Label>UF</Label>
              <Input {...register('uf')} placeholder="SP" maxLength={2} />
            </div>
          </div>
          <div className="grid gap-2 max-w-[160px]">
            <Label>CEP</Label>
            <Input {...register('cep')} placeholder="01310-100" />
          </div>
        </div>
      </div>

      <div className="grid gap-2">
        <Label>Observações</Label>
        <Input {...register('observacoes')} placeholder="Anotações internas" />
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
