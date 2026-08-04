export type AgendamentoStatus = 'Agendado' | 'Confirmado' | 'Concluido' | 'Cancelado' | 'AguardandoConfirmacao'

export interface AgendamentoListItem {
  id: string
  professionalId: string
  professionalName: string
  customerName: string
  servicoNome: string
  startAt: string
  endAt: string
  status: AgendamentoStatus
}

export interface AgendamentoResponse {
  id: string
  professionalName: string
  customerName: string
  customerPhone: string
  customerId: string | null
  servicoNome: string
  durationMinutes: number
  startAt: string
  endAt: string
  status: AgendamentoStatus
  notes: string | null
  saleId: string | null
  createdAt: string
}

export interface CriarAgendamentoRequest {
  professionalId: string
  customerName: string
  customerPhone: string
  customerId?: string
  serviceId: string
  startAt: string
  notes?: string
}

export interface ConcluirResponse {
  saleId: string
}

export interface ProfissionalResponse {
  id: string
  name: string
  phone: string | null
  isActive: boolean
}

export type TipoPeriodo = 'semana' | 'mes' | 'trimestre' | 'semestre' | 'ano'

export interface DisponibilidadeItem {
  diaSemana: number
  horaInicio: string
  horaFim: string
}

export interface DisponibilidadePeriodoResponse {
  startDate: string   // YYYY-MM-DD
  endDate: string      // YYYY-MM-DD
  faixas: DisponibilidadeItem[]
}

export interface SalvarDisponibilidadeRequest {
  startDate: string
  endDate: string
  faixas: DisponibilidadeItem[]
}

export interface BloqueioResponse {
  id: string
  professionalId: string | null
  professionalName: string | null
  startDate: string
  endDate: string
  reason: string | null
}
