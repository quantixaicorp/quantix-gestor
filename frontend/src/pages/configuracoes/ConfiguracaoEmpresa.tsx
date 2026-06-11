import { useEffect, useState } from 'react'
import { api } from '@/services/api'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { toast } from '@/hooks/useToast'
import type { ConfiguracaoEmpresaResponse } from '@/types/fiscal'

const API_BASE = import.meta.env.VITE_API_URL ?? 'http://localhost:5002'

function Section({ title, children, onSave, saving }: {
  title: string
  children: React.ReactNode
  onSave: () => void
  saving: boolean
}) {
  return (
    <div className="rounded-xl border bg-card p-5 space-y-4">
      <p className="text-sm font-semibold text-muted-foreground uppercase tracking-wide">{title}</p>
      {children}
      <Button onClick={onSave} disabled={saving} size="sm">
        {saving ? 'Salvando...' : 'Salvar'}
      </Button>
    </div>
  )
}

function Field({ label, children }: { label: string; children: React.ReactNode }) {
  return (
    <div className="space-y-1.5">
      <Label className="text-xs text-muted-foreground">{label}</Label>
      {children}
    </div>
  )
}

const REGIMES = [
  { value: 1, label: '1 — Simples Nacional' },
  { value: 2, label: '2 — Simples Nacional — Excesso' },
  { value: 3, label: '3 — Regime Normal' },
]
const AMBIENTES = [
  { value: 1, label: '1 — Produção' },
  { value: 2, label: '2 — Homologação (testes)' },
]

export default function ConfiguracaoEmpresa() {
  const [loading, setLoading] = useState(true)
  const [temToken, setTemToken] = useState(false)

  // Identificação
  const [ident, setIdent] = useState({ razaoSocial: '', nomeFantasia: '', cnpj: '', inscricaoEstadual: '', inscricaoMunicipal: '', telefone: '', email: '' })
  const [savingIdent, setSavingIdent] = useState(false)

  // Endereço
  const [end, setEnd] = useState({ logradouro: '', numero: '', complemento: '', bairro: '', codigoMunicipio: '', municipio: '', uf: '', cep: '' })
  const [savingEnd, setSavingEnd] = useState(false)

  // Identidade Visual
  const [visual, setVisual] = useState({ slug: '', nomeExibicao: '', corPrimaria: '#2563eb', descricaoPublica: '', logoUrl: '' })
  const [savingVisual, setSavingVisual] = useState(false)
  const [uploading, setUploading] = useState(false)

  // NF-e
  const [nfe, setNfe] = useState({ regimeTributario: 1, ambiente: 2, serieNfe: 1, serieNfce: 1 })
  const [focusNfeToken, setFocusNfeToken] = useState('')
  const [savingNfe, setSavingNfe] = useState(false)

  // Agendamento
  const [agend, setAgend] = useState({ aprovarAutomaticamente: true, valorSinal: '', horasLimiteCancelamento: '' })
  const [savingAgend, setSavingAgend] = useState(false)

  useEffect(() => {
    api.get<ConfiguracaoEmpresaResponse>('/api/configuracao-empresa')
      .then(c => {
        setIdent({ razaoSocial: c.razaoSocial ?? '', nomeFantasia: c.nomeFantasia ?? '', cnpj: c.cnpj ?? '', inscricaoEstadual: c.inscricaoEstadual ?? '', inscricaoMunicipal: c.inscricaoMunicipal ?? '', telefone: c.telefone ?? '', email: c.email ?? '' })
        setEnd({ logradouro: c.logradouro ?? '', numero: c.numero ?? '', complemento: c.complemento ?? '', bairro: c.bairro ?? '', codigoMunicipio: c.codigoMunicipio ?? '', municipio: c.municipio ?? '', uf: c.uf ?? '', cep: c.cep ?? '' })
        setVisual({ slug: c.slug ?? '', nomeExibicao: c.nomeFantasia ?? '', corPrimaria: c.corPrimaria ?? '#2563eb', descricaoPublica: c.descricaoPublica ?? '', logoUrl: c.logoUrl ?? '' })
        setNfe({ regimeTributario: c.regimeTributario ?? 1, ambiente: c.ambiente ?? 2, serieNfe: c.serieNfe ?? 1, serieNfce: c.serieNfce ?? 1 })
        setAgend({ aprovarAutomaticamente: c.aprovarAutomaticamente, valorSinal: c.valorSinal != null ? String(c.valorSinal) : '', horasLimiteCancelamento: c.horasLimiteCancelamento != null ? String(c.horasLimiteCancelamento) : '' })
        setTemToken(c.temToken)
      })
      .catch(() => toast.error('Erro ao carregar configurações'))
      .finally(() => setLoading(false))
  }, [])

  async function saveIdent() {
    setSavingIdent(true)
    try {
      await api.put('/api/configuracao-empresa', { ...ident })
      toast.success('Identificação salva!')
    } catch (e) { toast.error(e instanceof Error ? e.message : 'Erro') }
    finally { setSavingIdent(false) }
  }

  async function saveEnd() {
    setSavingEnd(true)
    try {
      await api.put('/api/configuracao-empresa', { ...end })
      toast.success('Endereço salvo!')
    } catch (e) { toast.error(e instanceof Error ? e.message : 'Erro') }
    finally { setSavingEnd(false) }
  }

  async function saveVisual() {
    setSavingVisual(true)
    try {
      await api.put('/api/configuracao-empresa/branding', { slug: visual.slug, nomeExibicao: visual.nomeExibicao || null, corPrimaria: visual.corPrimaria, descricaoPublica: visual.descricaoPublica || null })
      toast.success('Identidade visual salva!')
    } catch (e) { toast.error(e instanceof Error ? e.message : 'Erro') }
    finally { setSavingVisual(false) }
  }

  async function saveNfe() {
    setSavingNfe(true)
    try {
      const req: Record<string, unknown> = { ...nfe }
      if (focusNfeToken.trim()) req.focusNfeToken = focusNfeToken.trim()
      await api.put('/api/configuracao-empresa', req)
      setFocusNfeToken('')
      toast.success('Configuração NF-e salva!')
    } catch (e) { toast.error(e instanceof Error ? e.message : 'Erro') }
    finally { setSavingNfe(false) }
  }

  async function saveAgend() {
    setSavingAgend(true)
    try {
      await api.put('/api/configuracao-empresa/agendamento', {
        aprovarAutomaticamente: agend.aprovarAutomaticamente,
        valorSinal: agend.valorSinal ? parseFloat(agend.valorSinal) : null,
        horasLimiteCancelamento: agend.horasLimiteCancelamento ? parseInt(agend.horasLimiteCancelamento) : null,
      })
      toast.success('Configuração de agendamento salva!')
    } catch (e) { toast.error(e instanceof Error ? e.message : 'Erro') }
    finally { setSavingAgend(false) }
  }

  async function handleLogoUpload(e: React.ChangeEvent<HTMLInputElement>) {
    const file = e.target.files?.[0]
    if (!file) return
    setUploading(true)
    try {
      const formData = new FormData()
      formData.append('file', file)
      const token = localStorage.getItem('ga_token')
      const res = await fetch(`${API_BASE}/api/configuracao-empresa/logo`, {
        method: 'POST',
        headers: token ? { Authorization: `Bearer ${token}` } : {},
        body: formData,
      })
      if (!res.ok) throw new Error('Erro ao enviar logo')
      const data = await res.json() as { logoUrl: string }
      setVisual(v => ({ ...v, logoUrl: data.logoUrl }))
      toast.success('Logo atualizada!')
    } catch (e) { toast.error(e instanceof Error ? e.message : 'Erro ao enviar logo') }
    finally { setUploading(false) }
  }

  if (loading) return <div className="flex h-48 items-center justify-center text-muted-foreground">Carregando...</div>

  return (
    <div className="space-y-6 max-w-2xl">
      <h1 className="text-2xl font-bold">Configuração da Empresa</h1>

      {/* Identificação */}
      <Section title="Identificação" onSave={() => void saveIdent()} saving={savingIdent}>
        <div className="grid grid-cols-2 gap-4">
          <Field label="Razão Social"><Input value={ident.razaoSocial} onChange={e => setIdent(v => ({ ...v, razaoSocial: e.target.value }))} /></Field>
          <Field label="Nome Fantasia"><Input value={ident.nomeFantasia} onChange={e => setIdent(v => ({ ...v, nomeFantasia: e.target.value }))} /></Field>
          <Field label="CNPJ"><Input value={ident.cnpj} placeholder="00.000.000/0000-00" onChange={e => setIdent(v => ({ ...v, cnpj: e.target.value }))} /></Field>
          <Field label="Inscrição Estadual"><Input value={ident.inscricaoEstadual} onChange={e => setIdent(v => ({ ...v, inscricaoEstadual: e.target.value }))} /></Field>
          <Field label="Inscrição Municipal"><Input value={ident.inscricaoMunicipal} onChange={e => setIdent(v => ({ ...v, inscricaoMunicipal: e.target.value }))} /></Field>
          <Field label="Telefone"><Input value={ident.telefone} placeholder="(11) 99999-0000" onChange={e => setIdent(v => ({ ...v, telefone: e.target.value }))} /></Field>
          <Field label="E-mail"><Input type="email" value={ident.email} placeholder="contato@empresa.com" onChange={e => setIdent(v => ({ ...v, email: e.target.value }))} /></Field>
        </div>
      </Section>

      {/* Endereço */}
      <Section title="Endereço" onSave={() => void saveEnd()} saving={savingEnd}>
        <div className="grid grid-cols-2 gap-4">
          <div className="col-span-2"><Field label="Logradouro"><Input value={end.logradouro} onChange={e => setEnd(v => ({ ...v, logradouro: e.target.value }))} /></Field></div>
          <Field label="Número"><Input value={end.numero} onChange={e => setEnd(v => ({ ...v, numero: e.target.value }))} /></Field>
          <Field label="Complemento"><Input value={end.complemento} onChange={e => setEnd(v => ({ ...v, complemento: e.target.value }))} /></Field>
          <Field label="Bairro"><Input value={end.bairro} onChange={e => setEnd(v => ({ ...v, bairro: e.target.value }))} /></Field>
          <Field label="Código Município (IBGE)"><Input value={end.codigoMunicipio} onChange={e => setEnd(v => ({ ...v, codigoMunicipio: e.target.value }))} /></Field>
          <Field label="Município"><Input value={end.municipio} onChange={e => setEnd(v => ({ ...v, municipio: e.target.value }))} /></Field>
          <Field label="UF"><Input value={end.uf} maxLength={2} className="uppercase" onChange={e => setEnd(v => ({ ...v, uf: e.target.value.toUpperCase() }))} /></Field>
          <Field label="CEP"><Input value={end.cep} placeholder="00000-000" onChange={e => setEnd(v => ({ ...v, cep: e.target.value }))} /></Field>
        </div>
      </Section>

      {/* Identidade Visual */}
      <Section title="Identidade Visual" onSave={() => void saveVisual()} saving={savingVisual}>
        <Field label="Logo">
          <div className="flex items-center gap-4">
            {visual.logoUrl && (
              <img src={visual.logoUrl.startsWith('http') ? visual.logoUrl : `${API_BASE}${visual.logoUrl}`}
                alt="Logo" className="h-16 w-16 rounded-full object-cover border" />
            )}
            <label className="cursor-pointer">
              <span className="inline-flex items-center px-3 py-1.5 rounded-md border text-sm hover:bg-muted transition-colors">
                {uploading ? 'Enviando...' : 'Escolher arquivo'}
              </span>
              <input type="file" accept=".jpg,.jpeg,.png,.webp" className="hidden"
                onChange={e => void handleLogoUpload(e)} disabled={uploading} />
            </label>
            <p className="text-xs text-muted-foreground">jpg, png ou webp • máx 2MB</p>
          </div>
        </Field>
        <Field label="Nome de exibição público">
          <Input value={visual.nomeExibicao} onChange={e => setVisual(v => ({ ...v, nomeExibicao: e.target.value }))} placeholder="Ex: Barbearia do João" />
        </Field>
        <Field label="Cor primária">
          <div className="flex items-center gap-3">
            <input type="color" value={visual.corPrimaria}
              onChange={e => setVisual(v => ({ ...v, corPrimaria: e.target.value }))}
              className="h-9 w-16 rounded border cursor-pointer" />
            <Input value={visual.corPrimaria} placeholder="#2563eb" className="max-w-28"
              onChange={e => setVisual(v => ({ ...v, corPrimaria: e.target.value }))} />
          </div>
        </Field>
        <Field label="Slug (URL pública)">
          <div className="flex items-center gap-2">
            <span className="text-sm text-muted-foreground whitespace-nowrap">/agendar/</span>
            <Input value={visual.slug} placeholder="minha-empresa"
              onChange={e => setVisual(v => ({ ...v, slug: e.target.value.toLowerCase().replace(/[^a-z0-9-]/g, '') }))} />
          </div>
        </Field>
        <Field label="Descrição pública">
          <Input value={visual.descricaoPublica} placeholder="Ex: Barbearia especializada em cortes modernos"
            onChange={e => setVisual(v => ({ ...v, descricaoPublica: e.target.value }))} />
        </Field>
        {/* Preview */}
        <div>
          <p className="text-xs text-muted-foreground mb-2">Pré-visualização</p>
          <div className="rounded-lg overflow-hidden" style={{ backgroundColor: visual.corPrimaria }}>
            <div className="px-4 py-4 flex items-center gap-3">
              {visual.logoUrl
                ? <img src={visual.logoUrl.startsWith('http') ? visual.logoUrl : `${API_BASE}${visual.logoUrl}`}
                    alt="Logo" className="h-10 w-10 rounded-full object-cover border-2 border-white/30" />
                : <div className="h-10 w-10 rounded-full bg-white/20 flex items-center justify-center text-white text-lg font-bold">
                    {(visual.nomeExibicao?.[0] ?? 'E').toUpperCase()}
                  </div>
              }
              <div>
                <p className="text-white font-bold text-base leading-tight">{visual.nomeExibicao || 'Nome da empresa'}</p>
                {visual.descricaoPublica && <p className="text-white/80 text-xs">{visual.descricaoPublica}</p>}
              </div>
            </div>
          </div>
        </div>
      </Section>

      {/* NF-e */}
      <Section title="Emissão de Notas Fiscais" onSave={() => void saveNfe()} saving={savingNfe}>
        <div className="grid grid-cols-2 gap-4">
          <Field label="Regime Tributário">
            <select value={nfe.regimeTributario}
              onChange={e => setNfe(v => ({ ...v, regimeTributario: Number(e.target.value) }))}
              className="flex h-9 w-full rounded-lg border border-input bg-transparent px-3 py-1 text-sm">
              {REGIMES.map(r => <option key={r.value} value={r.value}>{r.label}</option>)}
            </select>
          </Field>
          <Field label="Ambiente">
            <select value={nfe.ambiente}
              onChange={e => setNfe(v => ({ ...v, ambiente: Number(e.target.value) }))}
              className="flex h-9 w-full rounded-lg border border-input bg-transparent px-3 py-1 text-sm">
              {AMBIENTES.map(a => <option key={a.value} value={a.value}>{a.label}</option>)}
            </select>
          </Field>
          <Field label="Série NF-e">
            <Input type="number" min="1" value={nfe.serieNfe}
              onChange={e => setNfe(v => ({ ...v, serieNfe: Number(e.target.value) }))} />
          </Field>
          <Field label="Série NFC-e">
            <Input type="number" min="1" value={nfe.serieNfce}
              onChange={e => setNfe(v => ({ ...v, serieNfce: Number(e.target.value) }))} />
          </Field>
        </div>
        <Field label={`Token Focus NF-e${temToken ? ' (configurado)' : ''}`}>
          <Input type="password" value={focusNfeToken} onChange={e => setFocusNfeToken(e.target.value)}
            placeholder={temToken ? 'Deixe em branco para manter o atual' : 'Cole o token da API Focus NFe'} />
        </Field>
      </Section>

      {/* Agendamento Online */}
      <Section title="Agendamento Online" onSave={() => void saveAgend()} saving={savingAgend}>
        <div className="space-y-4">
          <div className="flex items-center gap-2">
            <input type="checkbox" id="aprovarAuto" checked={agend.aprovarAutomaticamente}
              onChange={e => setAgend(v => ({ ...v, aprovarAutomaticamente: e.target.checked }))}
              className="h-4 w-4" />
            <Label htmlFor="aprovarAuto">Confirmar agendamentos automaticamente</Label>
          </div>
          <p className="text-xs text-muted-foreground -mt-2">
            Quando desmarcado, novos agendamentos ficam aguardando confirmação manual.
          </p>
          <Field label="Valor do sinal de reserva (R$)">
            <Input type="number" min="0" step="0.01" value={agend.valorSinal}
              onChange={e => setAgend(v => ({ ...v, valorSinal: e.target.value }))}
              placeholder="0,00 (sem sinal)" />
          </Field>
          <Field label="Horas mínimas para cancelamento">
            <Input type="number" min="0" value={agend.horasLimiteCancelamento}
              onChange={e => setAgend(v => ({ ...v, horasLimiteCancelamento: e.target.value }))}
              placeholder="Ex: 24 (sem restrição se vazio)" />
          </Field>
        </div>
      </Section>
    </div>
  )
}
