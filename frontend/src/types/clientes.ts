import type { TipoPessoa } from '@/lib/documento'

export interface ClienteResponse {
  id: string
  name: string
  whatsApp: string
  email: string | null
  notes: string | null
  createdAt: string
  personType: TipoPessoa | null
  cnpjCpf: string | null
}

export interface CreateClienteRequest {
  name: string
  whatsApp: string
  email?: string
  notes?: string
  personType?: TipoPessoa | null
  cnpjCpf?: string | null
}

export interface UpdateClienteRequest {
  name: string
  whatsApp: string
  email?: string
  notes?: string
  personType?: TipoPessoa | null
  cnpjCpf?: string | null
}
