import { useEffect, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { Plus, Search } from 'lucide-react'
import { useCompras } from '@/hooks/useCompras'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { KpiRow } from '@/components/ui/KpiRow'
import { toast } from '@/hooks/useToast'
import type { CompraResponse } from '@/types/compras'

const STATUS_COLORS: Record<string, string> = {
  Rascunho: 'bg-muted text-muted-foreground',
  Confirmada: 'bg-green-100 text-green-700 dark:bg-green-900/30 dark:text-green-400',
  Cancelada: 'bg-red-100 text-red-700 dark:bg-red-900/30 dark:text-red-400',
}

export default function Compras() {
  const navigate = useNavigate()
  const { compras, resumo, loading, list, getResumo, cancelar, remove } = useCompras()
  const [busca, setBusca] = useState('')

  useEffect(() => {
    void list()
    void getResumo()
  }, [list, getResumo])

  async function handleCancelar(c: CompraResponse) {
    if (!confirm(`Cancelar compra #${c.numero}?`)) return
    try {
      await cancelar(c.id)
      toast.success('Compra cancelada.')
    } catch (e) {
      toast.error(e instanceof Error ? e.message : 'Erro')
    }
  }

  async function handleDelete(c: CompraResponse) {
    if (!confirm(`Excluir rascunho #${c.numero}?`)) return
    try {
      await remove(c.id)
      toast.success('Rascunho excluído.')
    } catch (e) {
      toast.error(e instanceof Error ? e.message : 'Erro')
    }
  }

  const filtradas = compras.filter(c =>
    c.fornecedorNome.toLowerCase().includes(busca.toLowerCase()) ||
    String(c.numero).includes(busca) ||
    (c.numeroNota ?? '').includes(busca)
  )

  return (
    <div className="space-y-4">
      <div className="flex flex-wrap items-center justify-between gap-2">
        <h1 className="text-2xl font-bold">Compras</h1>
        <Button onClick={() => navigate('/compras/nova')}>
          <Plus size={16} className="mr-2" /> Nova Compra
        </Button>
      </div>

      {resumo && (
        <KpiRow items={[
          { label: 'Total Comprado no Mês', value: resumo.totalCompraMes.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' }) },
          { label: 'Qtd de Compras', value: String(resumo.qtdComprasMes) },
          { label: 'Contas a Pagar Geradas', value: resumo.totalContasPagarGeradas.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' }) },
        ]} />
      )}

      <div className="relative max-w-sm">
        <Search size={16} className="absolute left-3 top-1/2 -translate-y-1/2 text-muted-foreground" />
        <Input
          className="pl-9"
          placeholder="Buscar por fornecedor, nº ou nota..."
          value={busca}
          onChange={e => setBusca(e.target.value)}
        />
      </div>

      {loading ? (
        <p className="text-muted-foreground">Carregando...</p>
      ) : filtradas.length === 0 ? (
        <p className="text-center text-muted-foreground py-12">Nenhuma compra registrada</p>
      ) : (
        <div className="overflow-x-auto rounded-md border">
          <table className="w-full text-sm">
            <thead>
              <tr className="border-b bg-muted/50">
                <th className="px-4 py-3 text-left font-medium">Nº</th>
                <th className="px-4 py-3 text-left font-medium">Data</th>
                <th className="px-4 py-3 text-left font-medium">Fornecedor</th>
                <th className="px-4 py-3 text-left font-medium">Tipo</th>
                <th className="px-4 py-3 text-right font-medium">Valor Total</th>
                <th className="px-4 py-3 text-left font-medium">Status</th>
                <th className="px-4 py-3 text-left font-medium w-32">Ações</th>
              </tr>
            </thead>
            <tbody>
              {filtradas.map(c => (
                <tr
                  key={c.id}
                  className="border-b hover:bg-muted/30 cursor-pointer"
                  onClick={() => navigate(`/compras/${c.id}`)}
                >
                  <td className="px-4 py-3 font-medium">#{c.numero}</td>
                  <td className="px-4 py-3 text-muted-foreground">
                    {new Date(c.data).toLocaleDateString('pt-BR')}
                  </td>
                  <td className="px-4 py-3">{c.fornecedorNome}</td>
                  <td className="px-4 py-3 text-muted-foreground">{c.tipoCompra}</td>
                  <td className="px-4 py-3 text-right font-medium">
                    {c.valorTotal.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' })}
                  </td>
                  <td className="px-4 py-3">
                    <span className={`inline-flex items-center rounded-full px-2.5 py-0.5 text-xs font-medium ${STATUS_COLORS[c.status] ?? ''}`}>
                      {c.status}
                    </span>
                  </td>
                  <td className="px-4 py-3" onClick={e => e.stopPropagation()}>
                    <div className="flex gap-1">
                      {c.status === 'Rascunho' && (
                        <Button size="sm" variant="destructive" onClick={() => void handleDelete(c)}>
                          Excluir
                        </Button>
                      )}
                      {c.status === 'Confirmada' && (
                        <Button size="sm" variant="outline" onClick={() => void handleCancelar(c)}>
                          Cancelar
                        </Button>
                      )}
                    </div>
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
