import { useEffect, useState } from 'react'
import { api } from '@/services/api'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { toast } from '@/hooks/useToast'

interface BrandingConfig {
  slug: string | null
  corPrimaria: string | null
  descricaoPublica: string | null
  logoUrl: string | null
}

interface ConfiguracaoEmpresaBranding {
  slug?: string
  corPrimaria?: string
  descricaoPublica?: string
  logoUrl?: string
}

export default function AgendamentoPublicoConfig() {
  const [config, setConfig] = useState<BrandingConfig>({
    slug: '',
    corPrimaria: '#3B82F6',
    descricaoPublica: '',
    logoUrl: null,
  })
  const [saving, setSaving] = useState(false)
  const [uploading, setUploading] = useState(false)
  const [copied, setCopied] = useState(false)

  useEffect(() => {
    api.get<ConfiguracaoEmpresaBranding>('/api/configuracao-empresa')
      .then(data => setConfig({
        slug: data.slug ?? '',
        corPrimaria: data.corPrimaria ?? '#3B82F6',
        descricaoPublica: data.descricaoPublica ?? '',
        logoUrl: data.logoUrl ?? null,
      }))
      .catch(() => {})
  }, [])

  async function handleSave() {
    if (!config.slug) {
      toast.error('Informe um slug')
      return
    }
    const slugValido = /^[a-z0-9-]+$/.test(config.slug)
    if (!slugValido) {
      toast.error('Slug deve conter apenas letras minúsculas, números e hífens')
      return
    }
    setSaving(true)
    try {
      await api.put('/api/configuracao-empresa/branding', {
        slug: config.slug,
        corPrimaria: config.corPrimaria,
        descricaoPublica: config.descricaoPublica,
      })
      toast.success('Configurações salvas!')
    } catch (e) {
      toast.error(e instanceof Error ? e.message : 'Erro ao salvar')
    } finally {
      setSaving(false)
    }
  }

  async function handleLogoUpload(e: React.ChangeEvent<HTMLInputElement>) {
    const file = e.target.files?.[0]
    if (!file) return
    setUploading(true)
    try {
      const formData = new FormData()
      formData.append('file', file)
      const token = localStorage.getItem('ga_token')
      const apiBase = import.meta.env.VITE_API_URL ?? 'http://localhost:5002'
      const res = await fetch(`${apiBase}/api/configuracao-empresa/logo`, {
        method: 'POST',
        headers: token ? { Authorization: `Bearer ${token}` } : {},
        body: formData,
      })
      if (!res.ok) {
        const body = await res.json().catch(() => null) as { error?: string } | null
        throw new Error(body?.error ?? `Erro ao enviar logo (${res.status})`)
      }
      const data = await res.json() as { logoUrl: string }
      setConfig(c => ({ ...c, logoUrl: data.logoUrl }))
      toast.success('Logo atualizada!')
    } catch (e) {
      toast.error(e instanceof Error ? e.message : 'Erro ao enviar logo')
    } finally {
      setUploading(false)
    }
  }

  const link = config.slug ? `${window.location.origin}/agendar/${config.slug}` : null
  const apiBase = import.meta.env.VITE_API_URL ?? 'http://localhost:5002'

  return (
    <div className="space-y-6 max-w-lg">
      <h1 className="text-2xl font-bold">Agendamento Online</h1>

      <div className="rounded-md border p-4 space-y-4">
        <h2 className="font-semibold">Identidade Visual</h2>

        <div className="grid gap-2">
          <Label>Logo</Label>
          <div className="flex items-center gap-4">
            {config.logoUrl && (
              <img
                src={`${apiBase}${config.logoUrl}`}
                alt="Logo"
                className="h-16 w-16 rounded-full object-cover border"
              />
            )}
            <label className="cursor-pointer">
              <span className="inline-flex items-center px-3 py-1.5 rounded-md border text-sm hover:bg-muted transition-colors">
                {uploading ? 'Enviando...' : 'Escolher arquivo'}
              </span>
              <input
                type="file"
                accept=".jpg,.jpeg,.png,.webp"
                className="hidden"
                onChange={e => void handleLogoUpload(e)}
                disabled={uploading}
              />
            </label>
            <p className="text-xs text-muted-foreground">jpg, png ou webp • máx 2MB</p>
          </div>
        </div>

        <div className="grid gap-2">
          <Label>Cor principal</Label>
          <div className="flex items-center gap-3">
            <input
              type="color"
              value={config.corPrimaria ?? '#3B82F6'}
              onChange={e => setConfig(c => ({ ...c, corPrimaria: e.target.value }))}
              className="h-9 w-16 rounded border cursor-pointer"
            />
            <Input
              value={config.corPrimaria ?? ''}
              onChange={e => setConfig(c => ({ ...c, corPrimaria: e.target.value }))}
              placeholder="#3B82F6"
              className="max-w-28"
            />
          </div>
        </div>

        <div className="grid gap-2">
          <Label>Descrição pública</Label>
          <Input
            value={config.descricaoPublica ?? ''}
            onChange={e => setConfig(c => ({ ...c, descricaoPublica: e.target.value }))}
            placeholder="Ex: Barbearia especializada em cortes modernos"
          />
        </div>
      </div>

      <div className="rounded-md border p-4 space-y-3">
        <h2 className="font-semibold">Link do agendamento</h2>
        <div className="grid gap-2">
          <Label>Slug (identificador único)</Label>
          <div className="flex gap-2 items-center">
            <span className="text-sm text-muted-foreground whitespace-nowrap">/agendar/</span>
            <Input
              value={config.slug ?? ''}
              onChange={e =>
                setConfig(c => ({
                  ...c,
                  slug: e.target.value.toLowerCase().replace(/[^a-z0-9-]/g, ''),
                }))
              }
              placeholder="minha-empresa"
            />
          </div>
          <p className="text-xs text-muted-foreground">Apenas letras minúsculas, números e hífens.</p>
        </div>

        {link && (
          <div className="flex items-center gap-2 bg-muted rounded-md px-3 py-2">
            <p className="text-sm flex-1 truncate font-mono">{link}</p>
            <Button
              size="sm"
              variant="outline"
              onClick={() => {
                navigator.clipboard.writeText(link).catch(() => {})
                setCopied(true)
                setTimeout(() => setCopied(false), 2000)
              }}
            >
              {copied ? 'Copiado!' : 'Copiar'}
            </Button>
          </div>
        )}
      </div>

      <Button onClick={() => void handleSave()} disabled={saving}>
        {saving ? 'Salvando...' : 'Salvar configurações'}
      </Button>
    </div>
  )
}
