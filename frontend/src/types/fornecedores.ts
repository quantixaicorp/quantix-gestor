export interface FornecedorResponse {
  id: string
  name: string
  razaoSocial?: string
  nomeFantasia?: string
  cnpjCpf?: string | null
  inscricaoEstadual?: string
  phone?: string | null
  whatsApp?: string
  email?: string | null
  logradouro?: string | null
  city?: string | null
  uf?: string | null
  cep?: string | null
  contactPerson?: string | null
  notes?: string | null
  status: 'Ativo' | 'Inativo'
  createdAt: string
}

export interface CreateFornecedorRequest {
  name: string
  razaoSocial?: string
  nomeFantasia?: string
  cnpjCpf?: string
  inscricaoEstadual?: string
  phone?: string
  whatsApp?: string
  email?: string
  logradouro?: string
  city?: string
  uf?: string
  cep?: string
  contactPerson?: string
  notes?: string
}

export interface UpdateFornecedorRequest extends CreateFornecedorRequest {
  status?: string
}
