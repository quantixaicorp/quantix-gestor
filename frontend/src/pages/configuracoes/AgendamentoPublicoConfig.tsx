import { useEffect, useState } from 'react'
import { api } from '@/services/api'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { toast } from '@/hooks/useToast'
import type { ConfiguracaoEmpresaResponse } from '@/types/fiscal'

export default function AgendamentoPublicoConfig() {
  const [slug, setSlug] = useState('')
  const [aprovarAutomaticamente, setAprovarAutomaticamente] = useState(true)
  const [valorSinal, setValorSinal] = useState('')
  const [horasLimiteCancelamento, setHorasLimiteCancelamento] = useState('')
  const [saving, setSaving] = useState(false)

  useEffect(() => {
    api.get<ConfiguracaoEmpresaResponse>('/api/configuracao-empresa')
      .then(c => {
        setSlug(c.slug ?? '')
        setAprovarAutomaticamente(c.aprovarAutomaticamente)
        setValorSinal(c.valorSinal != null ? String(c.valorSinal) : '')
        setHorasLimiteCancelamento(c.horasLimiteCancelamento != null ? String(c.horasLimiteCancelamento) : '')
      })
      .catch(() => {})
  }, [])

  async function handleSave() {
    setSaving(true)
    try {
      await api.put('/api/configuracao-empresa/agendamento', {
        aprovarAutomaticamente,
        valorSinal: valorSinal ? parseFloat(valorSinal) : null,
        horasLimiteCancelamento: horasLimiteCancelamento ? parseInt(horasLimiteCancelamento) : null,
      })
      toast.success('Configurações salvas!')
    } catch (e) {
      toast.error(e instanceof Error ? e.message : 'Erro ao salvar')
    } finally {
      setSaving(false)
    }
  }

  const bookingUrl = slug ? `${window.location.origin}/agendar/${slug}` : null

  return (
    <div className="space-y-6 max-w-lg">
      <h1 className="text-2xl font-bold">Agendamento Online</h1>

      {bookingUrl ? (
        <div className="rounded-md border p-4 space-y-2">
          <p className="font-medium text-sm">Link público para clientes</p>
          <div className="flex items-center gap-2">
            <code className="text-xs bg-muted px-2 py-1.5 rounded flex-1 break-all">{bookingUrl}</code>
            <Button size="sm" variant="outline"
              onClick={() => { void navigator.clipboard.writeText(bookingUrl); toast.success('Copiado!') }}>
              Copiar
            </Button>
          </div>
          <p className="text-xs text-muted-foreground">
            Envie este link para seus clientes agendarem online.
          </p>
        </div>
      ) : (
        <p className="text-sm text-muted-foreground">
          Configure o slug da empresa em{' '}
          <a href="/configuracoes/empresa" className="underline text-primary">
            Configuração da Empresa
          </a>{' '}
          para gerar o link público.
        </p>
      )}

      <div className="rounded-md border p-4 space-y-4">
        <h2 className="font-semibold">Política de Agendamento</h2>

        <div className="space-y-1.5">
          <div className="flex items-center gap-2">
            <input type="checkbox" id="aprovarAuto" checked={aprovarAutomaticamente}
              onChange={e => setAprovarAutomaticamente(e.target.checked)} className="h-4 w-4" />
            <Label htmlFor="aprovarAuto">Confirmar agendamentos automaticamente</Label>
          </div>
          <p className="text-xs text-muted-foreground">
            Quando desmarcado, novos agendamentos ficam "Aguardando Confirmação".
          </p>
        </div>

        <div className="grid gap-2">
          <Label>Valor do sinal de reserva (R$)</Label>
          <Input type="number" min="0" step="0.01" value={valorSinal}
            onChange={e => setValorSinal(e.target.value)} placeholder="0,00 (sem sinal)" />
          <p className="text-xs text-muted-foreground">
            Cobrado via PIX no momento do agendamento. Requer Asaas configurado.
          </p>
        </div>

        <div className="grid gap-2">
          <Label>Horas mínimas para cancelamento</Label>
          <Input type="number" min="0" value={horasLimiteCancelamento}
            onChange={e => setHorasLimiteCancelamento(e.target.value)}
            placeholder="Ex: 24 (sem restrição se vazio)" />
        </div>
      </div>

      <Button onClick={() => void handleSave()} disabled={saving}>
        {saving ? 'Salvando...' : 'Salvar configurações'}
      </Button>
    </div>
  )
}
