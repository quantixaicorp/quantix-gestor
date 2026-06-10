import { useEffect, useState } from 'react'
import { api } from '@/services/api'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { toast } from '@/hooks/useToast'

interface ConfigIntegracoes {
  asaasApiKey: string
  asaasSandbox: boolean
  clickSignApiKey: string
  clickSignSandbox: boolean
}

export default function Integracoes() {
  const [config, setConfig] = useState<ConfigIntegracoes>({
    asaasApiKey: '', asaasSandbox: true,
    clickSignApiKey: '', clickSignSandbox: true,
  })
  const [saving, setSaving] = useState(false)

  useEffect(() => {
    api.get<{ asaasApiKey?: string | null; asaasSandbox?: boolean; clickSignApiKey?: string | null; clickSignSandbox?: boolean }>('/api/configuracao-empresa')
      .then(d => setConfig({
        asaasApiKey: d.asaasApiKey ?? '',
        asaasSandbox: d.asaasSandbox ?? true,
        clickSignApiKey: d.clickSignApiKey ?? '',
        clickSignSandbox: d.clickSignSandbox ?? true,
      }))
      .catch(() => {})
  }, [])

  async function handleSave() {
    setSaving(true)
    try {
      await api.put('/api/configuracao-empresa/integracoes', {
        asaasApiKey: config.asaasApiKey || null,
        asaasSandbox: config.asaasSandbox,
        clickSignApiKey: config.clickSignApiKey || null,
        clickSignSandbox: config.clickSignSandbox,
      })
      toast.success('Configurações salvas!')
    } catch (e) {
      toast.error(e instanceof Error ? e.message : 'Erro ao salvar')
    } finally {
      setSaving(false)
    }
  }

  return (
    <div className="space-y-6 max-w-lg">
      <h1 className="text-2xl font-bold">Integrações</h1>

      <div className="rounded-md border p-4 space-y-4">
        <h2 className="font-semibold">Asaas — Cobranças Automáticas</h2>
        <p className="text-sm text-muted-foreground">
          Conecte sua conta Asaas para emitir PIX e boletos diretamente do GestorAI.
          Obtenha sua API Key em app.asaas.com → Configurações → Integrações.
        </p>
        <div className="grid gap-2">
          <Label>API Key</Label>
          <Input
            type="password"
            value={config.asaasApiKey}
            onChange={e => setConfig(c => ({ ...c, asaasApiKey: e.target.value }))}
            placeholder="$aact_..."
          />
        </div>
        <div className="flex items-center gap-2">
          <input
            type="checkbox"
            id="sandbox"
            checked={config.asaasSandbox}
            onChange={e => setConfig(c => ({ ...c, asaasSandbox: e.target.checked }))}
          />
          <Label htmlFor="sandbox">Modo Sandbox (testes)</Label>
        </div>
      </div>

      <div className="rounded-md border p-4 space-y-4">
        <h2 className="font-semibold">ClickSign — Assinatura Digital</h2>
        <p className="text-sm text-muted-foreground">
          Conecte sua conta ClickSign para enviar contratos para assinatura eletrônica.
          Obtenha sua API Key em app.clicksign.com → Configurações → Integrações.
        </p>
        <div className="grid gap-2">
          <Label>API Key</Label>
          <Input
            type="password"
            value={config.clickSignApiKey}
            onChange={e => setConfig(c => ({ ...c, clickSignApiKey: e.target.value }))}
            placeholder="xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx"
          />
        </div>
        <div className="flex items-center gap-2">
          <input
            type="checkbox"
            id="clicksign-sandbox"
            checked={config.clickSignSandbox}
            onChange={e => setConfig(c => ({ ...c, clickSignSandbox: e.target.checked }))}
          />
          <Label htmlFor="clicksign-sandbox">Modo Sandbox (testes)</Label>
        </div>
      </div>

      <Button onClick={() => void handleSave()} disabled={saving}>
        {saving ? 'Salvando...' : 'Salvar todas as integrações'}
      </Button>
    </div>
  )
}
