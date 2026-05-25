// frontend/src/pages/orcamentos/Orcamentos.tsx
import { useEffect, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { Plus } from 'lucide-react'
import { useOrcamentos } from '@/hooks/useOrcamentos'
import { Button } from '@/components/ui/button'
import { Badge } from '@/components/ui/badge'
import type { OrcamentoStatus } from '@/types/orcamento'

const STATUS_TODOS: (OrcamentoStatus | 'Todos')[] = [
  'Todos', 'Rascunho', 'Enviado', 'Aprovado', 'Convertido', 'Rejeitado', 'Cancelado', 'Expirado',
]

const statusClassName = (s: OrcamentoStatus): string => {
  if (s === 'Rascunho')   return 'bg-gray-100 text-gray-700 border-gray-200'
  if (s === 'Enviado')    return 'bg-blue-100 text-blue-700 border-blue-200'
  if (s === 'Aprovado')   return 'bg-green-100 text-green-700 border-green-200'
  if (s === 'Convertido') return 'bg-purple-100 text-purple-700 border-purple-200'
  if (s === 'Rejeitado')  return 'bg-red-100 text-red-700 border-red-200'
  if (s === 'Cancelado')  return 'bg-rose-100 text-rose-700 border-rose-200'
  if (s === 'Expirado')   return 'bg-orange-100 text-orange-700 border-orange-200'
  return ''
}

const fmt = (v: number) => v.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' })
const fmtDate = (d: string) => new Date(d).toLocaleDateString('pt-BR')

export default function Orcamentos() {
  const navigate = useNavigate()
  const { orcamentos, loading, list } = useOrcamentos()
  const [filtro, setFiltro] = useState<OrcamentoStatus | 'Todos'>('Todos')

  useEffect(() => {
    void list(filtro === 'Todos' ? undefined : filtro)
  }, [list, filtro])

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between">
        <h1 className="text-2xl font-bold">Orçamentos</h1>
        <Button onClick={() => navigate('/orcamentos/novo')}>
          <Plus size={16} className="mr-2" /> Novo Orçamento
        </Button>
      </div>

      <div className="flex flex-wrap gap-2">
        {STATUS_TODOS.map(s => (
          <button
            key={s}
            onClick={() => setFiltro(s)}
            className={`rounded-full px-3 py-1 text-xs font-medium transition-colors ${
              filtro === s
                ? 'bg-primary text-primary-foreground'
                : 'bg-muted text-muted-foreground hover:bg-accent'
            }`}>
            {s}
          </button>
        ))}
      </div>

      {loading ? <p className="text-muted-foreground">Carregando...</p> : (
        <div className="rounded-md border">
          <table className="w-full text-sm">
            <thead>
              <tr className="border-b bg-muted/50">
                <th className="px-4 py-3 text-left font-medium">Nº</th>
                <th className="px-4 py-3 text-left font-medium">Título</th>
                <th className="px-4 py-3 text-left font-medium">Cliente</th>
                <th className="px-4 py-3 text-left font-medium">Validade</th>
                <th className="px-4 py-3 text-right font-medium">Total</th>
                <th className="px-4 py-3 text-left font-medium">Status</th>
                <th className="px-4 py-3" />
              </tr>
            </thead>
            <tbody>
              {orcamentos.map(o => (
                <tr key={o.id} className="border-b hover:bg-muted/30">
                  <td className="px-4 py-3 font-mono text-muted-foreground">
                    ORC-{String(o.numero).padStart(3, '0')}
                  </td>
                  <td className="px-4 py-3 font-medium">{o.titulo}</td>
                  <td className="px-4 py-3 text-muted-foreground">{o.clienteNome ?? '—'}</td>
                  <td className="px-4 py-3 text-muted-foreground">{fmtDate(o.dataValidade)}</td>
                  <td className="px-4 py-3 text-right font-medium">{fmt(o.total)}</td>
                  <td className="px-4 py-3">
                    <Badge className={statusClassName(o.status)}>{o.status}</Badge>
                  </td>
                  <td className="px-4 py-3">
                    <Button size="sm" variant="outline" onClick={() => navigate(`/orcamentos/${o.id}`)}>
                      {o.status === 'Rascunho' ? 'Enviar' : o.status === 'Aprovado' ? 'Converter' : 'Ver'}
                    </Button>
                  </td>
                </tr>
              ))}
              {orcamentos.length === 0 && (
                <tr>
                  <td colSpan={7} className="px-4 py-8 text-center text-muted-foreground">
                    Nenhum orçamento encontrado
                  </td>
                </tr>
              )}
            </tbody>
          </table>
        </div>
      )}
    </div>
  )
}
