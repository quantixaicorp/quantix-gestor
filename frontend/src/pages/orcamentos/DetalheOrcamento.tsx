// frontend/src/pages/orcamentos/DetalheOrcamento.tsx
import { useEffect, useState } from 'react'
import { useParams, useNavigate } from 'react-router-dom'
import { FileText, MessageCircle } from 'lucide-react'
import { useOrcamentos } from '@/hooks/useOrcamentos'
import { api } from '@/services/api'
import { Button } from '@/components/ui/button'
import { Badge } from '@/components/ui/badge'
import { useConfirm } from '@/hooks/useConfirm'
import { toast } from '@/hooks/useToast'
import type { OrcamentoStatus } from '@/types/orcamento'

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

export default function DetalheOrcamento() {
  const { id } = useParams<{ id: string }>()
  const navigate = useNavigate()
  const { orcamento, loading, error, get, enviar, aprovar, rejeitar, cancelar, converter } = useOrcamentos()
  const [acao, setAcao] = useState<string | null>(null)
  const { confirm, ConfirmDialogNode } = useConfirm()

  useEffect(() => {
    if (id) void get(id)
  }, [get, id])

  async function executar(fn: () => Promise<unknown>, label: string) {
    const ok = await confirm({ title: `Confirmar: ${label}?` })
    if (!ok) return
    setAcao(label)
    try { await fn() } catch (e) { toast.error(e instanceof Error ? e.message : 'Erro') }
    finally { setAcao(null) }
  }

  async function abrirPdf() {
    try {
      const html = await api.getText(`/api/orcamentos/${id}/pdf`)
      const w = window.open('', '_blank')
      if (!w) return
      w.document.write(html)
      w.document.close()
      w.print()
    } catch (e) {
      toast.error(e instanceof Error ? e.message : 'Erro ao abrir PDF')
    }
  }

  function abrirWhatsapp() {
    if (!orcamento?.clienteWhatsapp) return
    const num = String(orcamento.numero).padStart(3, '0')
    const total = fmt(orcamento.total)
    const validade = fmtDate(orcamento.dataValidade)
    const msg = encodeURIComponent(
      `Olá${orcamento.clienteNome ? ` ${orcamento.clienteNome}` : ''}! ` +
      `Segue o Orçamento ORC-${num}: "${orcamento.titulo}"\n` +
      `Total: ${total} | Válido até: ${validade}`
    )
    const digits = orcamento.clienteWhatsapp.replace(/\D/g, '')
    const phone = digits.startsWith('55') ? digits : `55${digits}`
    window.open(`https://wa.me/${phone}?text=${msg}`, '_blank')
  }

  async function handleConverter() {
    if (!id) return
    const ok = await confirm({
      title: 'Converter em venda?',
      description: 'Uma Venda Aberta será criada.',
    })
    if (!ok) return
    setAcao('converter')
    try {
      const result = await converter(id)
      if (result.vendaId) navigate(`/vendas/nova?vendaId=${result.vendaId}`)
    } catch (e) {
      toast.error(e instanceof Error ? e.message : 'Erro ao converter')
    } finally {
      setAcao(null)
    }
  }

  if (loading) return <p className="text-muted-foreground">Carregando...</p>
  if (error) return <p className="text-destructive">{error}</p>
  if (!orcamento) return null

  const o = orcamento
  const podeWhatsapp = !!o.clienteWhatsapp

  return (
    <div className="max-w-3xl space-y-6">
      {o.status === 'Expirado' && (
        <div className="rounded-md border border-destructive bg-destructive/10 px-4 py-3 text-sm text-destructive">
          Este orçamento expirou em {fmtDate(o.dataValidade)}.
        </div>
      )}

      <div className="flex items-start justify-between">
        <div>
          <p className="text-sm text-muted-foreground font-mono">
            ORC-{String(o.numero).padStart(3, '0')}
          </p>
          <h1 className="text-2xl font-bold">{o.titulo}</h1>
          {o.clienteNome && <p className="text-muted-foreground">{o.clienteNome}</p>}
        </div>
        <Badge className={statusClassName(o.status)}>{o.status}</Badge>
      </div>

      <div className="flex flex-wrap gap-2">
        {o.status === 'Rascunho' && (
          <>
            <Button onClick={() => executar(() => enviar(o.id), 'Enviar orçamento')}
              disabled={acao !== null}>
              {acao === 'Enviar orçamento' ? '...' : 'Enviar'}
            </Button>
            <Button variant="destructive" onClick={() => executar(() => cancelar(o.id), 'Cancelar orçamento')}
              disabled={acao !== null}>
              Cancelar
            </Button>
          </>
        )}
        {o.status === 'Enviado' && (
          <>
            <Button onClick={() => executar(() => aprovar(o.id), 'Aprovar orçamento')}
              disabled={acao !== null}>
              {acao === 'Aprovar orçamento' ? '...' : 'Aprovar'}
            </Button>
            <Button variant="outline" onClick={() => executar(() => rejeitar(o.id), 'Rejeitar orçamento')}
              disabled={acao !== null}>
              Rejeitar
            </Button>
            {podeWhatsapp && (
              <Button variant="outline" onClick={abrirWhatsapp}>
                <MessageCircle size={16} className="mr-2" /> WhatsApp
              </Button>
            )}
          </>
        )}
        {o.status === 'Aprovado' && (
          <Button onClick={handleConverter} disabled={acao !== null}>
            {acao === 'converter' ? '...' : 'Converter em Venda'}
          </Button>
        )}
        {o.status === 'Convertido' && o.vendaId && (
          <Button variant="outline" onClick={() => navigate('/vendas')}>
            Ver histórico de vendas
          </Button>
        )}
        <Button variant="outline" onClick={abrirPdf}>
          <FileText size={16} className="mr-2" /> PDF
        </Button>
      </div>

      <div className="grid gap-1 text-sm text-muted-foreground">
        <p>Válido até: <strong className="text-foreground">{fmtDate(o.dataValidade)}</strong></p>
        {o.observacao && <p>Obs: {o.observacao}</p>}
      </div>

      <div className="rounded-md border">
        <table className="w-full text-sm">
          <thead>
            <tr className="border-b bg-muted/50">
              <th className="px-4 py-3 text-left font-medium">Tipo</th>
              <th className="px-4 py-3 text-left font-medium">Descrição</th>
              <th className="px-4 py-3 text-right font-medium">Qtd</th>
              <th className="px-4 py-3 text-right font-medium">Unitário</th>
              <th className="px-4 py-3 text-right font-medium">Total</th>
            </tr>
          </thead>
          <tbody>
            {o.itens.map(item => (
              <tr key={item.id} className="border-b">
                <td className="px-4 py-3">
                  <span className="rounded bg-muted px-2 py-0.5 text-xs">{item.tipo}</span>
                </td>
                <td className="px-4 py-3">{item.descricao}</td>
                <td className="px-4 py-3 text-right">{item.quantidade}</td>
                <td className="px-4 py-3 text-right">{fmt(item.valorUnitario)}</td>
                <td className="px-4 py-3 text-right font-medium">
                  {fmt(item.quantidade * item.valorUnitario)}
                </td>
              </tr>
            ))}
          </tbody>
          <tfoot>
            <tr>
              <td colSpan={4} className="px-4 py-3 text-right font-semibold">Total:</td>
              <td className="px-4 py-3 text-right font-bold text-lg">{fmt(o.total)}</td>
            </tr>
          </tfoot>
        </table>
      </div>

      <Button variant="ghost" onClick={() => navigate('/orcamentos')}>← Voltar</Button>
      {ConfirmDialogNode}
    </div>
  )
}
