import { useEffect, useState } from 'react'
import { api } from '@/services/api'
import { Button } from '@/components/ui/button'
import { toast } from '@/hooks/useToast'
import { Check } from 'lucide-react'
import { cn } from '@/lib/utils'

interface PlanoSaaS {
  id: string
  nome: string
  descricao: string
  preco: number
  features: string[]
}

interface PlanoAtual {
  planoId: string
  planoNome: string
  preco: number
  features: string[]
  inicioEm: string
}

const FEATURE_LABELS: Record<string, string> = {
  asaas_cobrancas:      'Cobranças PIX/Boleto (Asaas)',
  automacoes_whatsapp:  'Automações via WhatsApp',
  assinatura_digital:   'Assinatura Digital (ClickSign)',
  sinal_reserva:        'Sinal de Reserva em Agendamentos',
  relatorios_avancados: 'Relatórios Avançados',
  nota_fiscal:          'Emissão de NF-e/NFS-e',
  multi_profissional:   'Múltiplos Profissionais',
}

const fmt = (v: number) => v.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' })

export default function PlanoAssinatura() {
  const [planos, setPlanos] = useState<PlanoSaaS[]>([])
  const [atual, setAtual] = useState<PlanoAtual | null>(null)
  const [ativando, setAtivando] = useState<string | null>(null)

  useEffect(() => {
    void api.get<PlanoSaaS[]>('/api/planos').then(setPlanos).catch(() => {})
    void api.get<PlanoAtual | null>('/api/planos/atual').then(setAtual).catch(() => {})
  }, [])

  async function handleAtivar(planoId: string) {
    setAtivando(planoId)
    try {
      await api.post(`/api/planos/ativar/${planoId}`, {})
      toast.success('Plano atualizado!')
      const plano = planos.find(p => p.id === planoId)
      if (plano) {
        setAtual({
          planoId: plano.id,
          planoNome: plano.nome,
          preco: plano.preco,
          features: plano.features,
          inicioEm: new Date().toISOString(),
        })
      }
    } catch (e) {
      toast.error(e instanceof Error ? e.message : 'Erro ao ativar plano')
    } finally {
      setAtivando(null)
    }
  }

  return (
    <div className="space-y-6 max-w-4xl">
      <div>
        <h1 className="text-2xl font-bold">Plano de Assinatura</h1>
        {atual && (
          <p className="text-muted-foreground mt-1">
            Plano atual: <strong>{atual.planoNome}</strong> — {fmt(atual.preco)}/mês
          </p>
        )}
        {!atual && (
          <p className="text-muted-foreground mt-1">Nenhum plano ativo. Escolha um plano abaixo.</p>
        )}
      </div>

      <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
        {planos.map(plano => {
          const isAtual = atual?.planoId === plano.id
          return (
            <div key={plano.id} className={cn(
              'rounded-xl border p-6 flex flex-col gap-4',
              isAtual && 'border-primary ring-2 ring-primary/30'
            )}>
              <div>
                <h2 className="text-lg font-bold">{plano.nome}</h2>
                <p className="text-sm text-muted-foreground">{plano.descricao}</p>
                <p className="text-2xl font-bold mt-2">
                  {fmt(plano.preco)}
                  <span className="text-sm font-normal text-muted-foreground">/mês</span>
                </p>
              </div>

              <ul className="space-y-1 flex-1">
                {Object.entries(FEATURE_LABELS).map(([key, label]) => (
                  <li key={key} className={cn(
                    'flex items-center gap-2 text-sm',
                    !plano.features.includes(key) && 'text-muted-foreground/50'
                  )}>
                    <Check className={cn(
                      'h-4 w-4 shrink-0',
                      plano.features.includes(key) ? 'text-green-500' : 'text-muted-foreground/30'
                    )} />
                    {label}
                  </li>
                ))}
              </ul>

              <Button
                className="w-full"
                variant={isAtual ? 'outline' : 'default'}
                disabled={isAtual || ativando !== null}
                onClick={() => void handleAtivar(plano.id)}
              >
                {isAtual ? 'Plano Atual' : ativando === plano.id ? 'Ativando...' : 'Escolher Plano'}
              </Button>
            </div>
          )
        })}
      </div>
    </div>
  )
}
