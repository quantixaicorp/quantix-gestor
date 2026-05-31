import { useState, useEffect } from 'react'
import { useConfiguracaoEmpresa } from '@/hooks/useConfiguracaoEmpresa'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { toast } from '@/hooks/useToast'
import type { AtualizarConfiguracaoEmpresaRequest } from '@/types/fiscal'

const REGIMES = [
  { value: 1, label: '1 — Simples Nacional' },
  { value: 2, label: '2 — Simples Nacional — Excesso' },
  { value: 3, label: '3 — Regime Normal' },
]

const AMBIENTES = [
  { value: 1, label: '1 — Produção' },
  { value: 2, label: '2 — Homologação (testes)' },
]

function Field({ label, children, colSpan }: { label: string; children: React.ReactNode; colSpan?: boolean }) {
  return (
    <div className={`space-y-1.5 ${colSpan ? 'col-span-2' : ''}`}>
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
        razaoSocial:        c.razaoSocial        ?? '',
        nomeFantasia:       c.nomeFantasia       ?? '',
        cnpj:               c.cnpj               ?? '',
        inscricaoEstadual:  c.inscricaoEstadual  ?? '',
        inscricaoMunicipal: c.inscricaoMunicipal ?? '',
        logradouro:         c.logradouro         ?? '',
        numero:             c.numero             ?? '',
        complemento:        c.complemento        ?? '',
        bairro:             c.bairro             ?? '',
        codigoMunicipio:    c.codigoMunicipio    ?? '',
        municipio:          c.municipio          ?? '',
        uf:                 c.uf                 ?? '',
        cep:                c.cep                ?? '',
        regimeTributario:   c.regimeTributario   ?? 1,
        ambiente:           c.ambiente           ?? 2,
        serieNfe:           c.serieNfe           ?? 1,
        serieNfce:          c.serieNfce          ?? 1,
      })
    })
  }, [obter])

  const set = (key: keyof AtualizarConfiguracaoEmpresaRequest) =>
    (e: React.ChangeEvent<HTMLInputElement | HTMLSelectElement>) =>
      setForm(prev => ({ ...prev, [key]: e.target.value }))

  const setNum = (key: keyof AtualizarConfiguracaoEmpresaRequest) =>
    (e: React.ChangeEvent<HTMLSelectElement>) =>
      setForm(prev => ({ ...prev, [key]: Number(e.target.value) }))

  async function handleSalvar() {
    setSalvando(true)
    try {
      const req: AtualizarConfiguracaoEmpresaRequest = { ...form }
      if (focusNfeToken.trim()) req.focusNfeToken = focusNfeToken.trim()
      await atualizar(req)
      setFocusNfeToken('')
      toast.success('Configuração salva com sucesso!')
    } catch (e) {
      toast.error(e instanceof Error ? e.message : 'Erro ao salvar configuração')
    } finally {
      setSalvando(false)
    }
  }

  if (loading) return <div className="flex h-48 items-center justify-center text-muted-foreground">Carregando...</div>
  if (error)   return <div className="flex h-48 items-center justify-center text-destructive">{error}</div>

  return (
    <div className="space-y-6 max-w-2xl">
      <h2 className="text-lg font-semibold">Configuração da Empresa</h2>

      <div className="rounded-xl border bg-card p-5 space-y-4">
        <p className="text-sm font-semibold text-muted-foreground uppercase tracking-wide">Dados da Empresa</p>
        <div className="grid grid-cols-2 gap-4">
          <Field label="Razão Social" colSpan>
            <Input value={form.razaoSocial ?? ''} onChange={set('razaoSocial')} />
          </Field>
          <Field label="Nome Fantasia" colSpan>
            <Input value={form.nomeFantasia ?? ''} onChange={set('nomeFantasia')} />
          </Field>
          <Field label="CNPJ">
            <Input value={form.cnpj ?? ''} onChange={set('cnpj')} placeholder="00.000.000/0000-00" />
          </Field>
          <Field label="Inscrição Estadual">
            <Input value={form.inscricaoEstadual ?? ''} onChange={set('inscricaoEstadual')} />
          </Field>
          <Field label="Inscrição Municipal">
            <Input value={form.inscricaoMunicipal ?? ''} onChange={set('inscricaoMunicipal')} />
          </Field>
          <Field label="Regime Tributário">
            <select value={form.regimeTributario ?? 1} onChange={setNum('regimeTributario')}
              className="flex h-9 w-full rounded-lg border border-input bg-transparent px-3 py-1 text-sm">
              {REGIMES.map(r => <option key={r.value} value={r.value}>{r.label}</option>)}
            </select>
          </Field>
        </div>
      </div>

      <div className="rounded-xl border bg-card p-5 space-y-4">
        <p className="text-sm font-semibold text-muted-foreground uppercase tracking-wide">Endereço</p>
        <div className="grid grid-cols-2 gap-4">
          <Field label="Logradouro" colSpan>
            <Input value={form.logradouro ?? ''} onChange={set('logradouro')} />
          </Field>
          <Field label="Número">
            <Input value={form.numero ?? ''} onChange={set('numero')} />
          </Field>
          <Field label="Complemento">
            <Input value={form.complemento ?? ''} onChange={set('complemento')} />
          </Field>
          <Field label="Bairro">
            <Input value={form.bairro ?? ''} onChange={set('bairro')} />
          </Field>
          <Field label="Município" colSpan>
            <Input value={form.municipio ?? ''} onChange={set('municipio')} />
          </Field>
          <Field label="Código Município (IBGE)">
            <Input value={form.codigoMunicipio ?? ''} onChange={set('codigoMunicipio')} />
          </Field>
          <Field label="UF">
            <Input value={form.uf ?? ''} onChange={set('uf')} maxLength={2} className="uppercase" />
          </Field>
          <Field label="CEP">
            <Input value={form.cep ?? ''} onChange={set('cep')} placeholder="00000-000" />
          </Field>
        </div>
      </div>

      <div className="rounded-xl border bg-card p-5 space-y-4">
        <p className="text-sm font-semibold text-muted-foreground uppercase tracking-wide">Configuração NF-e</p>
        <div className="grid grid-cols-2 gap-4">
          <Field label="Ambiente">
            <select value={form.ambiente ?? 2} onChange={setNum('ambiente')}
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
            Token Focus NFe{' '}
            {config?.temToken && <span className="text-green-600 font-medium">(configurado)</span>}
          </Label>
          <Input
            type="password"
            value={focusNfeToken}
            onChange={e => setFocusNfeToken(e.target.value)}
            placeholder={config?.temToken ? 'Deixe em branco para manter o atual' : 'Cole o token da API Focus NFe'}
          />
        </div>
      </div>

      <Button onClick={() => void handleSalvar()} disabled={salvando}>
        {salvando ? 'Salvando...' : 'Salvar Configuração'}
      </Button>
    </div>
  )
}
