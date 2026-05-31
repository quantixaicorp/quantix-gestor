import { useState, useEffect } from 'react'
import { FileText, RefreshCw, XCircle, ExternalLink, CheckCircle2, Clock, AlertCircle } from 'lucide-react'
import { useNotasFiscais } from '@/hooks/useNotasFiscais'
import { Button } from '@/components/ui/button'
import { Label } from '@/components/ui/label'
import { toast } from '@/hooks/useToast'
import type { NotaFiscalResponse } from '@/types/fiscal'

type StatusKey = 'Pendente' | 'Processando' | 'Autorizada' | 'Rejeitada' | 'Cancelada'

const STATUS_CONFIG: Record<StatusKey, { label: string; Icon: typeof Clock; className: string }> = {
  Pendente:    { label: 'Pendente',    Icon: Clock,        className: 'text-yellow-600 bg-yellow-50 border-yellow-200' },
  Processando: { label: 'Processando', Icon: Clock,        className: 'text-blue-600 bg-blue-50 border-blue-200' },
  Autorizada:  { label: 'Autorizada',  Icon: CheckCircle2, className: 'text-green-600 bg-green-50 border-green-200' },
  Rejeitada:   { label: 'Rejeitada',   Icon: AlertCircle,  className: 'text-red-600 bg-red-50 border-red-200' },
  Cancelada:   { label: 'Cancelada',   Icon: XCircle,      className: 'text-gray-500 bg-gray-50 border-gray-200' },
}

const fmt = new Intl.DateTimeFormat('pt-BR', { dateStyle: 'short', timeStyle: 'short' })
const fmtDate = (d: string | null) => (d ? fmt.format(new Date(d)) : '—')

function StatusBadge({ status }: { status: string }) {
  const cfg = STATUS_CONFIG[status as StatusKey] ?? STATUS_CONFIG.Pendente
  const { Icon } = cfg
  return (
    <span className={`inline-flex items-center gap-1 rounded border px-2 py-0.5 text-xs font-medium ${cfg.className}`}>
      <Icon size={11} />
      {cfg.label}
    </span>
  )
}

export default function ListaNotas() {
  const { notas, loading, error, list, cancelar, consultar } = useNotasFiscais()
  const [cancelModal, setCancelModal] = useState<NotaFiscalResponse | null>(null)
  const [motivo, setMotivo] = useState('')
  const [actionId, setActionId] = useState<string | null>(null)

  useEffect(() => { void list() }, [list])

  async function handleCancelar() {
    if (!cancelModal || !motivo.trim()) return
    setActionId(cancelModal.id)
    try {
      await cancelar(cancelModal.id, { motivo })
      setCancelModal(null)
      setMotivo('')
    } catch (e) {
      toast.error(e instanceof Error ? e.message : 'Erro ao cancelar nota')
    } finally {
      setActionId(null)
    }
  }

  async function handleConsultar(id: string) {
    setActionId(id)
    try {
      await consultar(id)
    } catch (e) {
      toast.error(e instanceof Error ? e.message : 'Erro ao consultar nota')
    } finally {
      setActionId(null)
    }
  }

  if (loading) return <div className="flex h-48 items-center justify-center text-muted-foreground">Carregando...</div>
  if (error) return <div className="flex h-48 items-center justify-center text-destructive">{error}</div>

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between">
        <h2 className="text-lg font-semibold">Notas Fiscais</h2>
        <Button size="sm" variant="outline" onClick={() => void list()}>
          <RefreshCw size={14} className="mr-1.5" /> Atualizar
        </Button>
      </div>

      {notas.length === 0 ? (
        <div className="flex flex-col items-center justify-center h-48 text-center text-muted-foreground">
          <FileText size={36} className="mb-3 opacity-30" />
          <p className="text-sm">Nenhuma nota fiscal emitida</p>
        </div>
      ) : (
        <div className="rounded-xl border overflow-hidden">
          <table className="w-full text-sm">
            <thead className="bg-muted/40 border-b">
              <tr>
                <th className="text-left px-4 py-3 font-medium text-muted-foreground">Número</th>
                <th className="text-left px-4 py-3 font-medium text-muted-foreground">Modelo</th>
                <th className="text-left px-4 py-3 font-medium text-muted-foreground">Status</th>
                <th className="text-left px-4 py-3 font-medium text-muted-foreground">Emitida em</th>
                <th className="text-left px-4 py-3 font-medium text-muted-foreground">Chave</th>
                <th className="px-4 py-3" />
              </tr>
            </thead>
            <tbody className="divide-y">
              {notas.map(nota => (
                <tr key={nota.id} className="hover:bg-muted/20 transition-colors">
                  <td className="px-4 py-3 font-mono">
                    {nota.numero ? `${nota.serie ?? 1}-${nota.numero}` : '—'}
                  </td>
                  <td className="px-4 py-3">
                    <span className="rounded bg-primary/10 text-primary px-1.5 py-0.5 text-xs font-semibold">
                      {nota.modelo}
                    </span>
                  </td>
                  <td className="px-4 py-3"><StatusBadge status={nota.status} /></td>
                  <td className="px-4 py-3 text-muted-foreground">{fmtDate(nota.criadaEm)}</td>
                  <td className="px-4 py-3 font-mono text-xs text-muted-foreground max-w-[160px] truncate">
                    {nota.chaveAcesso ?? '—'}
                  </td>
                  <td className="px-4 py-3">
                    <div className="flex items-center gap-1.5 justify-end">
                      {(nota.status === 'Pendente' || nota.status === 'Processando') && (
                        <Button size="sm" variant="outline" className="h-7 text-xs"
                          disabled={actionId === nota.id}
                          onClick={() => void handleConsultar(nota.id)}>
                          <RefreshCw size={11} className="mr-1" />
                          Consultar
                        </Button>
                      )}
                      {nota.pdfUrl && (
                        <a href={nota.pdfUrl} target="_blank" rel="noreferrer">
                          <Button size="sm" variant="outline" className="h-7 text-xs">
                            <ExternalLink size={11} className="mr-1" /> PDF
                          </Button>
                        </a>
                      )}
                      {nota.xmlUrl && (
                        <a href={nota.xmlUrl} target="_blank" rel="noreferrer">
                          <Button size="sm" variant="outline" className="h-7 text-xs">
                            <ExternalLink size={11} className="mr-1" /> XML
                          </Button>
                        </a>
                      )}
                      {nota.status === 'Autorizada' && (
                        <Button size="sm" variant="ghost"
                          className="h-7 text-xs text-destructive hover:text-destructive hover:bg-destructive/10"
                          onClick={() => { setCancelModal(nota); setMotivo('') }}>
                          <XCircle size={11} className="mr-1" /> Cancelar
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

      {cancelModal && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/40">
          <div className="bg-background rounded-xl border shadow-xl w-full max-w-md p-6 space-y-4">
            <h3 className="text-base font-semibold">Cancelar Nota Fiscal</h3>
            <p className="text-sm text-muted-foreground">
              NF {cancelModal.serie ?? 1}-{cancelModal.numero} — informe o motivo (mínimo 15 caracteres).
            </p>
            <div className="space-y-1.5">
              <Label>Motivo</Label>
              <textarea
                value={motivo}
                onChange={e => setMotivo(e.target.value)}
                rows={3}
                className="flex w-full rounded-lg border border-input bg-transparent px-3 py-2 text-sm resize-none focus-visible:outline-none focus-visible:ring-1 focus-visible:ring-ring"
                placeholder="Descreva o motivo do cancelamento..."
              />
              {motivo.length > 0 && motivo.length < 15 && (
                <p className="text-xs text-destructive">Mínimo 15 caracteres ({motivo.length}/15)</p>
              )}
            </div>
            <div className="flex gap-2 justify-end">
              <Button variant="outline" onClick={() => setCancelModal(null)}>Voltar</Button>
              <Button variant="destructive"
                disabled={motivo.trim().length < 15 || !!actionId}
                onClick={() => void handleCancelar()}>
                {actionId ? 'Cancelando...' : 'Confirmar Cancelamento'}
              </Button>
            </div>
          </div>
        </div>
      )}
    </div>
  )
}
