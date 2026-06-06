import { useEffect, useState } from 'react'
import { Plus, Trash2 } from 'lucide-react'
import { useFinanceiro } from '@/hooks/useFinanceiro'
import { useAuth } from '@/contexts/AuthContext'
import { Button } from '@/components/ui/button'
import { Badge } from '@/components/ui/badge'
import { Dialog, DialogContent, DialogHeader, DialogTitle } from '@/components/ui/dialog'
import LancamentoForm from '@/components/financeiro/LancamentoForm'
import { useConfirm } from '@/hooks/useConfirm'
import { toast } from '@/hooks/useToast'
import type { CreateLancamentoRequest, LancamentoResponse } from '@/types/financeiro'

const tipoVariant = (tipo: string) => tipo === 'Receita' ? 'secondary' : 'destructive'
const statusVariant = (s: string, vencido: boolean) =>
  s === 'Pago' ? 'secondary' :
  s === 'Cancelado' ? 'outline' :
  vencido ? 'destructive' : 'outline'

const fmt = (v: number) => v.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' })
const fmtDate = (d: string) => new Date(d).toLocaleDateString('pt-BR')

export default function Lancamentos() {
  const { lancamentos, loading, list, create, pagar, remove } = useFinanceiro()
  const { isAdmin } = useAuth()
  const [modalAberto, setModalAberto] = useState(false)
  const [pagando, setPagando] = useState<string | null>(null)
  const [excluindo, setExcluindo] = useState<string | null>(null)
  const { confirm, ConfirmDialogNode } = useConfirm()

  useEffect(() => { void list() }, [list])

  async function handleCreate(data: CreateLancamentoRequest) {
    await create(data)
    setModalAberto(false)
  }

  async function handleExcluir(l: LancamentoResponse) {
    const ok = await confirm({
      title: 'Excluir lançamento?',
      description: `Esta ação não pode ser desfeita. ${l.descricao} — ${fmt(l.valor)}`,
    })
    if (!ok) return
    setExcluindo(l.id)
    try {
      await remove(l.id)
      toast.success('Lançamento excluído')
    } catch (e) {
      toast.error(e instanceof Error ? e.message : 'Erro ao excluir')
    } finally { setExcluindo(null) }
  }

  async function handlePagar(l: LancamentoResponse) {
    const ok = await confirm({
      title: 'Confirmar pagamento?',
      description: `${fmt(l.valor)} — ${l.descricao}`,
    })
    if (!ok) return
    setPagando(l.id)
    try {
      await pagar(l.id, { dataPagamento: new Date().toISOString() })
    } finally { setPagando(null) }
  }

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between">
        <h1 className="text-2xl font-bold">Lançamentos</h1>
        <Button onClick={() => setModalAberto(true)}>
          <Plus size={16} className="mr-2" /> Novo Lançamento
        </Button>
      </div>

      {loading ? <p className="text-muted-foreground">Carregando...</p> : (
        <div className="rounded-md border">
          <table className="w-full text-sm">
            <thead>
              <tr className="border-b bg-muted/50">
                <th className="px-4 py-3 text-left font-medium">Descrição</th>
                <th className="px-4 py-3 text-left font-medium">Tipo</th>
                <th className="px-4 py-3 text-left font-medium">Categoria</th>
                <th className="px-4 py-3 text-left font-medium">Vencimento</th>
                <th className="px-4 py-3 text-right font-medium">Valor</th>
                <th className="px-4 py-3 text-left font-medium">Status</th>
                <th className="px-4 py-3" />
              </tr>
            </thead>
            <tbody>
              {lancamentos.map(l => (
                <tr key={l.id} className="border-b">
                  <td className="px-4 py-3 font-medium">{l.descricao}</td>
                  <td className="px-4 py-3">
                    <Badge variant={tipoVariant(l.tipo)}>{l.tipo}</Badge>
                  </td>
                  <td className="px-4 py-3 text-muted-foreground">{l.categoria}</td>
                  <td className="px-4 py-3 text-muted-foreground">{fmtDate(l.dataVencimento)}</td>
                  <td className="px-4 py-3 text-right font-medium">{fmt(l.valor)}</td>
                  <td className="px-4 py-3">
                    <Badge variant={statusVariant(l.status, l.vencido)}>
                      {l.vencido && l.status === 'Pendente' ? 'Vencida' : l.status}
                    </Badge>
                  </td>
                  <td className="px-4 py-3">
                    <div className="flex items-center gap-2">
                      {l.status === 'Pendente' && (
                        <Button size="sm" variant="outline"
                          disabled={pagando === l.id}
                          onClick={() => handlePagar(l)}>
                          {pagando === l.id ? '...' : l.tipo === 'Receita' ? 'Receber' : 'Pagar'}
                        </Button>
                      )}
                      {isAdmin && !l.vendaId && (
                        <Button size="sm" variant="ghost"
                          disabled={excluindo === l.id}
                          onClick={() => handleExcluir(l)}
                          className="text-destructive hover:text-destructive hover:bg-destructive/10">
                          <Trash2 size={14} />
                        </Button>
                      )}
                    </div>
                  </td>
                </tr>
              ))}
              {lancamentos.length === 0 && (
                <tr>
                  <td colSpan={7} className="px-4 py-8 text-center text-muted-foreground">
                    Nenhum lançamento encontrado
                  </td>
                </tr>
              )}
            </tbody>
          </table>
        </div>
      )}

      <Dialog open={modalAberto} onOpenChange={setModalAberto}>
        <DialogContent className="max-w-lg">
          <DialogHeader><DialogTitle>Novo Lançamento</DialogTitle></DialogHeader>
          <LancamentoForm onSubmit={handleCreate} onCancel={() => setModalAberto(false)} />
        </DialogContent>
      </Dialog>
      {ConfirmDialogNode}
    </div>
  )
}
