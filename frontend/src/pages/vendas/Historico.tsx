import { useEffect, useState } from 'react'
import { useVendas } from '@/hooks/useVendas'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'

const statusVariant = (s: string): 'secondary' | 'destructive' | 'outline' =>
  s === 'Concluida' ? 'secondary' :
  s === 'Cancelada' ? 'destructive' : 'outline'

export default function Historico() {
  const { vendas, loading, list, cancelar } = useVendas()
  const [de, setDe] = useState('')
  const [ate, setAte] = useState('')
  const [status, setStatus] = useState('')
  const [cancelando, setCancelando] = useState<string | null>(null)

  useEffect(() => { list() }, [list])

  function buscar() { list({ de: de || undefined, ate: ate || undefined, status: status || undefined }) }

  async function handleCancelar(id: string) {
    if (!confirm('Cancelar esta venda? O estoque será estornado.')) return
    setCancelando(id)
    try { await cancelar(id) } finally { setCancelando(null) }
  }

  const fmt = (v: number) => v.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' })

  return (
    <div className="space-y-4">
      <h1 className="text-2xl font-bold">Histórico de Vendas</h1>

      <div className="flex gap-3 flex-wrap items-end">
        <div className="grid gap-1">
          <label className="text-xs text-muted-foreground">De</label>
          <Input type="date" value={de} onChange={e => setDe(e.target.value)} className="h-8 w-36" />
        </div>
        <div className="grid gap-1">
          <label className="text-xs text-muted-foreground">Até</label>
          <Input type="date" value={ate} onChange={e => setAte(e.target.value)} className="h-8 w-36" />
        </div>
        <select value={status} onChange={e => setStatus(e.target.value)}
          className="h-8 rounded-md border border-input bg-transparent px-3 text-sm">
          <option value="">Todos os status</option>
          <option value="Concluida">Concluída</option>
          <option value="Cancelada">Cancelada</option>
        </select>
        <Button size="sm" onClick={buscar}>Filtrar</Button>
      </div>

      {loading ? (
        <p className="text-muted-foreground">Carregando...</p>
      ) : (
        <div className="rounded-md border">
          <table className="w-full text-sm">
            <thead>
              <tr className="border-b bg-muted/50">
                <th className="px-4 py-3 text-left font-medium">Data</th>
                <th className="px-4 py-3 text-left font-medium">Cliente</th>
                <th className="px-4 py-3 text-left font-medium">Pagamento</th>
                <th className="px-4 py-3 text-right font-medium">Total</th>
                <th className="px-4 py-3 text-left font-medium">Status</th>
                <th className="px-4 py-3" />
              </tr>
            </thead>
            <tbody>
              {vendas.map(v => (
                <tr key={v.id} className="border-b">
                  <td className="px-4 py-3">{new Date(v.dataHora).toLocaleString('pt-BR')}</td>
                  <td className="px-4 py-3">{v.clienteNome ?? 'Balcão'}</td>
                  <td className="px-4 py-3 text-muted-foreground">{v.formaPagamento}</td>
                  <td className="px-4 py-3 text-right font-medium">{fmt(v.total)}</td>
                  <td className="px-4 py-3">
                    <Badge variant={statusVariant(v.status)}>{v.status}</Badge>
                  </td>
                  <td className="px-4 py-3">
                    {v.status === 'Concluida' && (
                      <Button size="sm" variant="ghost"
                        className="text-destructive hover:text-destructive"
                        disabled={cancelando === v.id}
                        onClick={() => handleCancelar(v.id)}>
                        {cancelando === v.id ? '...' : 'Cancelar'}
                      </Button>
                    )}
                  </td>
                </tr>
              ))}
              {vendas.length === 0 && (
                <tr>
                  <td colSpan={6} className="px-4 py-8 text-center text-muted-foreground">
                    Nenhuma venda encontrada
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
