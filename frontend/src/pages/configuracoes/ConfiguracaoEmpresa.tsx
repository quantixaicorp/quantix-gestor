import { useEffect, useState } from 'react'
import { api } from '@/services/api'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { toast } from '@/hooks/useToast'

function maskCnpj(v: string) {
  const d = v.replace(/\D/g, '').slice(0, 14)
  return d
    .replace(/^(\d{2})(\d)/, '$1.$2')
    .replace(/^(\d{2})\.(\d{3})(\d)/, '$1.$2.$3')
    .replace(/\.(\d{3})(\d)/, '.$1/$2')
    .replace(/(\d{4})(\d)/, '$1-$2')
}

function maskCep(v: string) {
  const d = v.replace(/\D/g, '').slice(0, 8)
  return d.replace(/^(\d{5})(\d)/, '$1-$2')
}

function isValidCnpj(cnpj: string) {
  const d = cnpj.replace(/\D/g, '')
  if (d.length !== 14 || /^(\d)\1+$/.test(d)) return false
  const calc = (n: number) => {
    let s = 0, p = n - 7
    for (let i = 0; i < n; i++) { s += parseInt(d[i]) * (p--); if (p < 2) p = 9 }
    const r = s % 11; return r < 2 ? 0 : 11 - r
  }
  return calc(12) === parseInt(d[12]) && calc(13) === parseInt(d[13])
}
import type { ConfiguracaoEmpresaResponse } from '@/types/fiscal'
import { useDashboardLayout } from '@/hooks/useDashboardLayout'
import { ALL_WIDGETS, WIDGET_CATEGORY } from '@/components/dashboard/widgetRegistry'
import type { WidgetId } from '@/types/dashboard'
import { useRelatorioLayout } from '@/hooks/useRelatorioLayout'
import type { RelatorioTabId } from '@/types/relatorios'
import { cn } from '@/lib/utils'
import { ChevronDown, ChevronRight } from 'lucide-react'

const ALL_TABS: { id: RelatorioTabId; label: string; description: string }[] = [
  { id: 'visao-geral', label: 'Visão Geral', description: 'KPIs gerais, faturamento, ticket médio, margem' },
  { id: 'vendas', label: 'Vendas', description: 'Tendência, top produtos, forma de pagamento' },
  { id: 'financeiro', label: 'Financeiro', description: 'Fluxo de caixa e despesas por categoria' },
  { id: 'estoque', label: 'Estoque', description: 'Valor em estoque, giro e sem movimentação' },
  { id: 'clientes', label: 'Clientes', description: 'Ranking por faturamento e atividade' },
  { id: 'agendamentos', label: 'Agendamentos', description: 'Conclusão, ocupação e por profissional' },
  { id: 'contratos', label: 'Contratos', description: 'MRR, contratos ativos e vencendo' },
  { id: 'cobrancas', label: 'Cobranças', description: 'Inadimplência, aging e vencidas' },
  { id: 'orcamentos', label: 'Orçamentos', description: 'Pipeline, conversão e status' },
  { id: 'assinaturas', label: 'Assinaturas', description: 'MRR, churn e evolução mensal' },
  { id: 'curva-abc', label: 'Curva ABC', description: 'Classificação A/B/C de produtos e clientes' },
  { id: 'dre', label: 'DRE', description: 'Demonstração do Resultado do Exercício' },
  { id: 'compras', label: 'Compras', description: 'KPIs, evolução mensal, por fornecedor e top produtos comprados' },
]

const WIDGET_CATEGORIES_ORDER = [
  'Vendas', 'Financeiro', 'Clientes', 'Estoque',
  'Agendamentos', 'Contratos', 'Cobranças', 'Orçamentos', 'Assinaturas',
]

const API_BASE = import.meta.env.VITE_API_URL ?? 'http://localhost:5002'

const REGIMES = [
  { value: 1, label: '1 — Simples Nacional' },
  { value: 2, label: '2 — Simples Nacional — Excesso' },
  { value: 3, label: '3 — Regime Normal' },
]
const AMBIENTES = [
  { value: 1, label: '1 — Produção' },
  { value: 2, label: '2 — Homologação (testes)' },
]

type SettingsPage = 'empresa' | 'visual' | 'dashboard' | 'relatorios' | 'fiscal'
const NAV: { id: SettingsPage; label: string }[] = [
  { id: 'empresa', label: 'Empresa' },
  { id: 'visual', label: 'Visual' },
  { id: 'dashboard', label: 'Dashboard' },
  { id: 'relatorios', label: 'Relatórios' },
  { id: 'fiscal', label: 'Fiscal' },
]

function Panel({ title, children, action }: { title: string; children: React.ReactNode; action?: React.ReactNode }) {
  return (
    <div className="rounded-xl border bg-card p-5 space-y-4">
      <div className="flex items-center justify-between">
        <p className="text-sm font-semibold uppercase tracking-wide text-muted-foreground">{title}</p>
        {action}
      </div>
      {children}
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

export default function ConfiguracaoEmpresa() {
  const [page, setPage] = useState<SettingsPage>('empresa')

  const { widgets, load: loadLayout, save: saveLayout } = useDashboardLayout()
  const [savingLayout, setSavingLayout] = useState(false)
  const [localWidgets, setLocalWidgets] = useState<WidgetId[]>([])
  const [openCats, setOpenCats] = useState<Record<string, boolean>>({})

  const { tabs, load: loadRelatorioLayout, save: saveRelatorioLayout } = useRelatorioLayout()
  const [savingTabLayout, setSavingTabLayout] = useState(false)
  const [localTabs, setLocalTabs] = useState<RelatorioTabId[]>([])

  const [loading, setLoading] = useState(true)
  const [temToken, setTemToken] = useState(false)

  const [ident, setIdent] = useState({ razaoSocial: '', nomeFantasia: '', cnpj: '', inscricaoEstadual: '', inscricaoMunicipal: '', telefone: '', email: '', tipoNegocio: 'Lojista' })
  const [savingIdent, setSavingIdent] = useState(false)

  const [end, setEnd] = useState({ logradouro: '', numero: '', complemento: '', bairro: '', codigoMunicipio: '', municipio: '', uf: '', cep: '' })
  const [savingEnd, setSavingEnd] = useState(false)
  const [buscandoCep, setBuscandoCep] = useState(false)
  const [cnpjErro, setCnpjErro] = useState('')

  const [visual, setVisual] = useState({ slug: '', nomeExibicao: '', corPrimaria: '#2563eb', descricaoPublica: '', logoUrl: '' })
  const [savingVisual, setSavingVisual] = useState(false)
  const [uploading, setUploading] = useState(false)

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
        setIdent({ razaoSocial: c.razaoSocial ?? '', nomeFantasia: c.nomeFantasia ?? '', cnpj: c.cnpj ?? '', inscricaoEstadual: c.inscricaoEstadual ?? '', inscricaoMunicipal: c.inscricaoMunicipal ?? '', telefone: c.telefone ?? '', email: c.email ?? '', tipoNegocio: c.tipoNegocio || 'Lojista' })
        setEnd({ logradouro: c.logradouro ?? '', numero: c.numero ?? '', complemento: c.complemento ?? '', bairro: c.bairro ?? '', codigoMunicipio: c.codigoMunicipio ?? '', municipio: c.municipio ?? '', uf: c.uf ?? '', cep: c.cep ?? '' })
        setVisual({ slug: c.slug ?? '', nomeExibicao: c.nomeFantasia ?? '', corPrimaria: c.corPrimaria ?? '#2563eb', descricaoPublica: c.descricaoPublica ?? '', logoUrl: c.logoUrl ?? '' })
        setNfe({ regimeTributario: c.regimeTributario ?? 1, ambiente: c.ambiente ?? 2, serieNfe: c.serieNfe ?? 1, serieNfce: c.serieNfce ?? 1 })
        setTemToken(c.temToken)
      })
      .catch(() => toast.error('Erro ao carregar configurações'))
      .finally(() => setLoading(false))
  }, [])

  async function buscarCep(cep: string) {
    const raw = cep.replace(/\D/g, '')
    if (raw.length !== 8) return
    setBuscandoCep(true)
    try {
      const res = await fetch(`https://viacep.com.br/ws/${raw}/json/`)
      const data = await res.json() as { erro?: boolean; logradouro?: string; bairro?: string; localidade?: string; uf?: string; ibge?: string }
      if (data.erro) { toast.error('CEP não encontrado'); return }
      setEnd(v => ({
        ...v,
        logradouro: data.logradouro ?? v.logradouro,
        bairro: data.bairro ?? v.bairro,
        municipio: data.localidade ?? v.municipio,
        uf: data.uf ?? v.uf,
        codigoMunicipio: data.ibge ?? v.codigoMunicipio,
      }))
    } catch { toast.error('Erro ao buscar CEP') }
    finally { setBuscandoCep(false) }
  }

  async function saveIdent() {
    const cnpjRaw = ident.cnpj.replace(/\D/g, '')
    if (cnpjRaw && !isValidCnpj(cnpjRaw)) {
      setCnpjErro('CNPJ inválido')
      return
    }
    setCnpjErro('')
    setSavingIdent(true)
    try { await api.put('/api/configuracao-empresa', { ...ident, tipoNegocio: ident.tipoNegocio }); toast.success('Identificação salva!') }
    catch (e) { toast.error(e instanceof Error ? e.message : 'Erro') }
    finally { setSavingIdent(false) }
  }

  async function saveEnd() {
    setSavingEnd(true)
    try { await api.put('/api/configuracao-empresa', { ...end }); toast.success('Endereço salvo!') }
    catch (e) { toast.error(e instanceof Error ? e.message : 'Erro') }
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
    setLocalWidgets(prev => prev.includes(id) ? prev.filter(w => w !== id) : [...prev, id])
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
    setLocalTabs(prev => prev.includes(id) ? prev.filter(t => t !== id) : [...prev, id])
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

  async function saveLayoutConfig() {
    setSavingLayout(true)
    try { await saveLayout(localWidgets); toast.success('Layout do dashboard salvo!') }
    catch { toast.error('Erro ao salvar layout') }
    finally { setSavingLayout(false) }
  }

  async function saveTabLayoutConfig() {
    setSavingTabLayout(true)
    try { await saveRelatorioLayout(localTabs); toast.success('Abas de relatórios salvas!') }
    catch { toast.error('Erro ao salvar abas') }
    finally { setSavingTabLayout(false) }
  }

  if (loading) return <div className="flex h-48 items-center justify-center text-muted-foreground">Carregando...</div>

  return (
    <div className="space-y-6 max-w-2xl">
      <h1 className="text-2xl font-bold">Configuração da Empresa</h1>

      {/* ── Navegação ─────────────────────────────────────────── */}
      <div className="flex gap-1 rounded-lg border bg-muted/30 p-1 w-fit flex-wrap">
        {NAV.map(n => (
          <button
            key={n.id}
            onClick={() => setPage(n.id)}
            className={cn(
              'px-4 py-1.5 rounded-md text-sm font-medium transition-colors',
              page === n.id
                ? 'bg-background shadow-sm text-foreground'
                : 'text-muted-foreground hover:text-foreground'
            )}
          >
            {n.label}
          </button>
        ))}
      </div>

      {/* ── Empresa ───────────────────────────────────────────── */}
      {page === 'empresa' && (
        <div className="space-y-4">
          <Panel title="Identificação" action={
            <Button onClick={() => void saveIdent()} disabled={savingIdent} size="sm">
              {savingIdent ? 'Salvando...' : 'Salvar'}
            </Button>
          }>
            <div className="grid grid-cols-2 gap-4">
              <Field label="Razão Social"><Input value={ident.razaoSocial} onChange={e => setIdent(v => ({ ...v, razaoSocial: e.target.value }))} /></Field>
              <Field label="Nome Fantasia"><Input value={ident.nomeFantasia} onChange={e => setIdent(v => ({ ...v, nomeFantasia: e.target.value }))} /></Field>
              <Field label="CNPJ">
                <Input
                  value={ident.cnpj}
                  placeholder="00.000.000/0000-00"
                  className={cnpjErro ? 'border-destructive' : ''}
                  onChange={e => {
                    const masked = maskCnpj(e.target.value)
                    setIdent(v => ({ ...v, cnpj: masked }))
                    setCnpjErro('')
                  }}
                />
                {cnpjErro && <p className="text-xs text-destructive mt-1">{cnpjErro}</p>}
              </Field>
              <Field label="Inscrição Estadual"><Input value={ident.inscricaoEstadual} onChange={e => setIdent(v => ({ ...v, inscricaoEstadual: e.target.value }))} /></Field>
              <Field label="Inscrição Municipal"><Input value={ident.inscricaoMunicipal} onChange={e => setIdent(v => ({ ...v, inscricaoMunicipal: e.target.value }))} /></Field>
              <Field label="Telefone"><Input value={ident.telefone} placeholder="(11) 99999-0000" onChange={e => setIdent(v => ({ ...v, telefone: e.target.value }))} /></Field>
              <div className="col-span-2">
                <Field label="E-mail"><Input type="email" value={ident.email} placeholder="contato@empresa.com" onChange={e => setIdent(v => ({ ...v, email: e.target.value }))} /></Field>
              </div>
              <div className="col-span-2">
                <Field label="Tipo de negócio">
                  <select
                    value={ident.tipoNegocio}
                    onChange={e => setIdent(v => ({ ...v, tipoNegocio: e.target.value }))}
                    className="flex h-9 w-full rounded-md border border-input bg-transparent px-3 py-1 text-sm"
                  >
                    <option value="Lojista">Lojista (varejo / produtos)</option>
                    <option value="Prestador">Prestador de Serviço</option>
                  </select>
                </Field>
              </div>
            </div>
          </Panel>

          <Panel title="Endereço" action={
            <Button onClick={() => void saveEnd()} disabled={savingEnd} size="sm">
              {savingEnd ? 'Salvando...' : 'Salvar'}
            </Button>
          }>
            <div className="grid grid-cols-2 gap-4">
              <div className="col-span-2"><Field label="Logradouro"><Input value={end.logradouro} onChange={e => setEnd(v => ({ ...v, logradouro: e.target.value }))} /></Field></div>
              <Field label="Número"><Input value={end.numero} onChange={e => setEnd(v => ({ ...v, numero: e.target.value }))} /></Field>
              <Field label="Complemento"><Input value={end.complemento} onChange={e => setEnd(v => ({ ...v, complemento: e.target.value }))} /></Field>
              <Field label="Bairro"><Input value={end.bairro} onChange={e => setEnd(v => ({ ...v, bairro: e.target.value }))} /></Field>
              <Field label="CEP">
                <div className="flex gap-2">
                  <Input
                    value={end.cep}
                    placeholder="00000-000"
                    onChange={e => setEnd(v => ({ ...v, cep: maskCep(e.target.value) }))}
                    onBlur={e => void buscarCep(e.target.value)}
                  />
                  <Button type="button" variant="outline" size="sm" disabled={buscandoCep}
                    onClick={() => void buscarCep(end.cep)}>
                    {buscandoCep ? '...' : 'Buscar'}
                  </Button>
                </div>
              </Field>
              <Field label="Município"><Input value={end.municipio} onChange={e => setEnd(v => ({ ...v, municipio: e.target.value }))} /></Field>
              <Field label="UF"><Input value={end.uf} maxLength={2} className="uppercase" onChange={e => setEnd(v => ({ ...v, uf: e.target.value.toUpperCase() }))} /></Field>
              <div className="col-span-2">
                <Field label="Código Município (IBGE)"><Input value={end.codigoMunicipio} onChange={e => setEnd(v => ({ ...v, codigoMunicipio: e.target.value }))} /></Field>
              </div>
            </div>
          </Panel>
        </div>
      )}

      {/* ── Visual ────────────────────────────────────────────── */}
      {page === 'visual' && (
        <Panel title="Identidade Visual" action={
          <Button onClick={() => void saveVisual()} disabled={savingVisual} size="sm">
            {savingVisual ? 'Salvando...' : 'Salvar'}
          </Button>
        }>
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

          <div className="grid grid-cols-2 gap-4">
            <Field label="Nome de exibição público">
              <Input value={visual.nomeExibicao} onChange={e => setVisual(v => ({ ...v, nomeExibicao: e.target.value }))} placeholder="Ex: Barbearia do João" />
            </Field>
            <Field label="Cor primária">
              <div className="flex items-center gap-2">
                <input type="color" value={visual.corPrimaria}
                  onChange={e => setVisual(v => ({ ...v, corPrimaria: e.target.value }))}
                  className="h-9 w-12 rounded border cursor-pointer shrink-0" />
                <Input value={visual.corPrimaria} placeholder="#2563eb" className="max-w-28"
                  onChange={e => setVisual(v => ({ ...v, corPrimaria: e.target.value }))} />
              </div>
            </Field>
          </div>

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
        </Panel>
      )}

      {/* ── Layout da navegação ───────────────────────────────── */}
      {page === 'visual' && (
        <Panel title="Layout da navegação">
          <p className="text-xs text-muted-foreground -mt-2">Escolha como o menu principal é exibido no desktop e tablet. No celular o menu inferior é sempre usado.</p>
          <div className="grid grid-cols-2 gap-3 mt-1">
            {([
              {
                mode: 'sidebar',
                label: 'Sidebar lateral',
                desc: 'Menu fixo na esquerda com grupos e ícones',
                preview: (
                  <div className="flex gap-1 h-10 rounded overflow-hidden border">
                    <div className="w-4 bg-muted-foreground/20 flex flex-col gap-0.5 p-0.5">
                      {[1,2,3,4].map(i => <div key={i} className="h-1 rounded bg-muted-foreground/40" />)}
                    </div>
                    <div className="flex-1 bg-muted/30" />
                  </div>
                ),
              },
              {
                mode: 'topnav',
                label: 'Barra superior',
                desc: 'Menu horizontal no topo com dropdowns',
                preview: (
                  <div className="flex flex-col gap-1 h-10 rounded overflow-hidden border">
                    <div className="h-3 bg-muted-foreground/20 flex items-center gap-0.5 px-1">
                      {[1,2,3,4,5].map(i => <div key={i} className="h-1 w-4 rounded bg-muted-foreground/40" />)}
                    </div>
                    <div className="flex-1 bg-muted/30" />
                  </div>
                ),
              },
            ] as const).map(({ mode, label, desc, preview }) => {
              const current = (() => {
                try { return localStorage.getItem('layout-mode') || 'sidebar' } catch { return 'sidebar' }
              })()
              const selected = current === mode
              return (
                <button
                  key={mode}
                  type="button"
                  onClick={() => {
                    try { localStorage.setItem('layout-mode', mode) } catch { /* ignore */ }
                    window.dispatchEvent(new CustomEvent('layout-mode-change', { detail: mode }))
                  }}
                  className={`rounded-lg border-2 p-3 text-left space-y-2 transition-colors ${
                    selected ? 'border-primary bg-primary/5' : 'border-border hover:border-muted-foreground'
                  }`}
                >
                  {preview}
                  <div>
                    <p className={`text-sm font-medium ${selected ? 'text-primary' : ''}`}>{label}</p>
                    <p className="text-xs text-muted-foreground">{desc}</p>
                  </div>
                </button>
              )
            })}
          </div>
        </Panel>
      )}

      {/* ── Dashboard ─────────────────────────────────────────── */}
      {page === 'dashboard' && (
        <div className="space-y-4">
          {/* Widgets ativos (ordenados) */}
          <Panel title={`Widgets ativos — ${localWidgets.length} selecionados`} action={
            <Button onClick={() => void saveLayoutConfig()} disabled={savingLayout} size="sm">
              {savingLayout ? 'Salvando...' : 'Salvar ordem'}
            </Button>
          }>
            {localWidgets.length === 0 ? (
              <p className="text-sm text-muted-foreground py-2">Nenhum widget ativo. Adicione widgets abaixo.</p>
            ) : (
              <div className="space-y-1">
                {localWidgets.map((id, idx) => {
                  const meta = ALL_WIDGETS.find(w => w.id === id)
                  if (!meta) return null
                  const cat = WIDGET_CATEGORY[id] ?? 'Geral'
                  return (
                    <div key={id} className="flex items-center gap-2 rounded-lg border bg-muted/30 px-3 py-2">
                      <span className="text-xs text-muted-foreground w-5 text-center shrink-0">{idx + 1}</span>
                      <div className="flex flex-col gap-0.5 shrink-0">
                        <button onClick={() => moveWidget(id, -1)} disabled={idx === 0}
                          className="text-muted-foreground hover:text-foreground disabled:opacity-25 leading-none text-xs" aria-label="subir">▲</button>
                        <button onClick={() => moveWidget(id, 1)} disabled={idx === localWidgets.length - 1}
                          className="text-muted-foreground hover:text-foreground disabled:opacity-25 leading-none text-xs" aria-label="descer">▼</button>
                      </div>
                      <div className="flex-1 min-w-0">
                        <p className="text-sm font-medium leading-tight truncate">{meta.label}</p>
                      </div>
                      <span className="text-xs px-1.5 py-0.5 rounded bg-muted text-muted-foreground shrink-0">{cat}</span>
                      <button onClick={() => toggleWidget(id)} className="text-xs text-red-500 hover:text-red-700 shrink-0">✕</button>
                    </div>
                  )
                })}
              </div>
            )}
          </Panel>

          {/* Widgets disponíveis por categoria */}
          <div className="rounded-xl border bg-card p-5 space-y-3">
            <p className="text-sm font-semibold uppercase tracking-wide text-muted-foreground">Adicionar widgets</p>
            <p className="text-xs text-muted-foreground">Clique em uma categoria para expandir e adicionar widgets.</p>

            {WIDGET_CATEGORIES_ORDER.map(cat => {
              const catWidgets = ALL_WIDGETS.filter(w => (WIDGET_CATEGORY[w.id] ?? 'Geral') === cat)
              const available = catWidgets.filter(w => !localWidgets.includes(w.id))
              const activeCount = catWidgets.length - available.length
              const isOpen = openCats[cat] ?? false

              return (
                <div key={cat} className="rounded-lg border overflow-hidden">
                  <button
                    onClick={() => setOpenCats(p => ({ ...p, [cat]: !p[cat] }))}
                    className="w-full flex items-center justify-between px-4 py-3 bg-muted/20 hover:bg-muted/40 transition-colors text-left"
                  >
                    <div className="flex items-center gap-2">
                      {isOpen ? <ChevronDown size={14} /> : <ChevronRight size={14} />}
                      <span className="text-sm font-medium">{cat}</span>
                    </div>
                    <div className="flex items-center gap-2 text-xs text-muted-foreground">
                      {activeCount > 0 && (
                        <span className="px-1.5 py-0.5 rounded bg-primary/10 text-primary font-medium">
                          {activeCount} ativo{activeCount > 1 ? 's' : ''}
                        </span>
                      )}
                      <span>{available.length} disponíve{available.length === 1 ? 'l' : 'is'}</span>
                    </div>
                  </button>

                  {isOpen && (
                    <div className="divide-y">
                      {catWidgets.map(meta => {
                        const active = localWidgets.includes(meta.id)
                        return (
                          <div key={meta.id} className={cn(
                            'flex items-center gap-3 px-4 py-2.5',
                            active ? 'bg-primary/5' : 'hover:bg-muted/20'
                          )}>
                            <div className="flex-1 min-w-0">
                              <p className={cn('text-sm font-medium leading-tight', active && 'text-primary')}>{meta.label}</p>
                              <p className="text-xs text-muted-foreground truncate">{meta.description}</p>
                            </div>
                            {active ? (
                              <button onClick={() => toggleWidget(meta.id)}
                                className="text-xs text-red-500 hover:text-red-700 shrink-0 px-2 py-1 rounded border border-red-200 hover:border-red-400 transition-colors">
                                Remover
                              </button>
                            ) : (
                              <button onClick={() => toggleWidget(meta.id)}
                                className="text-xs text-primary hover:text-primary/80 shrink-0 px-2 py-1 rounded border border-primary/30 hover:border-primary transition-colors">
                                Adicionar
                              </button>
                            )}
                          </div>
                        )
                      })}
                    </div>
                  )}
                </div>
              )
            })}
          </div>
        </div>
      )}

      {/* ── Relatórios ────────────────────────────────────────── */}
      {page === 'relatorios' && (
        <div className="space-y-4">
          {/* Abas ativas (ordenadas) */}
          <Panel title={`Abas ativas — ${localTabs.length} de ${ALL_TABS.length}`} action={
            <Button onClick={() => void saveTabLayoutConfig()} disabled={savingTabLayout} size="sm">
              {savingTabLayout ? 'Salvando...' : 'Salvar ordem'}
            </Button>
          }>
            {localTabs.length === 0 ? (
              <p className="text-sm text-muted-foreground py-2">Nenhuma aba ativa. Ative abas abaixo.</p>
            ) : (
              <div className="space-y-1">
                {localTabs.map((id, idx) => {
                  const meta = ALL_TABS.find(t => t.id === id)
                  if (!meta) return null
                  return (
                    <div key={id} className="flex items-center gap-2 rounded-lg border bg-muted/30 px-3 py-2">
                      <span className="text-xs text-muted-foreground w-5 text-center shrink-0">{idx + 1}</span>
                      <div className="flex flex-col gap-0.5 shrink-0">
                        <button onClick={() => moveTab(id, -1)} disabled={idx === 0}
                          className="text-muted-foreground hover:text-foreground disabled:opacity-25 leading-none text-xs" aria-label="subir">▲</button>
                        <button onClick={() => moveTab(id, 1)} disabled={idx === localTabs.length - 1}
                          className="text-muted-foreground hover:text-foreground disabled:opacity-25 leading-none text-xs" aria-label="descer">▼</button>
                      </div>
                      <p className="flex-1 text-sm font-medium">{meta.label}</p>
                      <button onClick={() => toggleTab(id)} className="text-xs text-red-500 hover:text-red-700 shrink-0">✕</button>
                    </div>
                  )
                })}
              </div>
            )}
          </Panel>

          {/* Todas as abas disponíveis — grid */}
          {ALL_TABS.filter(t => !localTabs.includes(t.id)).length > 0 && (
            <div className="rounded-xl border bg-card p-5 space-y-3">
              <p className="text-sm font-semibold uppercase tracking-wide text-muted-foreground">Abas disponíveis</p>
              <div className="grid grid-cols-1 sm:grid-cols-2 gap-2">
                {ALL_TABS.filter(t => !localTabs.includes(t.id)).map(meta => (
                  <div key={meta.id} className="flex items-start gap-3 rounded-lg border border-dashed p-3 hover:bg-muted/20 transition-colors">
                    <div className="flex-1 min-w-0">
                      <p className="text-sm font-medium">{meta.label}</p>
                      <p className="text-xs text-muted-foreground mt-0.5">{meta.description}</p>
                    </div>
                    <button onClick={() => toggleTab(meta.id)}
                      className="text-xs text-primary hover:text-primary/80 shrink-0 px-2 py-1 rounded border border-primary/30 hover:border-primary transition-colors mt-0.5">
                      Ativar
                    </button>
                  </div>
                ))}
              </div>
            </div>
          )}
        </div>
      )}

      {/* ── Fiscal ────────────────────────────────────────────── */}
      {page === 'fiscal' && (
        <Panel title="Emissão de Notas Fiscais" action={
          <Button onClick={() => void saveNfe()} disabled={savingNfe} size="sm">
            {savingNfe ? 'Salvando...' : 'Salvar'}
          </Button>
        }>
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
          <Field label={`Token Focus NF-e${temToken ? ' (já configurado)' : ''}`}>
            <Input type="password" value={focusNfeToken} onChange={e => setFocusNfeToken(e.target.value)}
              placeholder={temToken ? 'Deixe em branco para manter o atual' : 'Cole o token da API Focus NFe'} />
          </Field>
        </Panel>
      )}
    </div>
  )
}
