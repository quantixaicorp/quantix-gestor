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
  name: string
  logoUrl: string | null
  primaryColor: string | null
  description: string | null
}

export interface PublicServicoResponse {
  id: string
  name: string
  price: number
  durationMinutes: number | null
}

export interface PublicProfissionalResponse {
  id: string
  name: string
}

export interface PublicAgendamentoConfirmado {
  id: string
  servicoNome: string
  professionalName: string
  startAt: string
  endAt: string
  depositPixQrCode?: string
  sinalValor?: number
}

export interface PublicCriarAgendamentoRequest {
  serviceId: string
  professionalId: string
  startAt: string
  customerName: string
  customerPhone: string
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
