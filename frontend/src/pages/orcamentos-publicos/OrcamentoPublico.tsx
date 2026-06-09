import { useEffect, useState } from 'react'
import { useParams } from 'react-router-dom'
import { Button } from '@/components/ui/button'
import type { OrcamentoPublico } from '@/types/orcamento-publico'

const API_BASE = import.meta.env.VITE_API_URL ?? 'http://localhost:5002'

export default function OrcamentoPublicoPage() {
  const { token } = useParams<{ token: string }>()
  const [orc, setOrc] = useState<OrcamentoPublico | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [acao, setAcao] = useState<'aprovar' | 'rejeitar' | null>(null)
  const [done, setDone] = useState<string | null>(null)

  useEffect(() => {
    fetch(`${API_BASE}/api/public/orcamentos/${token}`)
      .then(r => r.ok ? r.json() : r.json().then((b: { error?: string }) => Promise.reject(b?.error ?? 'Erro')))
      .then((data: OrcamentoPublico) => setOrc(data))
      .catch((e: unknown) => setError(String(e)))
      .finally(() => setLoading(false))
  }, [token])

  async function handleAcao(tipo: 'aprovar' | 'rejeitar') {
    setAcao(tipo)
    try {
      const res = await fetch(`${API_BASE}/api/public/orcamentos/${token}/${tipo}`, {
        method: 'POST',
      })
      const data = (await res.json()) as OrcamentoPublico
      if (!res.ok) throw new Error((data as unknown as { error?: string })?.error ?? 'Erro')
      setOrc(data)
      setDone(tipo === 'aprovar' ? 'Orçamento aprovado com sucesso!' : 'Orçamento rejeitado.')
    } catch (e) {
      setError(String(e))
    } finally {
      setAcao(null)
    }
  }

  if (loading) return <div className="flex items-center justify-center min-h-screen text-sm text-muted-foreground">Carregando...</div>
  if (error) return <div className="flex items-center justify-center min-h-screen text-sm text-destructive">{error}</div>
  if (!orc) return null

  const expirado = new Date(orc.dataValidade) < new Date()
  const podeAprovar = orc.status === 'Enviado' && !expirado
  const statusColor: Record<string, string> = {
    Rascunho: 'bg-gray-100 text-gray-700',
    Enviado: 'bg-blue-100 text-blue-700',
    Aprovado: 'bg-green-100 text-green-700',
    Rejeitado: 'bg-red-100 text-red-700',
    Expirado: 'bg-orange-100 text-orange-700',
    Convertido: 'bg-purple-100 text-purple-700',
    Cancelado: 'bg-gray-100 text-gray-500',
  }

  return (
    <div className="min-h-screen bg-gray-50 py-10 px-4">
      <div className="max-w-2xl mx-auto space-y-6">
        <div className="bg-white rounded-xl shadow-sm border p-6 space-y-4">
          <div className="flex items-center justify-between">
            <h1 className="text-xl font-bold">{orc.titulo}</h1>
            <span className={`text-xs font-medium px-2.5 py-1 rounded-full ${statusColor[orc.status] ?? 'bg-gray-100'}`}>
              {orc.status}
            </span>
          </div>
          {orc.clienteNome && <p className="text-sm text-muted-foreground">Cliente: {orc.clienteNome}</p>}
          <p className="text-sm text-muted-foreground">
            Válido até: {new Date(orc.dataValidade).toLocaleDateString('pt-BR')}
            {expirado && <span className="ml-2 text-orange-600 font-medium">(expirado)</span>}
          </p>

          <table className="w-full text-sm border-collapse">
            <thead>
              <tr className="border-b text-left">
                <th className="py-2 font-medium">Descrição</th>
                <th className="py-2 font-medium text-right">Qtd</th>
                <th className="py-2 font-medium text-right">Unit.</th>
                <th className="py-2 font-medium text-right">Total</th>
              </tr>
            </thead>
            <tbody>
              {orc.itens.map((item, i) => (
                <tr key={i} className="border-b last:border-0">
                  <td className="py-2">{item.descricao}</td>
                  <td className="py-2 text-right">{item.quantidade}</td>
                  <td className="py-2 text-right">R$ {item.valorUnitario.toFixed(2)}</td>
                  <td className="py-2 text-right">R$ {item.total.toFixed(2)}</td>
                </tr>
              ))}
            </tbody>
          </table>
          <div className="flex justify-end font-bold text-base pt-2 border-t">
            Total: R$ {orc.total.toFixed(2)}
          </div>

          {orc.observacao && (
            <p className="text-sm text-muted-foreground bg-muted/40 rounded p-3">{orc.observacao}</p>
          )}
        </div>

        {done && (
          <div className="bg-green-50 border border-green-200 rounded-xl p-4 text-center text-green-800 font-medium">
            {done}
          </div>
        )}

        {podeAprovar && !done && (
          <div className="flex gap-3">
            <Button
              className="flex-1 bg-green-600 hover:bg-green-700"
              onClick={() => void handleAcao('aprovar')}
              disabled={acao !== null}
            >
              {acao === 'aprovar' ? 'Aprovando...' : 'Aprovar Orçamento'}
            </Button>
            <Button
              variant="outline"
              className="flex-1 border-red-300 text-red-600 hover:bg-red-50"
              onClick={() => void handleAcao('rejeitar')}
              disabled={acao !== null}
            >
              {acao === 'rejeitar' ? 'Rejeitando...' : 'Rejeitar'}
            </Button>
          </div>
        )}

        <p className="text-center text-xs text-muted-foreground">Powered by GestorAI</p>
      </div>
    </div>
  )
}
