const API_BASE = import.meta.env.VITE_API_URL ?? 'http://localhost:5002'

async function publicRequest<T>(path: string, options: RequestInit = {}): Promise<T> {
  const res = await fetch(`${API_BASE}${path}`, {
    ...options,
    headers: { 'Content-Type': 'application/json', ...options.headers },
  })
  if (!res.ok) {
    const err = await res.json().catch(() => ({ error: res.statusText }))
    throw new Error((err as { error?: string }).error ?? res.statusText)
  }
  return res.json()
}

export interface PublicEmpresaInfo {
  nome: string
  logoUrl: string | null
  corPrimaria: string | null
  descricao: string | null
}

export interface PublicServicoResponse {
  id: string
  nome: string
  preco: number
  duracaoMinutos: number | null
}

export interface PublicProfissionalResponse {
  id: string
  nome: string
}

export interface PublicAgendamentoConfirmado {
  id: string
  servicoNome: string
  profissionalNome: string
  dataHoraInicio: string
  dataHoraFim: string
  sinalPixQrCode?: string
  sinalValor?: number
}

export interface PublicCriarAgendamentoRequest {
  servicoId: string
  profissionalId: string
  dataHoraInicio: string
  clienteNome: string
  clienteTelefone: string
}

export const publicBookingApi = {
  getInfo: (slug: string) =>
    publicRequest<PublicEmpresaInfo>(`/public/${slug}/info`),

  getServicos: (slug: string) =>
    publicRequest<PublicServicoResponse[]>(`/public/${slug}/servicos`),

  getProfissionais: (slug: string) =>
    publicRequest<PublicProfissionalResponse[]>(`/public/${slug}/profissionais`),

  getDisponibilidade: (slug: string, profissionalId: string) =>
    publicRequest<{ diasComDisponibilidade: number[] }>(
      `/public/${slug}/disponibilidade?profissionalId=${profissionalId}`),

  getSlots: (slug: string, profissionalId: string, servicoId: string, data: string) =>
    publicRequest<string[]>(
      `/public/${slug}/slots?profissionalId=${profissionalId}&servicoId=${servicoId}&data=${data}`),

  criarAgendamento: (slug: string, req: PublicCriarAgendamentoRequest) =>
    publicRequest<PublicAgendamentoConfirmado>(`/public/${slug}/agendamentos`, {
      method: 'POST',
      body: JSON.stringify(req),
    }),
}
