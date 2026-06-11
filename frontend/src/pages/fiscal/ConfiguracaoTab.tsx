import { useState, useEffect } from 'react'
import { Link } from 'react-router-dom'
import { useConfiguracaoEmpresa } from '@/hooks/useConfiguracaoEmpresa'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { toast } from '@/hooks/useToast'
import type { AtualizarConfiguracaoEmpresaRequest } from '@/types/fiscal'

const AMBIENTES = [
  { value: 1, label: '1 — Produção' },
  { value: 2, label: '2 — Homologação (testes)' },
]

function Field({ label, children }: { label: string; children: React.ReactNode }) {
  return (
    <div className="space-y-1.5">
      <Label className="text-xs text-muted-foreground">{label}</Label>
      {children}
    </div>
  )
}

export default function ConfiguracaoTab() {
  const { config, loading, error, obter, atualizar } = useConfiguracaoEmpresa()
  const [form, setForm] = useState<AtualizarConfiguracaoEmpresaRequest>({})
  const [salvando, setSalvando] = useState(false)
  const [focusNfeToken, setFocusNfeToken] = useState('')

  useEffect(() => {
    void obter().then(c => {
      if (!c) return
      setForm({
        ambiente: c.ambiente ?? 2,
        serieNfe: c.serieNfe ?? 1,
        serieNfce: c.serieNfce ?? 1,
      })
    })
  }, [obter])

  async function handleSalvar() {
    setSalvando(true)
    try {
      const req: AtualizarConfiguracaoEmpresaRequest = { ...form }
      if (focusNfeToken.trim()) req.focusNfeToken = focusNfeToken.trim()
      await atualizar(req)
      setFocusNfeToken('')
      toast.success('Configuração NF-e salva!')
    } catch (e) {
      toast.error(e instanceof Error ? e.message : 'Erro ao salvar')
    } finally {
      setSalvando(false)
    }
  }

  if (loading) return <div className="flex h-48 items-center justify-center text-muted-foreground">Carregando...</div>
  if (error) return <div className="flex h-48 items-center justify-center text-destructive">{error}</div>

  return (
    <div className="space-y-6 max-w-2xl">
      <div className="rounded-xl border bg-card p-4">
        <p className="text-sm text-muted-foreground">
          Dados da empresa (razão social, CNPJ, endereço) agora estão em{' '}
          <Link to="/configuracoes/empresa" className="underline text-primary">
            Configuração da Empresa
          </Link>.
        </p>
      </div>

      <h2 className="text-lg font-semibold">Configuração NF-e</h2>

      <div className="rounded-xl border bg-card p-5 space-y-4">
        <div className="grid grid-cols-2 gap-4">
          <Field label="Ambiente">
            <select value={form.ambiente ?? 2}
              onChange={e => setForm(p => ({ ...p, ambiente: Number(e.target.value) }))}
              className="flex h-9 w-full rounded-lg border border-input bg-transparent px-3 py-1 text-sm">
              {AMBIENTES.map(a => <option key={a.value} value={a.value}>{a.label}</option>)}
            </select>
          </Field>
          <Field label="Série NF-e">
            <Input type="number" min="1" value={form.serieNfe ?? 1}
              onChange={e => setForm(p => ({ ...p, serieNfe: Number(e.target.value) }))} />
          </Field>
          <Field label="Série NFC-e">
            <Input type="number" min="1" value={form.serieNfce ?? 1}
              onChange={e => setForm(p => ({ ...p, serieNfce: Number(e.target.value) }))} />
          </Field>
        </div>
        <div className="space-y-1.5">
          <Label className="text-xs text-muted-foreground">
            Token Focus NFe{config?.temToken && <span className="text-green-600 font-medium"> (configurado)</span>}
          </Label>
          <Input type="password" value={focusNfeToken} onChange={e => setFocusNfeToken(e.target.value)}
            placeholder={config?.temToken ? 'Deixe em branco para manter o atual' : 'Cole o token da API Focus NFe'} />
        </div>
      </div>

      <Button onClick={() => void handleSalvar()} disabled={salvando}>
        {salvando ? 'Salvando...' : 'Salvar Configuração NF-e'}
      </Button>
    </div>
  )
}
