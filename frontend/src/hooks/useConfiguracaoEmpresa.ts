import { useState, useCallback } from 'react'
import { api } from '@/services/api'
import type { ConfiguracaoEmpresaResponse, AtualizarConfiguracaoEmpresaRequest } from '@/types/fiscal'

export function useConfiguracaoEmpresa() {
  const [config, setConfig] = useState<ConfiguracaoEmpresaResponse | null>(null)
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)

  const obter = useCallback(async () => {
    setLoading(true)
    try {
      const data = await api.get<ConfiguracaoEmpresaResponse>('/api/configuracao-empresa')
      setConfig(data)
      return data
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Erro ao carregar configuração')
    } finally {
      setLoading(false)
    }
  }, [])

  const atualizar = useCallback(async (req: AtualizarConfiguracaoEmpresaRequest) => {
    const result = await api.put<ConfiguracaoEmpresaResponse>('/api/configuracao-empresa', req)
    setConfig(result)
    return result
  }, [])

  return { config, loading, error, obter, atualizar }
}
