import { useEffect, useState } from 'react'
import { useParams, useNavigate } from 'react-router-dom'
import { ChevronLeft, FileDown, Zap } from 'lucide-react'
import { useContratos } from '@/hooks/useContratos'
import { Button } from '@/components/ui/button'
import { cn } from '@/lib/utils'

const STATUS_STYLES: Record<string, string> = {
  Rascunho:  'bg-yellow-100 text-yellow-800 dark:bg-yellow-900/40 dark:text-yellow-200',
  Ativo:     'bg-green-100 text-green-800 dark:bg-green-900/40 dark:text-green-200',
  Encerrado: 'bg-gray-100 text-gray-600 dark:bg-gray-800 dark:text-gray-400',
  Cancelado: 'bg-red-100 text-red-700 dark:bg-red-900/40 dark:text-red-300',
}

export default function DetalheContrato() {
  const { id } = useParams<{ id: string }>()
  const navigate = useNavigate()
  const { contrato, loading, error, get, ativar, encerrar, cancelar, gerarCobrancas, downloadPdf } = useContratos()

  const [modalGerar, setModalGerar] = useState(false)
  const [de, setDe] = useState('')
  const [ate, setAte] = useState('')
  const [gerandoMsg, setGerandoMsg] = useState('')
  const [actionError, setActionError] = useState('')

  useEffect(() => { if (id) void get(id) }, [id, get])

  const fmtVal = (v: number) => v.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' })
  const fmtDate = (s: string) => new Date(s + 'T00:00:00').toLocaleDateString('pt-BR')

  const handleAcao = async (acao: () => Promise<unknown>) => {
    setActionError('')
    try { await acao() } catch (e) { setActionError(e instanceof Error ? e.message : 'Erro') }
  }

  const handleGerarCobrancas = async () => {
    if (!id || !de || !ate) return
    setGerandoMsg('')
    setActionError('')
    try {
      const result = await gerarCobrancas(id, { de, ate })
      setGerandoMsg(result.length === 0
        ? 'Nenhuma cobrança nova (já existem para o período).'
        : `${result.length} cobrança(s) gerada(s) com sucesso.`)
      setModalGerar(false)
    } catch (e) { setActionError(e instanceof Error ? e.message : 'Erro ao gerar cobranças') }
  }

  if (loading && !contrato) return <div className="text-muted-foreground p-8">Carregando...</div>
  if (error) return <div className="text-destructive p-8">{error}</div>
  if (!contrato) return null

  const c = contrato

  return (
    <div className="flex flex-col gap-6 max-w-2xl">
      <div className="flex items-center gap-2">
        <Button variant="ghost" size="icon" onClick={() => navigate('/contratos')}>
          <ChevronLeft className="h-5 w-5" />
        </Button>
        <div className="flex items-center gap-2">
          <h1 className="text-xl font-bold">Contrato {String(c.numero).padStart(3, '0')}</h1>
          <span className={cn('px-2 py-0.5 rounded-full text-xs font-medium', STATUS_STYLES[c.status])}>
            {c.status}
          </span>
        </div>
        <div className="ml-auto flex gap-2">
          <Button variant="outline" size="sm" onClick={() => downloadPdf(c.id)}>
            <FileDown className="h-4 w-4 mr-1" /> PDF
          </Button>
          {c.status === 'Ativo' && (
            <Button size="sm" onClick={() => setModalGerar(true)}>
              <Zap className="h-4 w-4 mr-1" /> Gerar Cobranças
            </Button>
          )}
        </div>
      </div>

      {actionError && (
        <div className="rounded-lg border border-destructive/30 bg-destructive/10 px-4 py-3 text-sm text-destructive">{actionError}</div>
      )}
      {gerandoMsg && (
        <div className="rounded-lg border border-green-300 bg-green-50 dark:bg-green-950/30 px-4 py-3 text-sm text-green-700 dark:text-green-300">{gerandoMsg}</div>
      )}

      <div className="rounded-xl border p-4 flex flex-col gap-3 text-sm">
        <div className="grid grid-cols-2 gap-x-4 gap-y-2">
          <div><span className="text-muted-foreground">Cliente:</span> <span className="font-medium">{c.clienteNome}</span></div>
          <div><span className="text-muted-foreground">Tipo:</span> {c.tipoCobranca === 'ParceladoPrazoFixo' ? 'Parcelado' : 'Recorrente'}</div>
          <div><span className="text-muted-foreground">Valor:</span> <span className="font-medium">{fmtVal(c.valor)}</span></div>
          <div><span className="text-muted-foreground">Periodicidade:</span> {c.periodicidade}</div>
          <div><span className="text-muted-foreground">Início:</span> {fmtDate(c.dataInicio)}</div>
          <div><span className="text-muted-foreground">Término:</span> {c.dataFim ? fmtDate(c.dataFim) : '—'}</div>
          <div><span className="text-muted-foreground">Vencimento:</span> dia {c.diaVencimento}</div>
        </div>

        {c.objeto && (
          <div>
            <div className="text-muted-foreground text-xs font-medium uppercase tracking-wide mb-1">Objeto</div>
            <div className="rounded bg-muted/40 px-3 py-2 whitespace-pre-wrap text-sm">{c.objeto}</div>
          </div>
        )}

        {c.itens.length > 0 && (
          <div>
            <div className="text-muted-foreground text-xs font-medium uppercase tracking-wide mb-2">Itens</div>
            <table className="w-full text-sm">
              <thead><tr className="border-b text-muted-foreground text-xs">
                <th className="text-left pb-1">Descrição</th>
                <th className="text-right pb-1">Qtd</th>
                <th className="text-right pb-1">Unit.</th>
                <th className="text-right pb-1">Total</th>
              </tr></thead>
              <tbody>
                {c.itens.map(item => (
                  <tr key={item.id} className="border-b last:border-0">
                    <td className="py-1">{item.descricao}</td>
                    <td className="py-1 text-right">{item.quantidade}</td>
                    <td className="py-1 text-right">{fmtVal(item.valorUnitario)}</td>
                    <td className="py-1 text-right font-medium">{fmtVal(item.quantidade * item.valorUnitario)}</td>
                  </tr>
                ))}
              </tbody>
            </table>
            <div className="text-right font-bold mt-2">Total: {fmtVal(c.total)}</div>
          </div>
        )}
      </div>

      <div className="flex gap-2">
        {c.status === 'Rascunho' && (
          <>
            <Button onClick={() => handleAcao(() => ativar(c.id))} className="bg-green-600 hover:bg-green-700">Ativar</Button>
            <Button variant="outline" className="text-destructive" onClick={() => handleAcao(() => cancelar(c.id))}>Cancelar</Button>
          </>
        )}
        {c.status === 'Ativo' && (
          <>
            <Button variant="outline" onClick={() => handleAcao(() => encerrar(c.id))}>Encerrar</Button>
            <Button variant="outline" className="text-destructive" onClick={() => handleAcao(() => cancelar(c.id))}>Cancelar</Button>
          </>
        )}
      </div>

      {modalGerar && (
        <div className="fixed inset-0 bg-black/50 z-50 flex items-center justify-center">
          <div className="bg-background rounded-xl border p-6 w-full max-w-sm flex flex-col gap-4">
            <h2 className="font-bold">Gerar Cobranças</h2>
            <div>
              <label className="block text-sm mb-1">De</label>
              <input type="date" value={de} onChange={e => setDe(e.target.value)}
                className="w-full rounded-lg border border-input bg-transparent px-3 py-2 text-sm" />
            </div>
            <div>
              <label className="block text-sm mb-1">Até</label>
              <input type="date" value={ate} onChange={e => setAte(e.target.value)}
                className="w-full rounded-lg border border-input bg-transparent px-3 py-2 text-sm" />
            </div>
            <div className="flex gap-2">
              <Button onClick={handleGerarCobrancas} disabled={!de || !ate}>Gerar</Button>
              <Button variant="outline" onClick={() => setModalGerar(false)}>Cancelar</Button>
            </div>
          </div>
        </div>
      )}
    </div>
  )
}
