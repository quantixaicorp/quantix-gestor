import { useState, useEffect, useCallback } from 'react'
import { publicBookingApi } from '@/services/publicBookingApi'
import type {
  PublicEmpresaInfo,
  PublicServicoResponse,
  PublicProfissionalResponse,
  PublicAgendamentoConfirmado,
} from '@/services/publicBookingApi'

export type WizardStep = 'servico' | 'profissional' | 'data' | 'horario' | 'dados' | 'confirmado'

export function usePublicBooking(slug: string) {
  const [step, setStep] = useState<WizardStep>('servico')
  const [info, setInfo] = useState<PublicEmpresaInfo | null>(null)
  const [servicos, setServicos] = useState<PublicServicoResponse[]>([])
  const [profissionais, setProfissionais] = useState<PublicProfissionalResponse[]>([])
  const [diasDisponiveis, setDiasDisponiveis] = useState<number[]>([])
  const [slots, setSlots] = useState<string[]>([])
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [confirmado, setConfirmado] = useState<PublicAgendamentoConfirmado | null>(null)

  const [servicoSelecionado, setServicoSelecionado] = useState<PublicServicoResponse | null>(null)
  const [profissionalSelecionado, setProfissionalSelecionado] = useState<PublicProfissionalResponse | null>(null)
  const [dataSelecionada, setDataSelecionada] = useState<string | null>(null)
  const [slotSelecionado, setSlotSelecionado] = useState<string | null>(null)

  useEffect(() => {
    async function load() {
      setLoading(true)
      try {
        const [infoData, servicosData] = await Promise.all([
          publicBookingApi.getInfo(slug),
          publicBookingApi.getServicos(slug),
        ])
        setInfo(infoData)
        setServicos(servicosData)
      } catch (e) {
        setError(e instanceof Error ? e.message : 'Erro ao carregar página')
      } finally {
        setLoading(false)
      }
    }
    void load()
  }, [slug])

  const selecionarServico = useCallback(async (servico: PublicServicoResponse) => {
    setServicoSelecionado(servico)
    setLoading(true)
    try {
      const data = await publicBookingApi.getProfissionais(slug)
      setProfissionais(data)
      setStep('profissional')
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Erro ao carregar profissionais')
    } finally {
      setLoading(false)
    }
  }, [slug])

  const selecionarProfissional = useCallback(async (prof: PublicProfissionalResponse) => {
    setProfissionalSelecionado(prof)
    setLoading(true)
    try {
      const data = await publicBookingApi.getDisponibilidade(slug, prof.id)
      setDiasDisponiveis(data.diasComDisponibilidade)
      setStep('data')
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Erro ao carregar disponibilidade')
    } finally {
      setLoading(false)
    }
  }, [slug])

  const selecionarData = useCallback(async (data: string) => {
    setDataSelecionada(data)
    if (!profissionalSelecionado || !servicoSelecionado) return
    setLoading(true)
    try {
      const slotsData = await publicBookingApi.getSlots(
        slug, profissionalSelecionado.id, servicoSelecionado.id, data)
      setSlots(slotsData)
      setStep('horario')
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Erro ao carregar horários')
    } finally {
      setLoading(false)
    }
  }, [slug, profissionalSelecionado, servicoSelecionado])

  const selecionarSlot = useCallback((slot: string) => {
    setSlotSelecionado(slot)
    setStep('dados')
  }, [])

  const confirmar = useCallback(async (clienteNome: string, clienteTelefone: string) => {
    if (!servicoSelecionado || !profissionalSelecionado || !slotSelecionado) return
    setLoading(true)
    try {
      const result = await publicBookingApi.criarAgendamento(slug, {
        servicoId: servicoSelecionado.id,
        profissionalId: profissionalSelecionado.id,
        dataHoraInicio: slotSelecionado,
        clienteNome,
        clienteTelefone,
      })
      setConfirmado(result)
      setStep('confirmado')
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Erro ao confirmar agendamento')
    } finally {
      setLoading(false)
    }
  }, [slug, servicoSelecionado, profissionalSelecionado, slotSelecionado])

  const voltar = useCallback(() => {
    const ordem: WizardStep[] = ['servico', 'profissional', 'data', 'horario', 'dados']
    const idx = ordem.indexOf(step as WizardStep)
    if (idx > 0) setStep(ordem[idx - 1])
  }, [step])

  return {
    step, info, servicos, profissionais, diasDisponiveis, slots,
    loading, error, confirmado,
    servicoSelecionado, profissionalSelecionado, dataSelecionada, slotSelecionado,
    selecionarServico, selecionarProfissional, selecionarData, selecionarSlot, confirmar, voltar,
  }
}
