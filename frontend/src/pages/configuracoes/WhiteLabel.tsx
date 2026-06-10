import { useEffect, useState } from 'react'
import { api } from '@/services/api'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { toast } from '@/hooks/useToast'

interface WhiteLabelConfig {
  slug: string
  logoUrl: string
  corPrimaria: string
  descricaoPublica: string
  dominioCustomizado: string
}

export default function WhiteLabel() {
  const [config, setConfig] = useState<WhiteLabelConfig>({
    slug: '', logoUrl: '', corPrimaria: '#2563eb',
    descricaoPublica: '', dominioCustomizado: '',
  })
  const [saving, setSaving] = useState(false)

  useEffect(() => {
    api.get<Partial<WhiteLabelConfig>>('/api/configuracao-empresa')
      .then(d => setConfig({
        slug: d.slug ?? '',
        logoUrl: d.logoUrl ?? '',
        corPrimaria: d.corPrimaria ?? '#2563eb',
        descricaoPublica: d.descricaoPublica ?? '',
        dominioCustomizado: d.dominioCustomizado ?? '',
      }))
      .catch(() => {})
  }, [])

  async function handleSave() {
    setSaving(true)
    try {
      await api.put('/api/configuracao-empresa/white-label', config)
      toast.success('Configurações salvas!')
    } catch (e) {
      toast.error(e instanceof Error ? e.message : 'Erro')
    } finally {
      setSaving(false)
    }
  }

  return (
    <div className="space-y-6 max-w-lg">
      <h1 className="text-2xl font-bold">White Label</h1>

      <div className="rounded-md border p-4 space-y-4">
        <h2 className="font-semibold">Identidade Visual</h2>

        <div className="grid gap-2">
          <Label>Slug público (URL da agenda)</Label>
          <div className="flex items-center gap-2">
            <span className="text-sm text-muted-foreground shrink-0">gestorai.com.br/agendar/</span>
            <Input
              value={config.slug}
              onChange={e => setConfig(c => ({ ...c, slug: e.target.value.toLowerCase().replace(/[^a-z0-9-]/g, '') }))}
              placeholder="minha-empresa"
            />
          </div>
        </div>

        <div className="grid gap-2">
          <Label>URL da Logo</Label>
          <Input
            value={config.logoUrl}
            onChange={e => setConfig(c => ({ ...c, logoUrl: e.target.value }))}
            placeholder="https://..."
          />
          {config.logoUrl && (
            <img src={config.logoUrl} alt="Logo" className="h-12 object-contain" />
          )}
        </div>

        <div className="grid gap-2">
          <Label>Cor Primária</Label>
          <div className="flex items-center gap-2">
            <input
              type="color"
              value={config.corPrimaria}
              onChange={e => setConfig(c => ({ ...c, corPrimaria: e.target.value }))}
              className="h-9 w-16 rounded border cursor-pointer"
            />
            <Input
              value={config.corPrimaria}
              onChange={e => setConfig(c => ({ ...c, corPrimaria: e.target.value }))}
              className="w-32"
              placeholder="#2563eb"
            />
          </div>
        </div>

        <div className="grid gap-2">
          <Label>Descrição pública</Label>
          <Input
            value={config.descricaoPublica}
            onChange={e => setConfig(c => ({ ...c, descricaoPublica: e.target.value }))}
            placeholder="Clínica de estética e bem-estar..."
          />
        </div>
      </div>

      <div className="rounded-md border p-4 space-y-4">
        <h2 className="font-semibold">Domínio Customizado</h2>
        <p className="text-sm text-muted-foreground">
          Configure um domínio próprio para o painel e página pública de agendamentos.
          Após configurar, aponte um CNAME para <code className="bg-muted px-1 rounded">gestorai.com.br</code>.
        </p>
        <div className="grid gap-2">
          <Label>Domínio</Label>
          <Input
            value={config.dominioCustomizado}
            onChange={e => setConfig(c => ({ ...c, dominioCustomizado: e.target.value }))}
            placeholder="agenda.minha-clinica.com.br"
          />
        </div>
      </div>

      <Button onClick={() => void handleSave()} disabled={saving}>
        {saving ? 'Salvando...' : 'Salvar'}
      </Button>
    </div>
  )
}
