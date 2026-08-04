export interface ClienteResponse {
  id: string
  name: string
  whatsApp: string
  email: string | null
  notes: string | null
  createdAt: string
}

export interface CreateClienteRequest {
  name: string
  whatsApp: string
  email?: string
  notes?: string
}

export interface UpdateClienteRequest {
  name: string
  whatsApp: string
  email?: string
  notes?: string
}
