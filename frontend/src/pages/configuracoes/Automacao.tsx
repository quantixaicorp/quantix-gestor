import { useEffect, useState } from 'react'
import { api } from '@/services/api'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { toast } from '@/hooks/useToast'

interface AutomacaoConfig {
  evolutionApiUrl: string
  evolutionApiKey: string
  evolutionInstance: string
  reminder3DaysBefore: boolean
  reminder1DayBefore: boolean
  reminderOnDueDate: boolean
  reminder1DayAfter: boolean
  reminder3DaysAfter: boolean
  reminder7DaysAfter: boolean
}

const TOGGLES: { key: keyof AutomacaoConfig; label: string }[] = [
  { key: 'reminder3DaysBefore', label: '3 dias antes do vencimento' },
  { key: 'reminder1DayBefore',  label: '1 dia antes do vencimento' },
  { key: 'reminderOnDueDate',   label: 'No dia do vencimento' },
  { key: 'reminder1DayAfter',   label: '1 dia após o vencimento' },
  { key: 'reminder3DaysAfter',  label: '3 dias após o vencimento' },
  { key: 'reminder7DaysAfter',  label: '7 dias após o vencimento' },
]

export default function Automacao() {
  const [config, setConfig] = useState<AutomacaoConfig>({
    evolutionApiUrl: '',
    evolutionApiKey: '',
    evolutionInstance: '',
    reminder3DaysBefore: true,
    reminder1DayBefore: true,
    reminderOnDueDate: true,
    reminder1DayAfter: true,
    reminder3DaysAfter: false,
    reminder7DaysAfter: false,
  })
  const [saving, setSaving] = useState(false)
  const [testando, setTestando] = useState(false)

  useEffect(() => {
    api.get<{
      evolutionApiUrl?: string | null
      evolutionInstance?: string | null
      reminder3DaysBefore?: boolean
      reminder1DayBefore?: boolean
      reminderOnDueDate?: boolean
      reminder1DayAfter?: boolean
      reminder3DaysAfter?: boolean
      reminder7DaysAfter?: boolean
    }>('/api/configuracao-empresa')
      .then(d => setConfig(c => ({
        ...c,
        evolutionApiUrl:  d.evolutionApiUrl  ?? '',
        evolutionInstance: d.evolutionInstance ?? '',
        reminder3DaysBefore: d.reminder3DaysBefore ?? true,
        reminder1DayBefore:  d.reminder1DayBefore  ?? true,
        reminderOnDueDate:   d.reminderOnDueDate   ?? true,
        reminder1DayAfter:   d.reminder1DayAfter   ?? true,
        reminder3DaysAfter:  d.reminder3DaysAfter  ?? false,
        reminder7DaysAfter:  d.reminder7DaysAfter  ?? false,
      })))
      .catch(() => {})
  }, [])

  async function handleSave() {
    setSaving(true)
    try {
      await api.put('/api/configuracao-empresa/automacao', {
        evolutionApiUrl:  config.evolutionApiUrl  || null,
        evolutionApiKey:  config.evolutionApiKey  || null,
        evolutionInstance: config.evolutionInstance || null,
        reminder3DaysBefore: config.reminder3DaysBefore,
        reminder1DayBefore:  config.reminder1DayBefore,
        reminderOnDueDate:   config.reminderOnDueDate,
        reminder1DayAfter:   config.reminder1DayAfter,
        reminder3DaysAfter:  config.reminder3DaysAfter,
        reminder7DaysAfter:  config.reminder7DaysAfter,
      })
      toast.success('Configurações salvas!')
    } catch (e) {
      toast.error(e instanceof Error ? e.message : 'Erro ao salvar')
    } finally {
      setSaving(false)
    }
  }

  async function handleTestarConexao() {
    if (!config.evolutionApiUrl || !config.evolutionApiKey) {
      toast.error('Preencha a URL e a API Key antes de testar.')
      return
    }
    setTestando(true)
    try {
      const res = await api.post<{ sucesso: boolean }>('/api/automacao/testar-conexao', {
        apiUrl: config.evolutionApiUrl,
        apiKey: config.evolutionApiKey,
      })
      if (res.sucesso) toast.success('Conexão bem-sucedida!')
      else toast.error('Falha na conexão. Verifique URL e API Key.')
    } catch {
      toast.error('Falha na conexão.')
    } finally {
      setTestando(false)
    }
  }

  return (
    <div className="space-y-6 max-w-lg">
      <h1 className="text-2xl font-bold">Automação</h1>

      <div className="rounded-md border p-4 space-y-4">
        <h2 className="font-semibold">Integração Evolution API (WhatsApp)</h2>
        <p className="text-sm text-muted-foreground">
          Conecte sua instância do Evolution API para enviar lembretes de cobrança via WhatsApp automaticamente.
        </p>
        <div className="grid gap-2">
          <Label>URL da instância</Label>
          <Input
            value={config.evolutionApiUrl}
            onChange={e => setConfig(c => ({ ...c, evolutionApiUrl: e.target.value }))}
            placeholder="https://evolution.suaempresa.com"
          />
        </div>
        <div className="grid gap-2">
          <Label>API Key</Label>
          <Input
            type="password"
            value={config.evolutionApiKey}
            onChange={e => setConfig(c => ({ ...c, evolutionApiKey: e.target.value }))}
            placeholder="Deixe em branco para manter a atual"
          />
        </div>
        <div className="grid gap-2">
          <Label>Nome da instância</Label>
          <Input
            value={config.evolutionInstance}
            onChange={e => setConfig(c => ({ ...c, evolutionInstance: e.target.value }))}
            placeholder="minha-instancia"
          />
        </div>
        <div className="flex gap-2">
          <Button variant="outline" onClick={() => void handleTestarConexao()} disabled={testando}>
            {testando ? 'Testando...' : 'Testar conexão'}
          </Button>
          <Button onClick={() => void handleSave()} disabled={saving}>
            {saving ? 'Salvando...' : 'Salvar'}
          </Button>
        </div>
      </div>

      <div className="rounded-md border p-4 space-y-4">
        <h2 className="font-semibold">Lembretes ativos</h2>
        <p className="text-sm text-muted-foreground">
          Escolha quando enviar lembretes de cobrança automaticamente via WhatsApp.
        </p>
        <div className="space-y-3">
          {TOGGLES.map(({ key, label }) => (
            <div key={key} className="flex items-center gap-2">
              <input
                type="checkbox"
                id={key}
                checked={config[key] as boolean}
                onChange={e => setConfig(c => ({ ...c, [key]: e.target.checked }))}
              />
              <Label htmlFor={key}>{label}</Label>
            </div>
          ))}
        </div>
        <Button onClick={() => void handleSave()} disabled={saving}>
          {saving ? 'Salvando...' : 'Salvar'}
        </Button>
      </div>
    </div>
  )
}
