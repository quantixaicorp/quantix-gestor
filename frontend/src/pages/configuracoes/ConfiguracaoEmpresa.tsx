import { useEffect, useState } from 'react'
import { api } from '@/services/api'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { toast } from '@/hooks/useToast'
import type { ConfiguracaoEmpresaResponse } from '@/types/fiscal'
import { useDashboardLayout } from '@/hooks/useDashboardLayout'
import { ALL_WIDGETS } from '@/components/dashboard/widgetRegistry'
import type { WidgetId } from '@/types/dashboard'
import { useRelatorioLayout } from '@/hooks/useRelatorioLayout'
import type { RelatorioTabId } from '@/types/relatorios'

const ALL_TABS: { id: RelatorioTabId; label: string; description: string }[] = [
  { id: 'visao-geral', label: 'Visão Geral', description: 'KPIs gerais: faturamento, ticket médio, margem, inadimplência' },
  { id: 'vendas', label: 'Vendas', description: 'Tendência de vendas, top produtos, forma de pagamento' },
  { id: 'financeiro', label: 'Financeiro', description: 'Fluxo de caixa e despesas por categoria' },
  { id: 'estoque', label: 'Estoque', description: 'Valor em estoque, giro e produtos sem movimentação' },
  { id: 'clientes', label: 'Clientes', description: 'Ranking de clientes por faturamento e taxa de atividade' },
  { id: 'agendamentos', label: 'Agendamentos', description: 'Taxa de conclusão, ocupação e ranking por profissional' },
  { id: 'contratos', label: 'Contratos', description: 'MRR, contratos ativos e próximos do vencimento' },
  { id: 'cobrancas', label: 'Cobranças', description: 'Inadimplência, aging e cobranças vencidas' },
  { id: 'orcamentos', label: 'Orçamentos', description: 'Pipeline, taxa de conversão e status dos orçamentos' },
  { id: 'assinaturas', label: 'Assinaturas', description: 'MRR, churn e evolução mensal das assinaturas' },
  { id: 'curva-abc', label: 'Curva ABC', description: 'Classificação A/B/C de produtos e clientes por faturamento' },
  { id: 'dre', label: 'DRE', description: 'Demonstração do Resultado do Exercício' },
]

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
  const { widgets, load: loadLayout, save: saveLayout } = useDashboardLayout()
  const [savingLayout, setSavingLayout] = useState(false)
  const [localWidgets, setLocalWidgets] = useState<WidgetId[]>([])

  const { tabs, load: loadRelatorioLayout, save: saveRelatorioLayout } = useRelatorioLayout()
  const [savingTabLayout, setSavingTabLayout] = useState(false)
  const [localTabs, setLocalTabs] = useState<RelatorioTabId[]>([])

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

  useEffect(() => { void loadLayout() }, [loadLayout])
  useEffect(() => { setLocalWidgets(widgets) }, [widgets])

  useEffect(() => { void loadRelatorioLayout() }, [loadRelatorioLayout])
  useEffect(() => { setLocalTabs(tabs) }, [tabs])

  useEffect(() => {
    api.get<ConfiguracaoEmpresaResponse>('/api/configuracao-empresa')
      .then(c => {
        setIdent({ razaoSocial: c.razaoSocial ?? '', nomeFantasia: c.nomeFantasia ?? '', cnpj: c.cnpj ?? '', inscricaoEstadual: c.inscricaoEstadual ?? '', inscricaoMunicipal: c.inscricaoMunicipal ?? '', telefone: c.telefone ?? '', email: c.email ?? '' })
        setEnd({ logradouro: c.logradouro ?? '', numero: c.numero ?? '', complemento: c.complemento ?? '', bairro: c.bairro ?? '', codigoMunicipio: c.codigoMunicipio ?? '', municipio: c.municipio ?? '', uf: c.uf ?? '', cep: c.cep ?? '' })
        setVisual({ slug: c.slug ?? '', nomeExibicao: c.nomeFantasia ?? '', corPrimaria: c.corPrimaria ?? '#2563eb', descricaoPublica: c.descricaoPublica ?? '', logoUrl: c.logoUrl ?? '' })
        setNfe({ regimeTributario: c.regimeTributario ?? 1, ambiente: c.ambiente ?? 2, serieNfe: c.serieNfe ?? 1, serieNfce: c.serieNfce ?? 1 })
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

  function toggleWidget(id: WidgetId) {
    setLocalWidgets(prev =>
      prev.includes(id) ? prev.filter(w => w !== id) : [...prev, id]
    )
  }

  function moveWidget(id: WidgetId, dir: -1 | 1) {
    setLocalWidgets(prev => {
      const idx = prev.indexOf(id)
      if (idx < 0) return prev
      const next = [...prev]
      const swap = idx + dir
      if (swap < 0 || swap >= next.length) return prev
      ;[next[idx], next[swap]] = [next[swap], next[idx]]
      return next
    })
  }

  function toggleTab(id: RelatorioTabId) {
    setLocalTabs(prev =>
      prev.includes(id) ? prev.filter(t => t !== id) : [...prev, id]
    )
  }

  function moveTab(id: RelatorioTabId, dir: -1 | 1) {
    setLocalTabs(prev => {
      const idx = prev.indexOf(id)
      if (idx < 0) return prev
      const next = [...prev]
      const swap = idx + dir
      if (swap < 0 || swap >= next.length) return prev
      ;[next[idx], next[swap]] = [next[swap], next[idx]]
      return next
    })
  }

  async function saveTabLayoutConfig() {
    setSavingTabLayout(true)
    try {
      await saveRelatorioLayout(localTabs)
      toast.success('Abas de relatórios salvas!')
    } catch {
      toast.error('Erro ao salvar abas')
    } finally {
      setSavingTabLayout(false)
    }
  }

  async function saveLayoutConfig() {
    setSavingLayout(true)
    try {
      await saveLayout(localWidgets)
      toast.success('Layout do dashboard salvo!')
    } catch {
      toast.error('Erro ao salvar layout')
    } finally {
      setSavingLayout(false)
    }
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

      {/* Dashboard */}
      <div className="rounded-xl border bg-card p-5 space-y-4">
        <p className="text-sm font-semibold text-muted-foreground uppercase tracking-wide">Widgets do Dashboard</p>
        <p className="text-xs text-muted-foreground">Ative ou desative widgets e reordene arrastando com as setas.</p>

        {/* Active widgets (ordered) */}
        {localWidgets.length > 0 && (
          <div className="space-y-1">
            <p className="text-xs font-medium text-muted-foreground mb-2">Ativos (em ordem)</p>
            {localWidgets.map((id, idx) => {
              const meta = ALL_WIDGETS.find(w => w.id === id)
              if (!meta) return null
              return (
                <div key={id} className="flex items-center gap-2 rounded-md border bg-muted/30 px-3 py-2">
                  <div className="flex flex-col gap-0.5">
                    <button
                      onClick={() => moveWidget(id, -1)}
                      disabled={idx === 0}
                      className="text-muted-foreground hover:text-foreground disabled:opacity-30 leading-none text-xs"
                      aria-label="mover para cima"
                    >▲</button>
                    <button
                      onClick={() => moveWidget(id, 1)}
                      disabled={idx === localWidgets.length - 1}
                      className="text-muted-foreground hover:text-foreground disabled:opacity-30 leading-none text-xs"
                      aria-label="mover para baixo"
                    >▼</button>
                  </div>
                  <div className="flex-1 min-w-0">
                    <p className="text-sm font-medium truncate">{meta.label}</p>
                    <p className="text-xs text-muted-foreground truncate">{meta.description}</p>
                  </div>
                  <button
                    onClick={() => toggleWidget(id)}
                    className="text-xs text-red-500 hover:text-red-700 shrink-0"
                  >Remover</button>
                </div>
              )
            })}
          </div>
        )}

        {/* Inactive widgets */}
        {ALL_WIDGETS.filter(w => !localWidgets.includes(w.id)).length > 0 && (
          <div className="space-y-1">
            <p className="text-xs font-medium text-muted-foreground mb-2">Disponíveis</p>
            {ALL_WIDGETS.filter(w => !localWidgets.includes(w.id)).map(meta => (
              <div key={meta.id} className="flex items-center gap-2 rounded-md border border-dashed px-3 py-2 opacity-60">
                <div className="flex-1 min-w-0">
                  <p className="text-sm font-medium truncate">{meta.label}</p>
                  <p className="text-xs text-muted-foreground truncate">{meta.description}</p>
                </div>
                <button
                  onClick={() => toggleWidget(meta.id)}
                  className="text-xs text-primary hover:underline shrink-0"
                >Adicionar</button>
              </div>
            ))}
          </div>
        )}

        <Button onClick={() => void saveLayoutConfig()} disabled={savingLayout} size="sm">
          {savingLayout ? 'Salvando...' : 'Salvar layout'}
        </Button>
      </div>

      {/* Abas de Relatórios */}
      <div className="rounded-xl border bg-card p-5 space-y-4">
        <p className="text-sm font-semibold text-muted-foreground uppercase tracking-wide">Abas de Relatórios</p>
        <p className="text-xs text-muted-foreground">Ative, desative e reordene as abas que aparecem no módulo de Relatórios.</p>

        {localTabs.length > 0 && (
          <div className="space-y-1">
            <p className="text-xs font-medium text-muted-foreground mb-2">Ativas (em ordem)</p>
            {localTabs.map((id, idx) => {
              const meta = ALL_TABS.find(t => t.id === id)
              if (!meta) return null
              return (
                <div key={id} className="flex items-center gap-2 rounded-md border bg-muted/30 px-3 py-2">
                  <div className="flex flex-col gap-0.5">
                    <button onClick={() => moveTab(id, -1)} disabled={idx === 0}
                      className="text-muted-foreground hover:text-foreground disabled:opacity-30 leading-none text-xs"
                      aria-label="mover para cima">▲</button>
                    <button onClick={() => moveTab(id, 1)} disabled={idx === localTabs.length - 1}
                      className="text-muted-foreground hover:text-foreground disabled:opacity-30 leading-none text-xs"
                      aria-label="mover para baixo">▼</button>
                  </div>
                  <div className="flex-1 min-w-0">
                    <p className="text-sm font-medium truncate">{meta.label}</p>
                    <p className="text-xs text-muted-foreground truncate">{meta.description}</p>
                  </div>
                  <button onClick={() => toggleTab(id)} className="text-xs text-red-500 hover:text-red-700 shrink-0">
                    Remover
                  </button>
                </div>
              )
            })}
          </div>
        )}

        {ALL_TABS.filter(t => !localTabs.includes(t.id)).length > 0 && (
          <div className="space-y-1">
            <p className="text-xs font-medium text-muted-foreground mb-2">Disponíveis</p>
            {ALL_TABS.filter(t => !localTabs.includes(t.id)).map(meta => (
              <div key={meta.id} className="flex items-center gap-2 rounded-md border border-dashed px-3 py-2 opacity-60">
                <div className="flex-1 min-w-0">
                  <p className="text-sm font-medium truncate">{meta.label}</p>
                  <p className="text-xs text-muted-foreground truncate">{meta.description}</p>
                </div>
                <button onClick={() => toggleTab(meta.id)} className="text-xs text-primary hover:underline shrink-0">
                  Adicionar
                </button>
              </div>
            ))}
          </div>
        )}

        <Button onClick={() => void saveTabLayoutConfig()} disabled={savingTabLayout} size="sm">
          {savingTabLayout ? 'Salvando...' : 'Salvar abas'}
        </Button>
      </div>

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

    </div>
  )
}
