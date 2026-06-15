export interface FornecedorResponse {
  id: string
  nome: string
  razaoSocial?: string
  nomeFantasia?: string
  cnpjCpf?: string | null
  inscricaoEstadual?: string
  telefone?: string | null
  whatsapp?: string
  email?: string | null
  logradouro?: string | null
  cidade?: string | null
  uf?: string | null
  cep?: string | null
  contato?: string | null
  observacoes?: string | null
  status: 'Ativo' | 'Inativo'
  dataCadastro: string
}

export interface CreateFornecedorRequest {
  nome: string
  razaoSocial?: string
  nomeFantasia?: string
  cnpjCpf?: string
  inscricaoEstadual?: string
  telefone?: string
  whatsapp?: string
  email?: string
  logradouro?: string
  cidade?: string
  uf?: string
  cep?: string
  contato?: string
  observacoes?: string
}

export interface UpdateFornecedorRequest extends CreateFornecedorRequest {
  status?: string
}
