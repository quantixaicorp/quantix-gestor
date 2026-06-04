export interface FornecedorResponse {
  id: string
  nome: string
  cnpjCpf: string | null
  telefone: string | null
  email: string | null
  logradouro: string | null
  cidade: string | null
  uf: string | null
  cep: string | null
  contato: string | null
  observacoes: string | null
  dataCadastro: string
}

export interface CreateFornecedorRequest {
  nome: string
  cnpjCpf?: string
  telefone?: string
  email?: string
  logradouro?: string
  cidade?: string
  uf?: string
  cep?: string
  contato?: string
  observacoes?: string
}

export interface UpdateFornecedorRequest {
  nome: string
  cnpjCpf?: string
  telefone?: string
  email?: string
  logradouro?: string
  cidade?: string
  uf?: string
  cep?: string
  contato?: string
  observacoes?: string
}
