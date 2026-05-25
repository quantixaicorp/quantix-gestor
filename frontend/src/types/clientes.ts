export interface ClienteResponse {
  id: string
  nome: string
  whatsapp: string
  email: string | null
  observacoes: string | null
  dataCadastro: string
}

export interface CreateClienteRequest {
  nome: string
  whatsapp: string
  email?: string
  observacoes?: string
}

export interface UpdateClienteRequest {
  nome: string
  whatsapp: string
  email?: string
  observacoes?: string
}
