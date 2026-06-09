import { useEffect, useState } from 'react'
import { api } from '@/services/api'
import type { AutomacaoLogItem } from '@/types/automacao'
import { EVENTO_LABEL } from '@/types/automacao'
import { Badge } from '@/components/ui/badge'

export default function LogAutomacao() {
  const [logs, setLogs] = useState<AutomacaoLogItem[]>([])
  const [apenasErros, setApenasErros] = useState(false)
  const [loading, setLoading] = useState(false)

  useEffect(() => {
    setLoading(true)
    const qs = apenasErros ? '?apenasErros=true' : ''
    api.get<AutomacaoLogItem[]>(`/api/automacao/log${qs}`)
      .then(setLogs)
      .catch(() => {})
      .finally(() => setLoading(false))
  }, [apenasErros])

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between">
        <h1 className="text-2xl font-bold">Log de Automações</h1>
        <div className="flex items-center gap-2 text-sm">
          <input
            type="checkbox"
            id="apenasErros"
            checked={apenasErros}
            onChange={e => setApenasErros(e.target.checked)}
          />
          <label htmlFor="apenasErros">Apenas falhas</label>
        </div>
      </div>

      {loading && <p className="text-muted-foreground text-sm">Carregando...</p>}

      {!loading && logs.length === 0 && (
        <p className="text-muted-foreground text-sm">Nenhum registro encontrado.</p>
      )}

      {!loading && logs.length > 0 && (
        <div className="rounded-md border overflow-x-auto">
          <table className="w-full text-sm">
            <thead>
              <tr className="border-b bg-muted/50">
                <th className="p-3 text-left font-medium">Data/hora</th>
                <th className="p-3 text-left font-medium">Cliente</th>
                <th className="p-3 text-left font-medium">Referência</th>
                <th className="p-3 text-left font-medium">Evento</th>
                <th className="p-3 text-left font-medium">Status</th>
                <th className="p-3 text-left font-medium">Erro</th>
              </tr>
            </thead>
            <tbody>
              {logs.map(log => (
                <tr key={log.id} className="border-b last:border-0 hover:bg-muted/30">
                  <td className="p-3 text-muted-foreground whitespace-nowrap">
                    {new Date(log.enviadoEm).toLocaleString('pt-BR')}
                  </td>
                  <td className="p-3">{log.clienteNome}</td>
                  <td className="p-3">{log.referencia}</td>
                  <td className="p-3">{EVENTO_LABEL[log.tipoEvento]}</td>
                  <td className="p-3">
                    {log.sucesso
                      ? <Badge variant="secondary" className="bg-green-100 text-green-700">Enviado</Badge>
                      : <Badge variant="destructive">Falha</Badge>
                    }
                  </td>
                  <td className="p-3 text-muted-foreground text-xs max-w-[200px] truncate">
                    {log.erroMsg ?? '—'}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </div>
  )
}
