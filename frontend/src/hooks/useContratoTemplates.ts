import { useState, useEffect, useCallback } from 'react'
import { api } from '@/services/api'
import type { ContratoTemplateListItem, CreateContratoTemplateRequest, ContratoTemplate } from '@/types/contrato-template'

export function useContratoTemplates() {
  const [templates, setTemplates] = useState<ContratoTemplateListItem[]>([])
  const [loading, setLoading] = useState(true)

  const fetchTemplates = useCallback(async () => {
    setLoading(true)
    try {
      const data = await api.get<ContratoTemplateListItem[]>('/api/contrato-templates')
      setTemplates(data)
    } catch {
      setTemplates([])
    } finally {
      setLoading(false)
    }
  }, [])

  useEffect(() => { void fetchTemplates() }, [fetchTemplates])

  async function create(req: CreateContratoTemplateRequest) {
    const created = await api.post<ContratoTemplate>('/api/contrato-templates', req)
    setTemplates(prev => [...prev, {
      id: created.id, nome: created.nome,
      tipoCobranca: created.tipoCobranca, periodicidade: created.periodicidade,
      valorPadrao: created.valorPadrao, qtdItens: created.itens.length,
    }])
    return created
  }

  async function remove(id: string) {
    await api.delete(`/api/contrato-templates/${id}`)
    setTemplates(prev => prev.filter(t => t.id !== id))
  }

  async function getById(id: string) {
    return api.get<ContratoTemplate>(`/api/contrato-templates/${id}`)
  }

  return { templates, loading, create, remove, getById, refetch: fetchTemplates }
}
