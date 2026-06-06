export type AgendamentoStatus = 'Agendado' | 'Confirmado' | 'Concluido' | 'Cancelado' | 'AguardandoConfirmacao'

export interface AgendamentoListItem {
  id: string
  profissionalId: string
  profissionalNome: string
  clienteNome: string
  servicoNome: string
  dataHoraInicio: string
  dataHoraFim: string
  status: AgendamentoStatus
}

export interface AgendamentoResponse {
  id: string
  profissionalNome: string
  clienteNome: string
  clienteTelefone: string
  clienteId: string | null
  servicoNome: string
  duracaoMinutos: number
  dataHoraInicio: string
  dataHoraFim: string
  status: AgendamentoStatus
  observacao: string | null
  vendaId: string | null
  criadoEm: string
}

export interface CriarAgendamentoRequest {
  profissionalId: string
  clienteNome: string
  clienteTelefone: string
  clienteId?: string
  servicoId: string
  dataHoraInicio: string
  observacao?: string
}

export interface ConcluirResponse {
  vendaId: string
}

export interface ProfissionalResponse {
  id: string
  nome: string
  telefone: string | null
  ativo: boolean
}

export type TipoPeriodo = 'semana' | 'mes' | 'trimestre' | 'semestre' | 'ano'

export interface DisponibilidadeItem {
  diaSemana: number
  horaInicio: string
  horaFim: string
}

export interface DisponibilidadePeriodoResponse {
  dataInicio: string   // YYYY-MM-DD
  dataFim: string      // YYYY-MM-DD
  faixas: DisponibilidadeItem[]
}

export interface SalvarDisponibilidadeRequest {
  dataInicio: string
  dataFim: string
  faixas: DisponibilidadeItem[]
}

export interface BloqueioResponse {
  id: string
  profissionalId: string | null
  profissionalNome: string | null
  dataInicio: string
  dataFim: string
  motivo: string | null
}
